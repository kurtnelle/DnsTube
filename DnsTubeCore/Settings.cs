using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DnsTubeCore
{
    public class Settings
    {
        public string SettingsFileName { get; set; }

        public string EmailAddress { get; set; }
        public bool IsUsingToken { get; set; }
        public string ApiKey { get; set; }
        public string ApiToken { get; set; }
        public Settings()
        {

        }

        public void Save() {
            File.WriteAllText(SettingsFileName, JsonConvert.SerializeObject(this));
        }

        public static Settings Load(string settingsFile = "settings.json")
        {
            if (File.Exists(settingsFile))
            {
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile));
            }
            else
            {
                return new Settings() { SettingsFileName = settingsFile };
            }
        }
    }
}
