using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Reflection;
using Avalonia.Markup.Xaml;
using GMMLauncher.ViewModels;
using GMMLauncher.Views;

namespace GMMLauncher
{
    public partial class App : Application
    {
        public static Settings Settings { get; set; }
        public static string appVersion { get; private set; }
        public override void Initialize()
        {
            appVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion;
            AvaloniaXamlLoader.Load(this);
            Settings = new Settings();
            Settings.LoadSettings();
        }
        

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                DisableAvaloniaDataAnnotationValidation();

                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}