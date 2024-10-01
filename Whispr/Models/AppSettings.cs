using System.Text.Json;
using System.IO;
using System.Diagnostics;
using System;

namespace Whispr.Models
{
    public class AppSettings
    {
        public string PythonPath { get; set; } = string.Empty;
        public bool IsPythonInstalled { get; set; } = false;
        public int Hotkey { get; set; } = 32;
        public string AIModel { get; set; } = "openai/whisper-tiny";
        public string RecordingMode { get; set; } = "Press and hold";

        private const string SettingsFileName = "localsettings.json";
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public static AppSettings LoadOrCreate()
        {
            if (File.Exists(SettingsFileName))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFileName);
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                    return loadedSettings ?? new AppSettings();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading settings: {ex.Message}");
                    return new AppSettings();
                }
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, _jsonOptions);
                File.WriteAllText(SettingsFileName, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}