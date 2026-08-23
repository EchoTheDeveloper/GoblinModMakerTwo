using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;

namespace GMMLauncher.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(CodeEditor editor = null)
    {
        InitializeComponent();
        DataContext = new SettingsWindowViewModel(this, editor);
        PopulateThemeBox();
        this.FindControl<TextBox>("SteamDirectory")!.Text = App.Settings.SteamDirectory;
        this.FindControl<ComboBox>("SelectTheme")!.SelectedValue = App.Settings.SelectedTheme;
        this.FindControl<CheckBox>("ShowLineNumbers")!.IsChecked = App.Settings.ShowLineNumbers;
        this.FindControl<CheckBox>("ShowExplorer")!.IsChecked = App.Settings.ShowExplorer;
        this.FindControl<CheckBox>("OverwriteCsproj")!.IsChecked = App.Settings.OverwriteCsproj;
        this.FindControl<CheckBox>("ZipMod")!.IsChecked = App.Settings.ZipMod;
        this.FindControl<CheckBox>("OpenPluginFolder")!.IsChecked = App.Settings.OpenPluginFolder;
        this.FindControl<NumericUpDown>("OpacityAmount")!.Text = App.Settings.OpacityAmount;
    }

    public void PopulateThemeBox()
    {
        var comboBox = this.FindControl<ComboBox>("SelectTheme");
        if (comboBox == null)
            return;

        comboBox.Items.Clear();

        foreach (var theme in FetchAllThemes())
        {
            comboBox.Items.Add(theme);
        }
    }

    private List<string> FetchAllThemes()
    {
        var files = Directory.GetFiles(Path.Combine("Resources", "Themes"));
        List<string> themeNames = new List<string>();
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            themeNames.Add(fileName);
        }
        return themeNames;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        WindowManager.Add(this);
    }
}