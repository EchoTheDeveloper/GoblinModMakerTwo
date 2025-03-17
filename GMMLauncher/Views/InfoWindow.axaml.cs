using System;
using Avalonia.Controls;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;
using System.Runtime.InteropServices;
using Avalonia.Layout;

namespace GMMLauncher.Views;

public partial class InfoWindow : Window
{
    private readonly TextBlock infoText;
    private readonly TextBlock titleText;
    public InfoWindowType windowType { get; private set; }

    public InfoWindow(string title, InfoWindowType windowType, string startText = "", bool playSound = false, Action OkOrYes = null, Action No = null, int height = 200, int width = 300, int fontSize = 15)
    {
        InitializeComponent();
        DataContext = new InfoWindowViewModel();
        
        titleText = this.FindControl<TextBlock>("TitleText");
        infoText = this.FindControl<TextBlock>("InfoText");
        
        ChangeWindowType(title, windowType, startText, playSound, OkOrYes, No, height, width, fontSize);
    }

    public void ChangeWindowType(string title, InfoWindowType newWindowType, string newText = "", bool playSound = false, Action OkOrYes = null, Action No = null, int height = 200, int width = 300, int fontSize = 15)
    {
        Height = height;
        Width = width;
        Title = title;
        Topmost = true;
        titleText.Text = title;
        infoText.Text = newText;
        infoText.FontSize = fontSize;
        windowType = newWindowType;
        
        var buttonPanel = this.FindControl<StackPanel>("ButtonPanel");
        
        buttonPanel.Children.Clear();
        switch (windowType)
        {
            case InfoWindowType.Ok:
                Button okButton = new Button
                {
                    Content = "Ok",
                    Command = new RelayCommand(() =>
                    {
                        OkOrYes?.Invoke();
                        Close();
                    }),
                };
                buttonPanel.Children.Add(okButton);
                break;
            case InfoWindowType.YesNo:
                Button noButton = new Button
                {
                    Content = "No",
                    Command = new RelayCommand(() =>
                    {
                        No?.Invoke();
                        Close();
                    }),
                };
                buttonPanel.Children.Add(noButton);
                Button yesButton = new Button
                {
                    Content = "Yes",
                    Command = new RelayCommand(() =>
                    {
                        OkOrYes?.Invoke();
                        Close();
                    }),
                };
                buttonPanel.Children.Add(yesButton);
                break;
            case InfoWindowType.Error:
                Button errorButton = new Button
                {
                    Content = "Ok",
                    Command = new RelayCommand(() =>
                    {
                        OkOrYes?.Invoke();
                        Close();
                    }),
                };
                buttonPanel.Children.Add(errorButton);
                break; 
        }

        if (playSound)
        {
            if (windowType == InfoWindowType.Error)
            {
                SystemSoundPlayer.PlayErrorSound();
            }
            else
            {
                SystemSoundPlayer.PlayInfoSound();
            }
        }
    }
    public void UpdateInfoText(string text)
    {
        infoText.Text = text;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public enum InfoWindowType
{
    Info,
    YesNo,
    Ok,
    Error
}

public static class SystemSoundPlayer
{
    [DllImport("user32.dll")]
    public static extern bool MessageBeep(uint uType);

    public static void PlayErrorSound() => MessageBeep(0x10);
    public static void PlayInfoSound() => MessageBeep(0x40);
}