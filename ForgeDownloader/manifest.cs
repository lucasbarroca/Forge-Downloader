using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForgeDownloader
{
    class manifest
    {
        Minecraft minecraft;
    }

    class Minecraft
    {
        public string version { get; set; } = "1.16.5";
        modLoader[] modLoaders { get; set; } = { new modLoader() };
    }

    class modLoader
    {
        public string id { get; set; }
        public bool primary { get; set; }
    }
}
