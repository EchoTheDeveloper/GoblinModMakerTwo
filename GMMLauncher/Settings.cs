using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GMMLauncher.Views;
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
    #region BepInEx Installation
    public async Task InstallBepInEx()
    {
        if (Directory.Exists(Path.Combine(SteamDirectory, "BepInEx")))
        {
            new InfoWindow("Already Installed", InfoWindowType.YesNo, "BepInEx is already installed. Are you trying to reinstall?", true, 
                () =>
                {
                    Directory.Delete(Path.Combine(SteamDirectory, "BepInEx"), true);
                    File.Delete(Path.Combine(SteamDirectory, "doorstop_config.ini"));
                    File.Delete(Path.Combine(SteamDirectory, "winhttp.dll"));
                    File.Delete(Path.Combine(SteamDirectory, "changelog.txt"));
                    InstallBepInEx();
                },
                fontSize:18).Show();
        }
        else
        {
            string version = await GetDefaultStableVersionAsync();
            string link = await GetBepInExDownloadLink(version);
            if (string.IsNullOrEmpty(link)) return;
            
    
            if (Directory.Exists(SteamDirectory))
            {
                string src = Path.Combine(Directory.GetCurrentDirectory(), "resources\\BepInEx.zip");
                await DownloadFileAsync(link, src);
                ZipFile.ExtractToDirectory(src, SteamDirectory);
                File.Delete(Path.Combine(SteamDirectory, ".doorstop_version"));
            }
            new InfoWindow("BepInEx Installed", InfoWindowType.Ok, "BepInEx was successfully installed! Please run Isle Goblin once, then exit. ", true, fontSize:20).Show();
        }
    }
    
    public async Task<string> GetDefaultStableVersionAsync()
    {
        HttpClient httpClient = new();
        List<string> releases = new List<string>();
        string url = "https://api.github.com/repos/BepInEx/BepInEx/releases";

        while (url != null)
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Settings");

                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var releasesData = JsonSerializer.Deserialize<List<Release>>(json);
                    foreach (var release in releasesData)
                    {
                        releases.Add(release.tag_name);
                    }

                    if (response.Headers.Contains("Link"))
                    {
                        string nextUrl = GetNextPageUrl(response.Headers.GetValues("Link"));
                        url = nextUrl;
                    }
                    else
                    {
                        url = null;
                    }
                }
            
        }

        var stableVersions = releases.FindAll(version => !version.Contains("pre") && !version.Contains("RC"));

        return stableVersions.Count > 0 ? stableVersions[0] : releases[0];
        string GetNextPageUrl(IEnumerable<string> linkHeader)
        {
            foreach (var link in linkHeader)
            {
                if (link.Contains("rel=\"next\""))
                {
                    var url = link.Split(';')[0].Trim('<', '>');
                    return url;
                }
            }
            return null;
        }
    }
    

    public static async Task DownloadFileAsync(string url, string destinationPath)
    {
        HttpClient httpClient = new();
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using FileStream fileStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using Stream contentStream = await response.Content.ReadAsStreamAsync();
            await contentStream.CopyToAsync(fileStream);
        }
        catch (Exception ex)
        {
            new InfoWindow("Error Downloading BepInEx", InfoWindowType.Error, ex.Message, true, fontSize:20).Show();

        }
    }
    
    private async Task<string> GetBepInExDownloadLink(string version)
    {
        string baseUrl = $"https://github.com/BepInEx/BepInEx/releases/download/{version}/BepInEx";
        string os = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier.Split("-")[0];
        string arch = GetSystemArch();
        if (os == "osx") os = "macos";

        string[] urls = PossibleUrls();
        
        foreach (string url in urls)
        {
            if (await UrlExistsAsync(url))
            {
                return url;
            }
        }

        var infoWindow = new InfoWindow("Error Getting Url", InfoWindowType.Error, "Error while getting BepInEx URL.", true); // TODO: EVENTUALLY MAKE SOMETHING BETTER FOR THIS
        infoWindow.Show();
        return null;

        string[] PossibleUrls()
        {
            string nov = version.Replace("v", "");
            return 
            [
                $"{baseUrl}_{os}_{arch}_{nov}.zip",
                $"{baseUrl}_{os}_{arch}_{nov}.0.zip",
                $"{baseUrl}_{os}_{arch}_{nov}.0.0.zip",
                $"{baseUrl}_{arch}_{nov}.zip",
                $"{baseUrl}_{arch}_{nov}.0.zip",
                $"{baseUrl}_{arch}_{nov}.0.0.zip",
                $"{baseUrl}_{nov}.zip",
            ];
        }
        string GetSystemArch()
        {
            string arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
            if (arch.Contains("64"))
            {
                return "X64";
            }
            return "X86";
        }
    }
    private static async Task<bool> UrlExistsAsync(string url)
    {
        HttpClient httpClient = new();
        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
}
public class Release
{
    public string tag_name { get; set; }
}
