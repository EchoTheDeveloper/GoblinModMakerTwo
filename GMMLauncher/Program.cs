using Avalonia;
using System;
using GMMBackend;

namespace GMMLauncher
{
    sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Logger.LogInfo("Starting GMMLauncher");
            Logger.LogInfo("GMMLauncher v1.0.0-alpha1");
            Logger.LogWarning("Using Development Build");
            Logger.LogTrace("TraceTest - should be Program.cs line 15");

            // Build and start the Avalonia application
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
