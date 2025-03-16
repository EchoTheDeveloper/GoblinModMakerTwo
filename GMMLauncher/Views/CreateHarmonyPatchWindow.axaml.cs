using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GMMBackend;

namespace GMMLauncher.Views;

public partial class CreateHarmonyPatch : Window
{
    public CreateHarmonyPatch(CodeEditor editor = null)
    {
        InitializeComponent();
        DataContext = new CreateHarmonyPatchViewModel(this, editor);
        this.FindControl<TextBox>("SteamDirectory").Text = App.Settings.SteamDirectory;
        this.FindControl<ComboBox>("SelectTheme").SelectedIndex = (int)App.Settings.SelectedTheme; // ADD THE ITEMS
        this.FindControl<CheckBox>("ShowLineNumbers").IsChecked = App.Settings.ShowLineNumbers;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}