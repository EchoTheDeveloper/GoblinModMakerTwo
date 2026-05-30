using Avalonia.Controls;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;
using GMMBackend;

namespace GMMLauncher.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(CodeEditor editor = null)
    {
        InitializeComponent();
        DataContext = new SettingsWindowViewModel(this, editor);
        WindowManager.Add(this);
        this.FindControl<TextBox>("SteamDirectory")!.Text = App.Settings.SteamDirectory;
        this.FindControl<ComboBox>("SelectTheme")!.SelectedIndex = (int)App.Settings.SelectedTheme;
        this.FindControl<CheckBox>("ShowLineNumbers")!.IsChecked = App.Settings.ShowLineNumbers;
        this.FindControl<CheckBox>("ShowExplorer")!.IsChecked = App.Settings.ShowExplorer;
        this.FindControl<CheckBox>("OverwriteCsproj")!.IsChecked = App.Settings.OverwriteCsproj;
        this.FindControl<CheckBox>("ZipMod")!.IsChecked = App.Settings.ZipMod;
        this.FindControl<CheckBox>("OpenPluginFolder")!.IsChecked = App.Settings.OpenPluginFolder;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}