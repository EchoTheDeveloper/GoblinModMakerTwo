using System;
using System.IO;

namespace GMMBackend
{

    /*
    Simple logger class that writes log messages to a file.
    The log is found in appdata/Goblin Mod Maker/LOG.txt
    I made it to make debugging easier than using Console.WriteLine
    It also Console.WriteLine the message so you can see it in the console
    to use it you do not need to make an instance all you need to do is include using GMMbackend; which will most likely be included in the files you need to use it in
    than just do Logger.Log or all the others.
    NOTE: Logger.LogTrace will log the line number of the caller
    NOTE: Logger.LogFatal will log the message and exit the program with exit code 1 (unless we give a seperate exit code)
     */

    public class Logger
    {
        private static readonly string logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Goblin Mod Maker", "LOG.txt");

        static Logger()
        {
            string logDirectory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }

        public static void Log(string message)
        {
            WriteLog("LOG:", message);
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
            WriteLog("TRACE:", $"{message} \n From: [File: {callerFile} | Line: {callerLine}]");

        }

        public static void LogWarning(string message)
        {
            WriteLog("WARNING:", message);
        }

        public static async void LogFatal(string message, int exitCode = 1)
        {
            WriteLog("FATAL", message);
            Environment.Exit(exitCode);
        }

        private static void WriteLog(string logType, string message)
        {
            try
            {
                string logMessage = $"{DateTime.Now:dd-MM-yyyy ss:mm:HH} [{logType}] {message}";
                Console.Write(logMessage);
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to log file: " + ex.Message);
            }
        }
    }
}
