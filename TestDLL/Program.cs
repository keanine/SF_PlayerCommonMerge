// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;

Console.WriteLine("Hello, World!");

[DllImport("SF_PlayerCommonMergerLib.dll", CallingConvention = CallingConvention.StdCall)]
static extern bool DllMain();

DllMain();
Console.WriteLine("Success");
