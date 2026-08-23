using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;

namespace GMMLauncher.Views;

public partial class UpdateWindow : Window
{
    public UpdateWindow()
    {
        InitializeComponent();
        DataContext = new UpdateWindowViewModel(this);
        
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        WindowManager.Add(this);
    }
}