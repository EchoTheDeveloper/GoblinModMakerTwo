using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;

namespace GMMLauncher.Views;

public partial class PromptWindow : Window
{
    public PromptWindow(string title, string[] prompts, Action<List<TextBox>, Window> done, Action<Window> cancel)
    {
        InitializeComponent();
        DataContext = new PromptWindowViewModel();
        Height = 300 + (prompts.Length * 40);
        Title = title;
        List<TextBox> answers = new();
        foreach (var t in prompts)
        {
            this.FindControl<StackPanel>("PromptsPanel")!.Children.Add(new TextBlock
            {
                Text = t,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 14,
            });
            TextBox textBox = new();
            this.FindControl<StackPanel>("PromptsPanel")!.Children.Add(textBox);
            answers.Add(textBox);

            if (t != prompts.Last())
            {
                this.FindControl<StackPanel>("PromptsPanel")!.Children.Add(new Separator
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
        }
        
        this.FindControl<TextBlock>("TitleText")!.Text = title;
        Dispatcher.UIThread.Post(() =>
        {
            this.FindControl<Button>("Done")!.Command = new RelayCommand(() => done?.Invoke(answers, this));
            this.FindControl<Button>("Cancel")!.Command = new RelayCommand(() => cancel?.Invoke(this));
        });

    }

    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}