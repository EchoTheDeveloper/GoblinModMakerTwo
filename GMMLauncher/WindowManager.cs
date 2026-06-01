using System.Collections.Generic;
using Avalonia.Controls;

namespace GMMLauncher;

public static class WindowManager
{
    public static List<Window> Windows = new();

    public static void Add(Window window)
    {
        Windows.Add(window);
        window.Closed += (_, _) => Windows.Remove(window);
    }
}