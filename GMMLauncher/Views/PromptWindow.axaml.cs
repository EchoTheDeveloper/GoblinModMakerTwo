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

    public PromptWindow(string title, List<(Type promptType, string promptText, object? defaultValue)> prompts = null, Action<List<Control>, Window> done = null, Action<Window> cancel = null, int baseHeight = 300)
    {
        InitializeComponent();
        DataContext = new PromptWindowViewModel();
        Height = baseHeight;
        MaxHeight = 800;
        if (prompts != null)
        {
            Height += (prompts.Count * 55);
        }
        answers = new List<Control>();
        Title = title;

        var promptsPanel = this.FindControl<StackPanel>("PromptsPanel")!;
        if (prompts != null)
        {
            foreach (var (promptType, promptText, defaultValue) in prompts)
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
                    inputField.HorizontalAlignment = HorizontalAlignment.Stretch;
                    switch (inputField)
                    {
                        case TextBox textBox:
                            textBox.Width = Math.Max(Width - 200, 150);
                            
                            Resized += (sender, args) =>
                            {
                                textBox.Width = Math.Max(Width - 200, 150);
                            };

                            textBox.Text = defaultValue as string;
                            break;
                        case CheckBox checkBox:
                            checkBox.HorizontalAlignment = HorizontalAlignment.Center;
                            checkBox.VerticalAlignment = VerticalAlignment.Center;
                            checkBox.IsChecked = defaultValue as bool?;
                            break;
                        case ComboBox comboBox:
                            if (defaultValue is IEnumerable<string> items)
                            {
                                foreach (var item in items)
                                {
                                    comboBox.Items.Add(item);
                                }
                                comboBox.SelectedIndex = 0;
                            }
                            break;
                        case Button button:
                            if (defaultValue is Action)
                            {
                                button.Command = new RelayCommand(defaultValue as Action);
                            }
                            break;
                    }
                    promptsPanel.Children.Add(inputField);
                    answers.Add(inputField);
                }

                if (promptText != prompts.Last().promptText)
                {
                    promptsPanel.Children.Add(new Separator { HorizontalAlignment = HorizontalAlignment.Stretch });
                }
            }
        }

        this.FindControl<TextBlock>("TitleText")!.Text = title;
        Dispatcher.UIThread.Post(() =>
        {
            this.FindControl<Button>("Done")!.Command = new RelayCommand(() =>
            {
                done?.Invoke(answers, this);
                Close();
            });
            this.FindControl<Button>("Cancel")!.Command = new RelayCommand(() =>
            {
                cancel?.Invoke(this);
                Close();
            });
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
