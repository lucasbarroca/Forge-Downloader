using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ForgeDownloader
{
    class Program
    {
        static string AppPath;
        static void Main(string[] args)
        {
            // Setup app path
            AppPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string manifestFilePath = $"{AppPath}/manifest.json";

            // Remove temp path if it's there and create it after
            if (Directory.Exists($"{AppPath}/ForgeDownloaderTemp"))
            {
                Directory.Delete($"{AppPath}/ForgeDownloaderTemp", true);
            }
            Directory.CreateDirectory($"{AppPath}/ForgeDownloaderTemp");

            // Get arguments and extract zip
            string modpackZipFile = null;
            if (args.Length > 0)
            {
                modpackZipFile = args[0];
            }
            else
            {
                // Check if it's by url
                if (File.Exists($"{AppPath}/forgedownloadermodpack.json"))
                {
                    try
                    {
                        Console.WriteLine("Searching for modpack...");

                        string modpackJson = File.ReadAllText($"{AppPath}/forgedownloadermodpack.json");
                        JObject modpack = JObject.Parse(modpackJson);

                        using (WebClient wc = new WebClient())
                        {
                            JArray modpackfiles = JArray.Parse(wc.DownloadString($"{AppConfig.APIUrl}/{modpack["projectID"]}/files"));
                            foreach (JObject file in modpackfiles)
                            {
                                if ((int)file["id"] == (int)modpack["fileID"])
                                {
                                    Console.WriteLine("Downloading modpack.zip...");
                                    wc.DownloadFile(file["downloadUrl"].ToString(), $"{AppPath}/ForgeDownloaderTemp/modpack.zip");
                                    modpackZipFile = $"{AppPath}/ForgeDownloaderTemp/modpack.zip";
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        modpackZipFile = null;
                    }
                }
            }

            // Extract zip if needed
            if (modpackZipFile != null)
            {
                if (modpackZipFile.EndsWith(".zip"))
                {
                    ZipFile.ExtractToDirectory(modpackZipFile, $"{AppPath}/ForgeDownloaderTemp");
                    manifestFilePath = $"{AppPath}/ForgeDownloaderTemp/manifest.json";
                }
            }

            // Start mods installation
            bool hasErrors = false;
            if (File.Exists(manifestFilePath))
            {
                // Load manifest
                Console.WriteLine("Loading manifest.json...");

                string manifestFile = File.ReadAllText(manifestFilePath);

                JObject manifest = JObject.Parse(manifestFile);

                // Loop all mods
                JToken[] files = manifest["files"].ToArray();
                int downloadCount = 1;

                foreach (JObject file in files)
                {
                    // Get mod files
                    try
                    {
                        // Get mod Infos
                        WebClient webClient = new WebClient();
                        JObject modInfo = JObject.Parse(webClient.DownloadString($"{AppConfig.APIUrl}/{file["projectID"]}"));
                        JArray modFiles = JArray.Parse(webClient.DownloadString($"{AppConfig.APIUrl}/{file["projectID"]}/files"));

                        webClient.Dispose();

                        // Download needed mod file
                        try
                        {
                            // Get specific mod file from list
                            JObject mod = null;
                            foreach(JObject modFile in modFiles)
                            {
                                if ((int)modFile["id"] == (int)file["fileID"])
                                {
                                    mod = modFile;
                                    break;
                                }
                            }

                            if (mod != null)
                            {
                                Console.WriteLine($"{downloadCount}/{files.Length} Downloading \"{modInfo["name"]}\"...");

                                DownloadModFile(mod);
                                downloadCount++;
                            }
                            else
                            {
                                throw new Exception("Can't find mod file!");
                            }
                        }
                        catch(Exception e)
                        {
                            hasErrors = true;
                            Console.WriteLine($"Error during download \"{modInfo["name"]}\".");
                            Console.WriteLine(e.ToString());
                            break;
                        }
                    }
                    catch(Exception e)
                    {
                        hasErrors = true;
                        Console.WriteLine($"Error during get mod {file["projectID"]} informations.");
                        Console.WriteLine(e.ToString());
                        break;
                    }
                }

                Console.WriteLine("All mods installed!");

                // Setup mod overrides
                try
                {
                    if (Directory.Exists($"{AppPath}/ForgeDownloaderTemp/overrides"))
                    {
                        Console.WriteLine("Setting up overrides...");
                        CopyDir($"{AppPath}/ForgeDownloaderTemp/overrides", AppPath);
                    }

                    if (Directory.Exists($"{AppPath}/ForgeDownloaderTemp"))
                    {
                        Directory.Delete($"{AppPath}/ForgeDownloaderTemp", true);
                    }
                }
                catch (Exception e)
                {
                    hasErrors = true;
                    Console.WriteLine($"Error during setup mod overrides.");
                    Console.WriteLine(e.ToString());
                    
                }

                if (!hasErrors)
                {
                    Console.WriteLine("Mod pack installation finished! You can play now ;)");
                }
            }
            else
            {
                Console.WriteLine("Manifest file not found!");
            }

            // Prevent auto closing
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void DownloadModFile(JObject mod)
        {
            // Create mods folder
            Directory.CreateDirectory($"{AppPath}/mods");

            // Check if download is needed


            // Download mod
            Uri uri = new Uri(mod["downloadUrl"].ToString());
            string filename = System.IO.Path.GetFileName(uri.LocalPath);

            using(WebClient wc = new WebClient())
            {
                wc.DownloadFile(uri, $"{AppPath}/mods/" + filename);
            }
        }

        static void CopyDir(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            // Get Files & Copy
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);

                // ADD Unique File Name Check to Below!!!!
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest, true);
            }

            // Get dirs recursively and copy files
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyDir(folder, dest);
            }
        }
    }
}
