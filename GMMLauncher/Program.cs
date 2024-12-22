using Avalonia;
using System;
using GMMBackend;

namespace GMMLauncher
{
    sealed class Program
    {
        // Main method
        [STAThread]
        public static void Main(string[] args)
        {
            Logger.LogInfo("Starting GMMLauncher");
            Logger.LogWarning("Using InfDev");

            // Build and start the Avalonia application
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
