using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using GMMLauncher.Views;

namespace GMMLauncher;

public static class WindowManager
{
    public static List<Window> Windows = new();

    public static void Add(Window window)
    {
        Windows.Add(window);
        window.Closed += (_, _) => Windows.Remove(window);
    }

    public static CodeEditor SearchForModsInCodeEditor(string filePath)
    {
        return Windows.OfType<CodeEditor>().FirstOrDefault(w => w.Mod.GetModFilePath().Equals(filePath));
    }
}