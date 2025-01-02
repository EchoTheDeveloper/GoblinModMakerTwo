using Avalonia.Controls;
using GMMLauncher.ViewModels;

namespace GMMLauncher.Views;

public partial class CodeEditor : Window
{
    public CodeEditor()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}