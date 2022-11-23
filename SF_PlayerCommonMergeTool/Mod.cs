using System;
using System.Collections.Generic;
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
            IniFile modIni = new IniFile(path + "/mod.ini");

            modIni.KeyExists("Title", "Desc");
            string title = modIni.Read("Title", "Desc");

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
