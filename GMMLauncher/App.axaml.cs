using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using GMMLauncher.ViewModels;
using GMMLauncher.Views;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Themes.Reader;

namespace GMMLauncher
{
    public partial class App : Application
    {
        public static App Instance { get; set; }
        public static Settings Settings { get; set; }
        public static RecentProjects RecentProjects { get; set; }
        public static string? appVersion { get; private set; }
        public static ObservableCollection<AssemblyItem> DecompiledTree { get; set; }
        public override void Initialize()
        {
            Instance = this;
            appVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            AvaloniaXamlLoader.Load(this);
            
            Settings = new Settings();
            Settings.LoadSettings();
            
            RecentProjects = new RecentProjects();
            RecentProjects.LoadRecentProjects();
            
            CleanThemeApply();
            //Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();  // Having problems with this line, maybe just me tho
        }

        public static void CleanThemeApply()
        {
            var installation = new TextMate.Installation(new TextEditor(), new RegistryOptions(ThemeName.DarkPlus));
            ApplyTheme(installation, Settings.SelectedTheme);
            Instance.ApplyThemeColorsToResources(installation);
        }
        
        public async Task ApplyThemeColorsToResources(TextMate.Installation? e)
        {
            if (e == null) return;
            
            Resources["OverlayCornerRadius"] =  new CornerRadius(0,8,8,8);
            Resources["MenuFlyoutPresenterThemePadding"] = new Thickness(4,0,4,0);
            Resources["TabItemRightSeparatorHeight"] = (double)20;
            Resources["TabItemRightSeparatorWidth"] = (double)2;
            Resources["ControlCornerRadius"] = new CornerRadius(8);
            Resources["TabItemRightSeparatorCornerRadius"] = new CornerRadius(2);
            Resources["TextControlBorderThemeThickness"] = new Thickness(0);


            ApplyBrushAction(e, "tab.bg.inactive", brush =>
            {
                Resources["TabItemBackgroundBrush"] = brush;
            });
            // Classic press
            ApplyBrushAction(e, "tab.bg.unfocused", brush =>
            {
                Resources["MenuFlyoutItemKeyboardAcceleratorTextForegroundDisabled"] = brush;
                Resources["TabItemBackgroundBrushWindowInactive"] = brush;
                Resources["RepeatButtonBackgroundPressed"] = brush;
                Resources["ComboBoxBackgroundPressed"] = brush;
                Resources["ComboBoxBorderBrushPressed"] = brush;
            });
            ApplyBrushAction(e, "tab.close.unfocused", brush =>
            {
                Resources["CloseItemButtonInactiveWindowBrush"] = brush;
            });
            ApplyBrushAction(e, "tab.separator", brush =>
            {
                Resources["TabItemRightSeparatorBackgroundBrush"] = brush;
                Resources["CloseItemButtonInactiveWindowPointerOverBrush"] = brush;
            });
            ApplyBrushAction(e, "tab.close.hover", brush =>
            {
                Resources["CloseItemButtonPointerOverBrush"] = brush;
            });
            // Classic Hover
            ApplyBrushAction(e, "tab.bg.inactive.unfocused.hover", brush =>
            {
                Resources["TabItemHeaderBackgroundUnselectedPointerOverWindowInactive"] = brush;
                Resources["ComboBoxBackgroundPointerOver"] = brush;
                Resources["ComboBoxBorderBrushPointerOver"] = brush;
                Resources["TextControlBorderBrushPointerOver"] = brush;
                Resources["TextControlBackgroundPointerOver"] = brush;
                Resources["RepeatButtonBackgroundPointerOver"] = brush;
            });
            // Classic BG
            ApplyBrushAction(e, "tab.bg.inactive.hover", brush =>
            {
                Resources["RepeatButtonBackground"] = brush;
                Resources["TextControlBackground"] = brush;
                Resources["MenuFlyoutPresenterBackground"] = brush;
                Resources["MenuFlyoutPresenterBorderBrush"] = brush;
                Resources["ComboBoxDropDownBorderBrush"] = brush;
                Resources["ComboBoxDropDownBackground"] = brush;
                Resources["TabItemHeaderBackgroundUnselectedPointerOver"] = brush;
            });
            ApplyBrushAction(e, "env.foreground", brush =>
            {
                Resources["EditorForegroundBrush"] = brush;
                Resources["TextControlForeground"] = brush;
                Resources["TextControlForegroundFocused"] = brush;
                Resources["TextControlForegroundPointerOver"] = brush;
                Resources["MenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver"] = brush;
                Resources["MenuFlyoutItemKeyboardAcceleratorTextForeground"] = brush;
                Resources["MenuFlyoutSubItemChevronPointerOver"] = brush;
                Resources["MenuFlyoutSubItemChevron"] = brush;
                Resources["MenuFlyoutSubItemChevronSubMenuOpened"] = brush;
                Resources["MenuFlyoutItemKeyboardAcceleratorTextForegroundPressed"] = brush;
                Resources["MenuFlyoutSubItemChevronPressed"] = brush;
                Resources["CaptionButtonForeground"] = brush;
                Resources["CheckBoxCheckGlyphForegroundChecked"] = brush;
                Resources["CheckBoxCheckGlyphForegroundCheckedPointerOver"] = brush;
                Resources["CheckBoxCheckBackgroundFillChecked"] = brush;
                Resources["CheckBoxCheckBackgroundStrokeUncheckedPressed"] = brush;
                Resources["CheckBoxCheckBackgroundStrokeCheckedPointerOver"] = brush;
                Resources["CheckBoxCheckBackgroundStrokeCheckedPressed"] = brush;
            });

            ApplyBrushAction(e, "editor.foreground", brush =>
            {
                Resources["TreeViewItemForeground"] = brush;
                Resources["WindowAccent"] = brush;
                Resources["CloseItemButtonForeground"] = brush;
            });

            ApplyColorAction(e, "editor.background", color =>
            {
                Resources["WindowColor"] = color;
                var brush = new SolidColorBrush(color);
                Resources["SystemControlTransparentBrush"] = brush;
                Resources["EditorBackgroundBrush"] = brush;
                Resources["SelectedTabItemBackgroundBrush"] = brush;
                Resources["TextControlBackgroundFocused"] = brush;
                Resources["TextControlBorderBrushFocused"] = brush;
            });
            
            ApplyBrushAction(e, "link", brush =>
            {
                Resources["LinkBrush"] = brush;
            });
            ApplyBrushAction(e, "link.hover", brush =>
            {
                Resources["LinkHoverBrush"] = brush;
            });
            ApplyBrushAction(e, "decoration.bg.hover", brush =>
            {
                Resources["DecorationBackgroundHover"] = brush;
                Resources["CheckBoxCheckBackgroundFillChecked"] = brush;
                Resources["CheckBoxCheckBackgroundFillCheckedPointerOver"] = brush;
            });
            ApplyBrushAction(e, "decoration.bg.pressed", brush =>
            {
                Resources["DecorationBackgroundBrushPressed"] = brush;
                Resources["CheckBoxCheckBackgroundFillCheckedPressed"] = brush;
                Resources["CheckBoxCheckBackgroundFillUncheckedPressed"] = brush;
            });
            ApplyBrushAction(e, "decoration.exit.bg.hover", brush =>
            {
                Resources["ExitDecorationBackgroundBrushHover"] = brush;
            });
            ApplyBrushAction(e, "decoration.exit.bg.pressed", brush =>
            {
                Resources["ExitDecorationBackgroundBrushPressed"] = brush;
            });
            ApplyBrushAction(e, "editor.selectionBackground", brush =>
            {
                Resources["TextBoxSelectionBrush"] = brush;
            });
            ApplyBrushAction(e, "editorLineNumber.foreground", brush =>
            {
                Resources["LineNumberForegroundBrush"] = brush;
            });
        }
        public static bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson, Action<IBrush> applyColorAction)
        {
            if (!e.TryGetThemeColor(colorKeyNameFromJson, out var colorString))
            {
                Console.WriteLine($"Unable to find {colorKeyNameFromJson}");
                return false;
            }

            if (!Color.TryParse(colorString, out Color color))
                return false;

            var colorBrush = new SolidColorBrush(color);
            applyColorAction(colorBrush);
            return true;
        }
        public static bool ApplyColorAction(TextMate.Installation e, string colorKeyNameFromJson, Action<Color> applyColorAction)
        {
            if (!e.TryGetThemeColor(colorKeyNameFromJson, out var colorString))
                return false;
    
            if (!Color.TryParse(colorString, out Color color))
                return false;
    
            applyColorAction(color);
            return true;
        }
        public static void ApplyTheme(TextMate.Installation installation, string themeName)
        {
            using var stream = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Themes", themeName + ".json"));
            using var reader = new StreamReader(stream);
            var theme = ThemeReader.ReadThemeSync(reader);
            
            installation.SetTheme(theme);
        }

        public void SetResource(string resource, object? value)
        {
            Resources[resource] = value;
        }
        
        
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // DisableAvaloniaDataAnnotationValidation();

                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        // private void DisableAvaloniaDataAnnotationValidation()
        // {
        //     // Get an array of plugins to remove
        //     var dataValidationPluginsToRemove =
        //         BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
        //
        //     // remove each entry found
        //     foreach (var plugin in dataValidationPluginsToRemove)
        //     {
        //         BindingPlugins.DataValidators.Remove(plugin);
        //     }
        // }
    }
}