using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SF_PlayerCommonMergeTool
{
    public class UX
    {
        public static Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            { "MergeComplete", "You can now close this tool, open Hedge Mod Manager and enable the newly generated mod \"${modName}\"" }
        };

        public static string GetMessage(string id, params string[] vars)
        {
            string str = messages[id];

            while (str.Contains("$"))
            {
                int firstIndex = str.IndexOf('$');
                int lastIndex = str.IndexOf('}');
                str.Replace(str.Substring(firstIndex, lastIndex - firstIndex), vars[0]);
            }

            return str;
        }
    }
}
