using System.Collections.Generic;
using Avalonia.Controls;

namespace GMMBackend;

public static class WindowManager
{
    public static List<Window> Windows = new();

    public static void Add(Window window)
    {
        Windows.Add(window);
        window.Closed += (_, _) => Windows.Remove(window);
    }
}