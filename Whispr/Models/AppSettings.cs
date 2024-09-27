using System.Text.Json;
using System.IO;

namespace Whispr.Models
{
    public class AppSettings
    {
        public string PythonPath { get; set; } = string.Empty;
        public bool IsPythonInstalled { get; set; } = false;

        private const string SettingsFileName = "settings.json";

        public static AppSettings Load()
        {
            if (File.Exists(SettingsFileName))
            {
                string json = File.ReadAllText(SettingsFileName);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFileName, json);
        }
    }
}