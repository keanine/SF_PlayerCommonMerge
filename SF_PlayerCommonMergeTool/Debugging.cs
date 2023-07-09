using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SF_PlayerCommonMergeTool
{
    public class Debugging
    {
        //[Conditional("DEBUG")]
        public static void WriteToLog(string message)
        {
            if (Preferences.LogDebugInformation)
            {
                if (File.Exists("merge_tool_log.txt"))
                {
                    File.AppendAllText("merge_tool_log.txt", message + "\n");
                }
                else
                {
                    File.WriteAllText("merge_tool_log.txt", message + "\n");
                }

            }
        }
    }
}
