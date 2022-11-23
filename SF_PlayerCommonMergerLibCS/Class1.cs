using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keanine.SonicFrontiers.PlayerCommonMerger
{
    public class Class1
    {
        [DllExport]
        public static void TestConnection()
        {
            StreamWriter writer =  File.CreateText("D:\\Test.txt");

            writer.WriteLine("Testing DLL");
            writer.Close();
        }
    }
}
