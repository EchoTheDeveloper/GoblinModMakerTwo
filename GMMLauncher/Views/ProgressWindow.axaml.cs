using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GMMBackend;
using GMMLauncher.ViewModels;

namespace GMMLauncher.Views;

public partial class ProgressWindow : Window
{
    private readonly ProgressBar? _bar;
    public ProgressWindow()
    {
        InitializeComponent();
        _bar = this.FindControl<ProgressBar>("ProgressBar");
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        WindowManager.Add(this);
    }

    public void SetProgress(double value)
    {
        _bar.Value = value;
    }
}
