using Avalonia.Controls;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;

namespace GMMLauncher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this);
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}