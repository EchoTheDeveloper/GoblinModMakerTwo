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
        this.FindControl<TextBox>("SteamDirectory")!.Text = App.Settings.SteamDirectory;
        this.FindControl<ComboBox>("SelectTheme")!.SelectedIndex = (int)App.Settings.SelectedTheme;
        this.FindControl<CheckBox>("ShowLineNumbers")!.IsChecked = App.Settings.ShowLineNumbers;
        this.FindControl<CheckBox>("ShowExplorer")!.IsChecked = App.Settings.ShowExplorer;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}