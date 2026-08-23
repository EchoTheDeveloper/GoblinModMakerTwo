using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GMMLauncher.Views;
using Microsoft.Win32;

namespace GMMLauncher;

public class Settings
{
    public string SteamDirectory { get; set; } = "C:\\Program Files (x86)\\Steam\\steamapps\\common";
    public string SelectedTheme { get; set; } = "Gobliny";
    public bool ShowLineNumbers { get; set; } = true;
    public bool ShowExplorer { get; set; } = true;
    public bool OverwriteCsproj { get; set; } = true;
    public bool ZipMod { get; set; }
    public bool OpenPluginFolder { get; set; }
    
    private string _opacityAmount = "0.65";
    public string OpacityAmount
    {
        get => _opacityAmount;
        set
        {
            if (_opacityAmount == value)
                return;

            _opacityAmount = value;
            App.Instance.SetResource("OpacityAmount", double.Parse(_opacityAmount));
        }
    }

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
                ShowExplorer = settings.ShowExplorer;
                OverwriteCsproj = settings.OverwriteCsproj;
                ZipMod = settings.ZipMod;
                OpenPluginFolder = settings.OpenPluginFolder;
                OpacityAmount = settings.OpacityAmount;
            }
        }
    }
    public void SaveSettings()
    {
        string filePath = Path.Combine(GMMBackend.Utils.GetAppDataPath(), "settings.json");
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true,  });
        File.WriteAllText(filePath, json);
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
    
    #region Update Service

    #region GMM Installation

    public async Task<(bool, string)> GMMUpdateAvailable()
    {
        string version = await GetDefaultStableVersionAsync("https://api.github.com/repos/EchoTheDeveloper/GoblinModMakerTwo/releases");
        return (!App.appVersion.Equals(version), version);
    }
    #endregion
    
    #region BepInEx Installation
    public async Task<(bool, string?, string)> BepInExUpdateAvailable()
    {
        string version = await GetDefaultStableVersionAsync("https://api.github.com/repos/BepInEx/BepInEx/releases");
        string? currentVersion = RetrieveCurrentBepInExVersion();
        
        return (!currentVersion.Equals(version.Replace("v", "")), currentVersion, version);
    }

    public string? RetrieveCurrentBepInExVersion()
    {
        string dllPath = Path.Combine(SteamDirectory, "BepInEx", "core", "BepInEx.dll");
        
        if (!File.Exists(dllPath))
            return null;
        
        return FileVersionInfo.GetVersionInfo(dllPath).FileVersion;
    }

    public async Task InstallBepInEx(bool bypass = false)
    {
        var window = new InfoWindow("Installing BepInEx", InfoWindowType.Info, "Checking folders for BepInEx", true);
        window.Show();

        string bepInFolder = Path.Combine(SteamDirectory, "BepInEx");
        string pluginsFolder = Path.Combine(bepInFolder, "plugins");
        string tempPluginsFolder = Path.Combine(SteamDirectory, "temp_plugins");
        if (!bypass && Directory.Exists(bepInFolder))
        {
            if (Directory.Exists(pluginsFolder)) // could check for something like cache or config instead
            {
                window.ChangeWindowType("Already Installed", InfoWindowType.YesNo, "BepInEx is already installed. Are you trying to reinstall?", true, 
                    (window) =>
                    {
                        PrepBepInExFolders();
                        _ = InstallBepInEx();
                        window.Close();
                    });
            }
            else
            {
                window.ChangeWindowType("Partially Installed", InfoWindowType.YesNo, "BepInEx is partially installed. Please run Isle Goblin once.", true);
            }
        }
        else
        {
            if (Directory.Exists(bepInFolder))
            {
                PrepBepInExFolders();
            }
            
            window.UpdateInfoText("Getting new BepInEx version information");
            string version = await GetDefaultStableVersionAsync("https://api.github.com/repos/BepInEx/BepInEx/releases");
            string link = await GetBepInExDownloadLink(version);
            if (string.IsNullOrEmpty(link))
            {
                window.ChangeWindowType("Error Fetching Update Link", InfoWindowType.Ok, $"Unable to retrieve a working BepInEx release link :(", true);
            }
            
    
            if (Directory.Exists(SteamDirectory))
            {
                window.UpdateInfoText("Installing new BepInEx version");
                string src = Path.Combine(Directory.GetCurrentDirectory(), "resources\\BepInEx.zip");
                await DownloadFileAsync(link, src, "BepInEx");
                ZipFile.ExtractToDirectory(src, SteamDirectory);
                File.Delete(Path.Combine(SteamDirectory, ".doorstop_version"));
                if (Directory.Exists(tempPluginsFolder))
                {
                    window.UpdateInfoText("Moving plugins back into BepInEx");
                    Directory.Move(tempPluginsFolder, pluginsFolder);
                }
                window.ChangeWindowType("BepInEx Installed", InfoWindowType.Ok, "BepInEx was successfully installed! Please run Isle Goblin once, then exit. ", true, fontSize:20);
            }
            else
            {
                window.ChangeWindowType("Error Installing BepInEx", InfoWindowType.Ok, $"The current Steam directory ({SteamDirectory}) does not exist", true);
            }
        }

        void PrepBepInExFolders()
        {
            window.UpdateInfoText("Cleaning previous installation of BepInEx (plugins are saved)");
            Directory.Move(pluginsFolder, tempPluginsFolder);
            Directory.Delete(Path.Combine(SteamDirectory, "BepInEx"), true);
            File.Delete(Path.Combine(SteamDirectory, "doorstop_config.ini"));
            File.Delete(Path.Combine(SteamDirectory, "winhttp.dll"));
            File.Delete(Path.Combine(SteamDirectory, "changelog.txt"));
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
    #endregion
    
    #region Goblin Manager
    
    #endregion
    public async Task<string> GetDefaultStableVersionAsync(string url)
    {
        HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        
        List<string> releases = new List<string>();
        
        while (!url.Equals(""))
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Settings");

            HttpResponseMessage response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                break;
            }
            
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
                url = "";
            }
            
        }
        
        if (releases.Count < 1) return "";
        

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
            return "";
        }
    }
    
    public static async Task DownloadFileAsync(string url, string destinationPath, string name)
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
            new InfoWindow($"Error Downloading {name}", InfoWindowType.Error, ex.Message, true, fontSize:20).Show();
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

