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

    public PromptWindow(string title, List<(Type promptType, string promptText, object? defaultValue, bool required)> prompts = null, Action<List<Control>, Window> done = null, Action<Window> cancel = null, int baseHeight = 300, string cancelText = "Cancel", PixelPoint? postion = null)
    {
        InitializeComponent();
        DataContext = new PromptWindowViewModel();
        Height = baseHeight;
        this.FindControl<Button>("Cancel").Content = cancelText;
        if (postion.HasValue)
        {
            Position = postion.Value;
        }
        if (prompts != null)
        {
            Height += (prompts.Count * 55);
        }
        answers = new List<Control>();
        Title = title;
        var requiredFields = new List<TextBox>();

        var promptsPanel = this.FindControl<StackPanel>("PromptsPanel")!;
        if (prompts != null)
        {
            foreach (var (promptType, promptText, defaultValue, required ) in prompts)
            {
                
                if (promptType != typeof(Button) && promptText != "")
                {
                    promptsPanel.Children.Add(new TextBlock
                    {
                        Text = promptText,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 14,
                        
                    });
                }

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
                            if (required) requiredFields.Add(textBox);
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
                            button.HorizontalAlignment = HorizontalAlignment.Center;
                            button.Content = promptText;
                            break;
                    }
                    promptsPanel.Children.Add(inputField);
                    answers.Add(inputField);
                }

                if (promptText != prompts.Last().promptText && promptType != typeof(Button))
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
                foreach (TextBox box in requiredFields)
                {
                    if (String.IsNullOrEmpty(box.Text?.Trim()))
                    {
                        new InfoWindow("Field Empty", InfoWindowType.Error, $"One or multiple fields left empty.", true, fontSize:20).Show();
                        return;
                    }
                }

                done.Invoke(answers, this);
                // Close();
            });
            this.FindControl<Button>("Cancel")!.Command = new RelayCommand(() =>
            {
                cancel?.Invoke(this);
                Close();
            });
        });
        
        if (Height > 800) Height = 800;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
