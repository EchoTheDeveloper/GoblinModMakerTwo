using Avalonia.Controls;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;
using GMMBackend;

namespace GMMLauncher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this);
        WindowManager.Add(this);
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}