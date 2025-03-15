using System.IO;
using System.Text.Json;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace GMMLauncher;

public class Settings
{
    public string SteamDirectory { get; set; } = "DEFAULT";
    public ThemeName SelectedTheme { get; set; } = ThemeName.DarkPlus;
    public bool ShowLineNumbers { get; set; } = true;

    public void LoadSettings()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
            SaveSettings();
        }
        using (Stream fileStream = new FileStream(filePath, FileMode.Open))
        {
            var settings = JsonSerializer.Deserialize<Settings>(fileStream, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });

            if (settings != null)
            {
                SteamDirectory = settings.SteamDirectory;
                SelectedTheme = settings.SelectedTheme;
                ShowLineNumbers = settings.ShowLineNumbers;
            }
        }
    }

    public void SaveSettings()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true,  });
        File.WriteAllText(filePath, json);
    }
}