using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SF_PlayerCommonMergeTool
{
    [System.Serializable]
    public class Preferences
    {
        private static Preferences instance { get; set; }

        public static string appData = string.Empty;

        public float toolVersion = 2.0f;
        public static float ToolVersion { get { return instance.toolVersion; } }

        public string latestPlayercommonHash = "8d18390d4092f44556cf548f32b8f70f";
        public static string LatestPlayercommonHash { get { return instance.latestPlayercommonHash; } }

        public string nameOfGameUpdate = "1.41";
        public static string NameOfGameUpdate { get { return instance.nameOfGameUpdate; } }

        public int addonFormat = 1;
        public static int AddonFormat { get { return instance.addonFormat; } }



        public bool logDebugInformation { get; set; }
        public static bool LogDebugInformation { get { return instance.logDebugInformation; } set { instance.logDebugInformation = value; } }

        public bool allowCheckingForUpdates { get; set; }
        public static bool AllowCheckingForUpdates { get { return instance.allowCheckingForUpdates; } set { instance.allowCheckingForUpdates = value; } }

        public bool allowUpdatingPac { get; set; }
        public static bool AllowUpdatingPac { get { return instance.allowUpdatingPac; } set { instance.allowUpdatingPac = value; } }

        public string updateBranch { get; set; }
        public static string UpdateBranch { get { return instance.updateBranch; } set { instance.updateBranch = value; } }

        public static void Initialize()
        {
            //Load from JSON and create instance
            string path = Path.Combine(appData, "preferences.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                instance = (Preferences)JsonSerializer.Deserialize(json, typeof(Preferences));
            }
            else
            {
                instance = new Preferences();
                LogDebugInformation = false;
                AllowCheckingForUpdates = true;
                UpdateBranch = "Main";
            }
        }

        public static void Save()
        {
            string json = JsonSerializer.Serialize(instance, new JsonSerializerOptions { WriteIndented = true });
            string path = Path.Combine(appData, "preferences.json");
            File.WriteAllText(path, json);
        }
    }
}
