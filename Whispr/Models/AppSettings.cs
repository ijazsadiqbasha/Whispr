using System.Text.Json;
using System.IO;

namespace Whispr.Models
{
    public class AppSettings
    {
        public string PythonPath { get; set; } = string.Empty;
        public bool IsPythonInstalled { get; set; } = false;
        public int Hotkey { get; set; } = 32; // Default to space key

        private const string SettingsFileName = "localsettings.json";
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public AppSettings()
        {
            // Don't call Load() in the constructor
        }

        public static AppSettings LoadOrCreate()
        {
            if (File.Exists(SettingsFileName))
            {
                string json = File.ReadAllText(SettingsFileName);
                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                return loadedSettings ?? new AppSettings();
            }
            return new AppSettings();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(this, _jsonOptions);
            File.WriteAllText(SettingsFileName, json);
        }
    }
}