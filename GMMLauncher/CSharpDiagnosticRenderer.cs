using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using Microsoft.CodeAnalysis;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System.Text.RegularExpressions;
namespace GMMLauncher;

public class CSharpDiagnosticRenderer : IBackgroundRenderer
{
    private readonly TextEditor _editor;

    private List<DiagnosticMarker> _markers = new();

    public CSharpDiagnosticRenderer(TextEditor editor)
    {
        _editor = editor;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void SetDiagnostics(IEnumerable<DiagnosticMarker> markers)
    {
        _markers = markers.ToList();

        _editor.TextArea.TextView.InvalidateLayer(Layer);
    }

    public List<DiagnosticMarker>? CheckForDiagnosticAtOffset(int offset)
    {
        List<DiagnosticMarker> markers = new();
        foreach (var marker in _markers)
        {
            if (offset >= marker.StartOffset && offset <= marker.EndOffset)
            {
                markers.Add(marker);
            }
        }
        return markers;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        foreach (var marker in _markers)
        {
            DrawMarker(
                textView,
                drawingContext,
                marker);
        }
    }

    private void DrawMarker(TextView textView, DrawingContext drawingContext, DiagnosticMarker marker)
    {
        var start = marker.StartOffset;
        var end = marker.EndOffset;

        if (start < 0 ||
            end <= start ||
            start >= _editor.Document.TextLength)
            return;

        end = System.Math.Min(
            end,
            _editor.Document.TextLength);

        var segment = new SimpleSegment(
            start,
            end - start);
        try
        {
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                var pen = new Pen(
                    marker.Brush,
                    1.5);
    
                DrawSquiggle(
                    drawingContext,
                    rect,
                    pen);
            }
        }
        catch (VisualLinesInvalidException)
        {
            return;
        }
    }

    private void DrawSquiggle(DrawingContext drawingContext, Rect rect, Pen pen)
    {
        const double waveHeight = 3;
        const double waveWidth = 3;

        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            context.BeginFigure(
                new Point(
                    rect.Left,
                    rect.Bottom - 1),
                false);

            var x = rect.Left;

            bool up = true;

            while (x < rect.Right)
            {
                var nextX =
                    System.Math.Min(
                        x + waveWidth,
                        rect.Right);

                var y =
                    rect.Bottom -
                    (up ? waveHeight : 1);

                context.LineTo(
                    new Point(nextX, y));

                up = !up;
                x = nextX;
            }
        }

        drawingContext.DrawGeometry(
            null,
            pen,
            geometry);
    }
}

public class DiagnosticMarker
{
    public int StartOffset { get; }

    public int EndOffset { get; }

    public string Message { get; }

    public DiagnosticSeverity Severity { get; }

    public IBrush Brush { get; }

    public DiagnosticMarker(int startOffset, int endOffset, string message, DiagnosticSeverity severity, IBrush brush)
    {
        StartOffset = startOffset;
        EndOffset = endOffset;
        Message = message;
        Severity = severity;
        Brush = brush;
    }

    #region Simplify Helper
    public string GetSimplifiedMessage()
    {
        var message = Message.Trim();
    
        message = Regex.Replace(
            message,
            @"[A-Za-z]:\\[^:]*(?=[:])",
            "");
    
        message = Regex.Replace(
            message,
            @"'([^']+)'",
            "'$1'");
    
        return message switch
        {
            var m when m.Contains(
                    "conflicts with the imported type")
                => SimplifyTypeConflict(m),
            
            var m when m.Contains(
                "does not exist in the current context")
                => SimplifyUnknownIdentifier(m),
    
            var m when m.Contains(
                "could not be found")
                => SimplifyNotFound(m),
    
            var m when m.Contains(
                "Cannot implicitly convert type")
                => SimplifyConversion(m),
    
            var m when m.Contains(
                "Argument")
                && m.Contains("cannot convert from")
                => SimplifyArgument(m),
    
            var m when m.Contains(
                "No overload for method")
                && m.Contains("takes")
                => SimplifyOverload(m),
    
            var m when m.Contains(
                "The type or namespace name")
                && m.Contains("could not be found")
                => SimplifyTypeNotFound(m),
    
            var m when m.Contains(
                "is inaccessible due to its protection level")
                => SimplifyInaccessible(m),
    
            var m when m.Contains(
                "already contains a definition for")
                => SimplifyDuplicate(m),
            
    
            _ => message
        };
    }
    
    private static string SimplifyUnknownIdentifier(string message)
    {
        var match = Regex.Match(
            message,
            @"The name '([^']+)' does not exist");
    
        return match.Success
            ? $"Unknown identifier '{match.Groups[1].Value}'"
            : message;
    }
    
    private static string SimplifyNotFound(string message)
    {
        var match = Regex.Match(
            message,
            @"'([^']+)'");
    
        return match.Success
            ? $"'{match.Groups[1].Value}' could not be found"
            : message;
    }
    
    private static string SimplifyConversion(string message)
    {
        var match = Regex.Match(
            message,
            @"Cannot implicitly convert type '([^']+)' to '([^']+)'");
    
        return match.Success
            ? $"Cannot convert '{match.Groups[1].Value}' to '{match.Groups[2].Value}'"
            : message;
    }
    
    private static string SimplifyArgument(string message)
    {
        var match = Regex.Match(
            message,
            @"Argument (\d+): cannot convert from '([^']+)' to '([^']+)'");
    
        return match.Success
            ? $"Argument {match.Groups[1].Value}: cannot convert '{match.Groups[2].Value}' to '{match.Groups[3].Value}'"
            : message;
    }
    
    private static string SimplifyOverload(string message)
    {
        var match = Regex.Match(
            message,
            @"No overload for method '([^']+)' takes (\d+) arguments");
    
        return match.Success
            ? $"'{match.Groups[1].Value}' doesn't accept {match.Groups[2].Value} arguments"
            : message;
    }
    
    private static string SimplifyTypeNotFound(string message)
    {
        var match = Regex.Match(
            message,
            @"The type or namespace name '([^']+)' could not be found");
    
        return match.Success
            ? $"Type or namespace '{match.Groups[1].Value}' not found"
            : message;
    }
    
    private static string SimplifyInaccessible(string message)
    {
        var match = Regex.Match(
            message,
            @"'([^']+)' is inaccessible");
    
        return match.Success
            ? $"'{match.Groups[1].Value}' is inaccessible"
            : message;
    }
    
    private static string SimplifyDuplicate(string message)
    {
        var match = Regex.Match(
            message,
            @"already contains a definition for '([^']+)'");
    
        return match.Success
            ? $"Duplicate definition '{match.Groups[1].Value}'"
            : message;
    }
    private static string SimplifyTypeConflict(string message)
    {
        var match = Regex.Match(
            message,
            @"The type '([^']+)' in '([^']+)' conflicts with the imported type '([^']+)' in '([^']+)'");

        if (!match.Success)
            return message;

        var definedType = match.Groups[1].Value;
        var importedType = match.Groups[3].Value;
        var importedAssembly = Path.GetFileName(
            match.Groups[4].Value);

        if (definedType == importedType)
            return $"Type '{definedType}' conflicts with imported type from '{importedAssembly}'";

        return $"Type '{definedType}' conflicts with imported type '{importedType}' from '{importedAssembly}'";
    }
    #endregion
}

