using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace GMMLauncher.Views;

public partial class PromptWindow : Window
{
    public List<Control> answers;
    public PromptWindow(string title, List<(Type promptType, string promptText)> prompts = null, Action<List<Control>, Window> done = null, Action<Window> cancel = null, int baseHeight = 300)
    {
        InitializeComponent();
        DataContext = new PromptWindowViewModel();
        Height = baseHeight;
        if (prompts != null)
        {
            Height += (prompts.Count * 65);
        }
        answers= new List<Control>();
        Title = title;
    
        var promptsPanel = this.FindControl<StackPanel>("PromptsPanel")!;
        if (prompts != null)
        {
            foreach (var (promptType, promptText) in prompts)
            {
                promptsPanel.Children.Add(new TextBlock
                {
                    Text = promptText,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 14,
                });
    
                Control? inputField = null;
    
                if (promptType != typeof(TextBlock))
                {
                    try
                    {
                        inputField = Activator.CreateInstance(promptType) as Control;
                    }
                    catch
                    {
                        inputField = new TextBox();
                    }
                }
    
                if (inputField != null)
                {
                    promptsPanel.Children.Add(inputField);
                    answers.Add(inputField);
                    inputField.HorizontalAlignment = HorizontalAlignment.Center;
                }
    
                if (promptText != prompts.Last().promptText)
                {
                    promptsPanel.Children.Add(new Separator { HorizontalAlignment = HorizontalAlignment.Center });
                }
            }
        }
    
        this.FindControl<TextBlock>("TitleText")!.Text = title;
        Dispatcher.UIThread.Post(() =>
        {
            this.FindControl<Button>("Done")!.Command = new RelayCommand(() =>
            {
                if (done != null)
                    done?.Invoke(answers, this);
                else
                    Close();
            });
            this.FindControl<Button>("Cancel")!.Command = new RelayCommand(() =>
            {
                if (cancel != null)
                    cancel.Invoke(this);
                else
                    Close();
            });
        });
        
    }

    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}