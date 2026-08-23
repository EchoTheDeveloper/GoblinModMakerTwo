using System;
using Avalonia.Controls;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;
using System.Runtime.InteropServices;
using GMMBackend;

namespace GMMLauncher.Views;

public partial class InfoWindow : Window
{
    private readonly TextBlock infoText;
    private readonly TextBlock titleText;
    private readonly TextBlock identifierText;
    public InfoWindowType windowType { get; private set; }

    public InfoWindow(string title, InfoWindowType windowType, string startText = "", bool playSound = false, Action<Window> OkOrYes = null, Action<Window> No = null, int height = 200, int width = 300, int fontSize = 15, string okButtonText = "Ok", string yesButtonText = "Yes", string noButtontext = "No", string identifier = "")
    {
        InitializeComponent();
        DataContext = new InfoWindowViewModel();
        
        titleText = this.FindControl<TextBlock>("TitleText");
        infoText = this.FindControl<TextBlock>("InfoText");
        
        this.FindControl<TextBlock>("IdentifierText").Text = identifier;
        
        ChangeWindowType(title, windowType, startText, playSound, OkOrYes, No, height, width, fontSize, okButtonText , yesButtonText, noButtontext);
    }

    public void ChangeWindowType(string title, InfoWindowType newWindowType, string newText = "", bool playSound = false, Action<Window> OkOrYes = null, Action<Window> No = null, int height = 200, int width = 300, int fontSize = 15, string okButtonText = "Ok", string yesButtonText = "Yes", string noButtontext = "No")
    {
        Height = height;
        Width = width;
        Title = title;
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
                    Content = okButtonText,
                    Command = new RelayCommand(() =>
                    {
                        if (OkOrYes != null) OkOrYes?.Invoke(this);
                        else Close();
                    }),
                };
                buttonPanel.Children.Add(okButton);
                break;
            case InfoWindowType.YesNo:
                Button yesButton = new Button
                {
                    Content = yesButtonText,
                    Command = new RelayCommand(() =>
                    {
                        if (OkOrYes != null) OkOrYes?.Invoke(this);
                        else Close();
                    }),
                };
                buttonPanel.Children.Add(yesButton);
                Button noButton = new Button
                {
                    Content = noButtontext,
                    Command = new RelayCommand(() =>
                    {
                        if (No != null) No?.Invoke(this);
                        else Close();
                    }),
                };
                buttonPanel.Children.Add(noButton);
                break;
            case InfoWindowType.Error:
                Button errorButton = new Button
                {
                    Content = okButtonText,
                    Command = new RelayCommand(() =>
                    {
                        if (OkOrYes != null) OkOrYes?.Invoke(this);
                        else Close();
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
        Topmost = true;
    }
    public void UpdateInfoText(string text)
    {
        infoText.Text = text;
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        WindowManager.Add(this);
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