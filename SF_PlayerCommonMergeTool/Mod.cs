using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SF_PlayerCommonMergeTool
{
    public class Mod
    {
        public enum ActiveSection { None, Physics, Color }

        public string title;
        public string path;
        public ActiveSection activeSection = ActiveSection.None;

        public Mod(string path)
        {
            bool isConfigMod = false;

            IniFile modIni;

            if (File.Exists(path + "/mod.ini"))
            {
                modIni = new IniFile(path + "/mod.ini");
            }
            else
            {
                modIni = new IniFile(path + "/.." + "/mod.ini");
                isConfigMod = true;
            }

            modIni.KeyExists("Title", "Desc");
            string title = modIni.Read("Title", "Desc");

            if (isConfigMod)
            {
                title += " (" + Path.GetFileName(path) + ")";
            }

            this.title = title;
            this.path = path;
            this.activeSection = ActiveSection.None;
        }

        public override string ToString()
        {
            return title;
        }
    }
}
