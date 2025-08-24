using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using GMMLauncher.ViewModels;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using GMMBackend;

namespace GMMLauncher.Views;

public partial class PromptWindow : Window
{
    private readonly List<TextBox> requiredFields = [];
    public readonly List<Control> answers = [];
    
    public PromptWindow(string title, List<(Type promptType, string promptText, object? defaultValue, bool required)>? prompts = null, Action<List<Control>, Window>? done = null, Action<Window>? cancel = null, int baseHeight = 300, string cancelText = "Cancel")
    {
        InitializeComponent();
        WindowManager.Add(this);
        Height = baseHeight;
        this.FindControl<Button>("Cancel")!.Content = cancelText;
        if (prompts != null)
        {
            Height += (prompts.Count * 55);
        }
        Title = title;

        var promptsPanel = this.FindControl<StackPanel>("PromptsPanel")!;
        if (prompts != null)
        {
            foreach (var (promptType, promptText, defaultValue, required ) in prompts)
            {
                Control? inputField;

                try
                {
                    inputField = Activator.CreateInstance(promptType) as Control;
                }
                catch
                {
                    inputField = new TextBox();
                }


                if (inputField != null)
                {
                    inputField.Tag = promptText.Trim();
                    inputField.Margin = new Thickness(5);
                    inputField.HorizontalAlignment = HorizontalAlignment.Stretch;
                    
                    AddInputField(promptsPanel, inputField, (promptType, promptText, defaultValue, required));
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
                if (requiredFields.Any(box => string.IsNullOrEmpty(box.Text?.Trim())))
                {
                    new InfoWindow("Field Empty", InfoWindowType.Error, $"One or multiple fields left empty.", true, fontSize:20).Show();
                    return;
                }

                done?.Invoke(answers, this);
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


    private void AddInputField(StackPanel panel, Control inputField, (Type promptType, string promptText, object? defaultValue, bool required) prompt)
    {
        if (prompt.promptType != typeof(Button) && prompt.promptText != "")
        {
            var updatedText = prompt.promptText;
            if (prompt.required)
            {
                updatedText += '*';
            }
            panel.Children.Add(new TextBlock
            {
                Text = updatedText,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 14,
                        
            });
        }

        if (prompt.promptType == typeof(TextBlock)) return;
        
        if (inputField is not Expander)
        {
            panel.Children.Add(inputField);
            answers.Add(inputField);
        }
        switch (inputField)
        {
            case TextBox textBox:
                textBox.Text = prompt.defaultValue as string;
                if (prompt.required)
                {
                    requiredFields.Add(textBox);
                }
                textBox.Width = Math.Max(Width - 200, 150);
                            
                Resized += (_, _) =>
                {
                    textBox.Width = Math.Max(Width - 200, 150);
                };
                break;
            case CheckBox checkBox:
                checkBox.HorizontalAlignment = HorizontalAlignment.Center;
                checkBox.VerticalAlignment = VerticalAlignment.Center;
                checkBox.IsChecked = prompt.defaultValue as bool?;
                break;
            case ComboBox comboBox:
                string selectedValue = "";
                if (prompt.defaultValue is (string v, List<(string choice, Type, string labelText, string defaultValue, bool required)> itemsWithOnSelected))
                {
                    foreach (var item in itemsWithOnSelected)
                    {
                        comboBox.Items.Add(item.choice);
                        selectedValue = v;
                        var textBox = new TextBox
                        {
                            Name = inputField.Name,
                            Text = item.defaultValue,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        var textBlock = new TextBlock
                        {
                            Name = inputField.Name,
                            Text = item.labelText,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 14,
                        };
                        if (item.Item2 != null!)
                        {
                            comboBox.SelectionChanged += (_, _) =>
                            {
                                if (!string.IsNullOrEmpty(item.labelText))
                                {
                                    textBox.Tag = item.labelText.Trim();
                                    panel.Children.Remove(textBlock);
                                }
                                else
                                {
                                    textBox.Tag = prompt.promptText.Trim();
                                }

                                panel.Children.Remove(textBox);
                                requiredFields.Remove(textBox);
                                answers.Remove(textBox);


                                if ((string)comboBox.SelectedItem! == item.choice)
                                {
                                    if (item.required)
                                    {
                                        requiredFields.Add(textBox);
                                        if (!textBlock.Text.EndsWith('*'))
                                            textBlock.Text += '*';
                                    }

                                    var inputFieldIndex = panel.Children.IndexOf(inputField);
                                    if (!string.IsNullOrEmpty(item.labelText))
                                    {
                                        panel.Children.Insert(inputFieldIndex+1, textBlock);
                                        panel.Children.Insert(inputFieldIndex+2, textBox);
                                        var comboBoxIndex = answers.IndexOf(comboBox);
                                        answers.Insert(comboBoxIndex+1, textBox);
                                    }
                                    else
                                    {
                                        panel.Children.Insert(inputFieldIndex+1, textBox);
                                        answers.Add(textBox);
                                    }
                                }
                            };
                        }
                    }
                }
                else if (prompt.defaultValue is (string _v, List<string> items))
                {
                    selectedValue = _v;
                    foreach (var item in items)
                    {
                        comboBox.Items.Add(item);
                    }
                }

                comboBox.SelectedValue = comboBox.Items.Contains(selectedValue) ? selectedValue : "Other";
                break;
            case Button button:
                if (prompt.defaultValue is Action action)
                {
                    button.Command = new RelayCommand(action);
                }
                button.HorizontalAlignment = HorizontalAlignment.Center;
                button.Content = prompt.promptText;
                break;
            case Expander expander:
                if (prompt.defaultValue is IEnumerable<(Type innerType, string innerText, object? innerDefault, bool innerRequired)> expanderItems)
                {
                    expander = new Expander
                    {
                        Header = prompt.promptText,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
            
                    var expanderPanel = new StackPanel();
            
                    foreach (var (innerType, innerText, innerDefault, innerRequired) in expanderItems)
                    {
            
                        Control? innerInputField;
            
                        try
                        {
                            innerInputField = Activator.CreateInstance(innerType) as Control;
                        }
                        catch
                        {
                            innerInputField = new TextBox();
                        }
            
                        if (innerInputField != null)
                        {
                            innerInputField.Name = prompt.promptText;
                            innerInputField.Tag = innerText;
                            innerInputField.HorizontalAlignment = HorizontalAlignment.Center;
                            innerInputField.Margin = new Thickness(5);

                            if (innerInputField is CheckBox)
                            {
                                var horizontalPanel = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 10, 0, 2)
                                };

                                horizontalPanel.Children.Add(new TextBlock
                                {
                                    Text = innerText,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    FontSize = 14,
                                    Margin = new Thickness(0, 0, 5, 0)
                                });

                                answers.Add(innerInputField);
                                horizontalPanel.Children.Add(innerInputField);
                                expanderPanel.Children.Add(horizontalPanel);
                            }
                            else
                            {
                                AddInputField(expanderPanel, innerInputField, (innerType, innerText, innerDefault, innerRequired));
                            }
                        }
                    }
            
                    expander.Content = expanderPanel;
                    panel.Children.Add(expander);
                }
                break;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
