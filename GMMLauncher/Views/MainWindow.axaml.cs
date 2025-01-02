using Avalonia.Controls;
using GMMLauncher.ViewModels;

namespace GMMLauncher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}