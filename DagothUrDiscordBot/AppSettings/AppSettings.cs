using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DagothUrDiscordBot.AppSettings
{
    internal class AppSettings
    {
        private static Dictionary<string, string> settings = new Dictionary<string, string>();

        public static string? GetAppSetting(string settingName)
        {
            if (settings.ContainsKey(settingName)){
                return settings[settingName];
            }
            else
            {
                return null;
            }
        }

        public static void LoadAppSettingsFile()
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory)!.Parent!.Parent!.FullName ?? "";
            string appSettingsJSONFilePath = $"{projectDirectory}/appsettings.json";
            if (File.Exists(appSettingsJSONFilePath))
            {
                // Fetch the file contents
                string appSettingsJSON = File.ReadAllText(appSettingsJSONFilePath);
                // Load it
                Dictionary<string, string>? appSettingsData = JsonConvert.DeserializeObject<Dictionary<string, string>>(appSettingsJSON);
                if (appSettingsJSON!= null)
                {
                    AppSettings.settings = appSettingsData!;
                }
            }
            else
            {
                throw new FileNotFoundException($"{appSettingsJSONFilePath} must be present to run this bot.");
            }
        }
    }
}
