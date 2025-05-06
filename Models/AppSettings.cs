using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MusicSyncDesktop
{
    public class AppSettings
    {
        private readonly string _settingsFilePath = "appsettings.json";
        private JsonElement _root;

        public AppSettings()
        {
            Load();
        }

        public string Get(string sectionAndKey)
        {
            var (section, key) = Split(sectionAndKey);

            if (_root.TryGetProperty(section, out var sectionElement) &&
                sectionElement.ValueKind == JsonValueKind.Object &&
                sectionElement.TryGetProperty(key, out var valueElement))
            {
                return valueElement.GetString();
            }

            return null;
        }

        public void Update(string sectionAndKey, string value)
        {
            var json = File.ReadAllText(_settingsFilePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

            var (section, key) = Split(sectionAndKey);

            if (!dict.ContainsKey(section))
                dict[section] = new Dictionary<string, string>();

            dict[section][key] = value;

            var newJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, newJson);

            Load(); // перезагружаем дерево
        }

        private void Load()
        {
            if (!File.Exists(_settingsFilePath))
                throw new FileNotFoundException("appsettings.json not found.");

            var json = File.ReadAllText(_settingsFilePath);
            using var doc = JsonDocument.Parse(json);
            _root = doc.RootElement.Clone(); // сохранить копию
        }

        private (string section, string key) Split(string sectionAndKey)
        {
            var parts = sectionAndKey.Split(':', 2);
            if (parts.Length != 2)
                throw new ArgumentException("Key must be in format Section:Key");
            return (parts[0], parts[1]);
        }
    }
}
