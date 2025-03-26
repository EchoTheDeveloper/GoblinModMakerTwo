using System;
using System.IO;

namespace GMMBackend
{
    public static class Utils
    {
        public static string GetAppDataPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GoblinModMaker");
        }
        public static string GetAppDataPath(string fileName)
        {
            return Path.Combine(GetAppDataPath(), fileName);
        }
    }
    public static class Logger
    {
        private static readonly string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Goblin Mod Maker");

        private static readonly string logFilePath = Path.Combine(logDirectory, "LOG.txt");

        static Logger()
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public static void Log<T>(T message)
        {
            WriteLog("LOG:", message as string);
        }

        public static void LogError(string message)
        {
            WriteLog("ERROR:", message);
        }

        public static void LogInfo(string message)
        {
            WriteLog("INFO:", message);
        }

        public static void LogTrace(string message)
        {
            var stackTrace = new System.Diagnostics.StackTrace(true);
            var callerLine = stackTrace.GetFrame(1).GetFileLineNumber();
            var callerFile = stackTrace.GetFrame(1).GetFileName();
            WriteLog("TRACE:", $"{message} \nFrom: [File: {callerFile} | Line: {callerLine}]");
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARNING:", message);
        }

        public static void LogFatal(string message, int exitCode = 1)
        {
            WriteLog("FATAL:", $"{message} SHUTDOWN CAUSED BY A FATAL ERROR. Exit Code: {exitCode}");
            Environment.Exit(exitCode);
        }

        private static void WriteLog(string logType, string message)
        {
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fff} [{logType}] {message}";
                Console.WriteLine(logMessage);
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to log file: " + ex.Message);
            }
        }
    }
}