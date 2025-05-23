﻿using Avalonia;
using System;
using System.Reflection;
using GMMBackend;

namespace GMMLauncher
{
    sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Logger.LogInfo("Starting GMMLauncher");
            Logger.LogInfo($"GMMLauncher v{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}");
            // Logger.LogWarning("Using Development Build");
            // Logger.LogTrace("TraceTest - should be Program.cs line 15");
            Console.Write(GitManagement.GenGitKey("johnpork@timcheese.com"));

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
