﻿using System;
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

        public static string appdata = string.Empty;

        public bool logDebugInformation { get; set; }
        public static bool LogDebugInformation { get { return instance.logDebugInformation; } set { instance.logDebugInformation = value; } }

        public static void Initialize()
        {
            //Load from JSON and create instance
            string path = Path.Combine(appdata, "preferences.json");

            if ( File.Exists(path))
            {
                string json = File.ReadAllText(path);
                instance = (Preferences)JsonSerializer.Deserialize(json, typeof(Preferences));
            }
            else
            {
                instance = new Preferences();
            }
        }

        public static void Save()
        {
            string json = JsonSerializer.Serialize(instance, new JsonSerializerOptions { WriteIndented = true });
            string path = Path.Combine(appdata, "preferences.json");
            File.WriteAllText(path, json);
        }
    }
}