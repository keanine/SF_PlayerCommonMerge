using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SF_PlayerCommonMergeTool
{
    public class Debugging
    {
        public static void WriteToLog(string message)
        {
            if (Preferences.LogDebugInformation)
            {
                if (!File.Exists("merge_tool_log.txt"))
                {
                    File.CreateText("merge_tool_log.txt");
                }

                File.WriteAllText("merge_tool_log.txt", message);
            }
        }
    }
}
