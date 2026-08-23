using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;
using GMMBackend;

namespace GMMLauncher.Views;

public partial class MainWindow : Window
{
    public ModInfo? _rightClickedMod;
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this);
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        WindowManager.Add(this);
    }
    
    public void OnItemMenuClosed(object? sender, RoutedEventArgs e)
    {
        _rightClickedMod = null;
    }
    
    public void ConfigureMod()
    {
        if (_rightClickedMod == null) return;
        
        new Mod(_rightClickedMod.Path).ConfigureMod();
    }
        
    public void OpenModInExplorer()
    {
        if (_rightClickedMod == null) return;

        var filePath = _rightClickedMod.Path;
        new Mod(filePath).OpenInExplorer();
    }
        
    public void DeleteMod()
    {
        if (_rightClickedMod == null) return;
        
        new Mod(_rightClickedMod.Path).DeleteMod();
    }

    private void Mod_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(sender as Control);
        if (!pointerPoint.Properties.IsRightButtonPressed) return;
        
        if (sender is Control control && control.DataContext is ModInfo item)
        {
            _rightClickedMod = item;
            e.Handled = true;
        }
    }
}