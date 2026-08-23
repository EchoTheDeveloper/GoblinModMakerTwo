using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Controls;

namespace GMMLauncher;

public class RecentProjects
{
    public ObservableCollection<ModInfo> RecentProjectList { get; private set; } = new();
    public string FilePath => Path.Combine(GMMBackend.Utils.GetAppDataPath(), "recent.json");

    
    public void LoadRecentProjects()
    {
        if (!File.Exists(FilePath))
        {
            File.Create(FilePath).Close();
            SaveRecents();
        }
        using (Stream fileStream = new FileStream(FilePath, FileMode.Open))
        {
            var recentProjects = JsonSerializer.Deserialize<RecentProjectsHolder>(fileStream, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
            if (recentProjects != null)
            {
                RecentProjectList = recentProjects.RecentProjectList;
            }
        }
        VerifyProjects();
    }
    public void VerifyProjects()
    {
        var list = RecentProjectList
            .Where(x => Path.Exists(x.Path))
            .GroupBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.MaxBy(x => x.LastOpened)!)
            .OrderByDescending(x => x.LastOpened)
            .ToList();
        RecentProjectList.Clear();
        foreach (var item in list)
        {
            RecentProjectList.Add(item);
        }
        SaveRecents();
    }

    public void AddRecentProject(ModInfo modInfo)
    {
        RecentProjectList.Add(modInfo);
        VerifyProjects();
        SaveRecents();
    }
    public void SaveRecents()
    {
        var data = new RecentProjectsHolder
        {
            RecentProjectList = RecentProjectList,
        };
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true,  });
        File.WriteAllText(FilePath, json);
    }

}

public class RecentProjectsHolder
{
    public ObservableCollection<ModInfo> RecentProjectList { get; set; } = new();
}

public class ModInfo
{
    public string Name { get; set; }
    public DateTime LastOpened { get; set; }
    public string Path { get; set; }

    public void OpenCommand()
    {
        MenuCommands.LoadModFromFile(filePath: Path);
    }
}