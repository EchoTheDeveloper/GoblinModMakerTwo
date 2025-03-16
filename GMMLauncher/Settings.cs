using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace GMMLauncher;

public class Settings
{
    public string SteamDirectory { get; set; } = "C:\\Program Files (x86)\\Steam\\steamapps\\common";
    public ThemeName SelectedTheme { get; set; } = ThemeName.DarkPlus;
    public bool ShowLineNumbers { get; set; } = true;

    public void LoadSettings()
    {
        string filePath = Path.Combine(GMMBackend.Utils.GetAppDataPath(), "settings.json");
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
            SteamDirectory = FindSteamDirectory();
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
    
    public string FindSteamDirectory()
    {
        string folderName = "Isle Goblin Playtest";
        string steamPath = TryGetSteamDirectory();
        if (!string.IsNullOrEmpty(steamPath))
        {
            string steamCommonPath = Path.Combine(steamPath, "steamapps", "common", folderName);
            if (Directory.Exists(Path.Combine(steamCommonPath))) // TODO: WHEN MAKING ALL AROUND MOD MAKER MAKE THIS ADJUSTABLE
            {
                return steamCommonPath;
            }
        }
        string[] commonDrives = { "C:", "D:", "E:", "F:", "Z:" };
        foreach (string drive in commonDrives)
        {
            string possiblePath = Path.Combine(drive, "SteamLibrary", "steamapps", "common", folderName);
            if (Directory.Exists(Path.Combine(possiblePath)))
            {
                return possiblePath;
            }

            string programFilesPath = Path.Combine(drive, "Program Files", "Steam", "steamapps", "common", folderName);
            if (Directory.Exists(programFilesPath))
            {
                return programFilesPath;
            }

            string programFilesX86Path = Path.Combine(drive, "Program Files (x86)", "Steam", "steamapps", "common", folderName);
            if (Directory.Exists(programFilesX86Path))
            {
                return programFilesX86Path;
            }
        }
        
        return null;
    }

    private string TryGetSteamDirectory()
    {
        try
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
            {
                return key?.GetValue("InstallPath") as string;
            }
        }
        catch
        {
            return null;
        }
    }

    public void SaveSettings()
    {
        string filePath = Path.Combine(GMMBackend.Utils.GetAppDataPath(), "settings.json");
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true,  });
        File.WriteAllText(filePath, json);
    }
}