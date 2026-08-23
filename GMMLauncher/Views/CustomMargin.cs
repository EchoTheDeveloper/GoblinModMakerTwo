using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;

#nullable enable

namespace GMMLauncher.Views
{
    /// <summary>
    /// A custom margin that draws a red dot when clicked. (Similar to the breakpoint margin the
    /// Visual Studio editor.)
    /// </summary>
    public sealed class CustomMargin : AbstractMargin
    {
        private readonly IBrush _defaultbackgroundBrush = Brushes.Transparent;
        private IBrush _backgroundBrush = new ImmutableSolidColorBrush(new Color(255, 51, 51, 51));
        private readonly IBrush _pointerOverBrush = new ImmutableSolidColorBrush(new Color(192, 80, 80, 80));
        private readonly IPen _pointerOverPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(192, 37, 37, 37)), 1);
        private readonly IBrush _markerBrush = new ImmutableSolidColorBrush(new Color(255, 195, 81, 92));
        private readonly IPen _markerPen = new ImmutablePen(new ImmutableSolidColorBrush(new Color(255, 240, 92, 104)), 1);

        private readonly List<int> _markedDocumentLines = [];
        private int _pointerOverLine = -1;

        public IBrush BackGroundBrush
        {
            get => _backgroundBrush;
            set
            {
                _backgroundBrush = value;
                InvalidateVisual();
            }
        }

        public void SetDefaultBackgroundBrush()
        {
            _backgroundBrush = _defaultbackgroundBrush;
            InvalidateVisual();
        }

        public CustomMargin()
        {
            Cursor = new Cursor(StandardCursorType.Arrow);
        }

        protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
        {
            if (oldTextView != null)
            {
                oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
                oldTextView.DocumentChanged -= OnDocumentChanged;
            }

            if (newTextView != null)
            {
                newTextView.VisualLinesChanged += OnVisualLinesChanged;
                newTextView.DocumentChanged += OnDocumentChanged;
            }

            base.OnTextViewChanged(oldTextView, newTextView);
        }

        private void OnVisualLinesChanged(object? sender, EventArgs eventArgs)
        {
            InvalidateVisual();
        }

        private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
            _markedDocumentLines.Clear();
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(20, 0);
        }

        private int GetLineNumber(PointerEventArgs e)
        {
            double visualY = e.GetPosition(TextView).Y + TextView.VerticalOffset;
            VisualLine visualLine = TextView.GetVisualLineFromVisualTop(visualY);
            return (visualLine == null) ? -1 : visualLine.FirstDocumentLine.LineNumber;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            _pointerOverLine = GetLineNumber(e);
            InvalidateVisual();

            base.OnPointerMoved(e);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            _pointerOverLine = -1;
            InvalidateVisual();

            base.OnPointerExited(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            int line = _pointerOverLine = GetLineNumber(e);

            if (!_markedDocumentLines.Remove(line))
                _markedDocumentLines.Add(line);

            _markedDocumentLines.Sort();
            InvalidateVisual();
            e.Handled = true;

            base.OnPointerPressed(e);
        }

        public override void Render(DrawingContext context)
        {
            context.DrawRectangle(_backgroundBrush, null, Bounds);

            if (TextView?.VisualLinesValid == true)
            {
                foreach (var visualLine in TextView.VisualLines)
                {
                    double y = visualLine.VisualTop - TextView.VerticalOffset + visualLine.Height / 2;

                    if (_markedDocumentLines.Contains(visualLine.FirstDocumentLine.LineNumber))
                        context.DrawEllipse(_markerBrush, _markerPen, new Point(10, y), 8, 8);
                    else if (_pointerOverLine == visualLine.FirstDocumentLine.LineNumber)
                        context.DrawEllipse(_pointerOverBrush, _pointerOverPen, new Point(10, y), 8, 8);
                }
            }

            base.Render(context);
        }
    }
    public class CustomFoldingMargin : AbstractMargin
    {
        private readonly FoldingManager _foldingManager;

        private int _hoveredLine = -1;

        public CustomFoldingMargin(FoldingManager foldingManager)
        {
            _foldingManager = foldingManager;

            Width = 20;

            PointerMoved += OnPointerMoved;
            PointerExited += OnPointerExited;
            PointerPressed += OnPointerPressed;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var textView = TextView;

            if (textView == null)
                return;

            foreach (var folding in _foldingManager.AllFoldings)
            {
                if (folding.StartOffset < 0 ||
                    folding.StartOffset > textView.Document.TextLength)
                    continue;

                var line = textView.Document.GetLineByOffset(
                    folding.StartOffset);

                var visualLine = textView.GetVisualLine(
                    line.LineNumber);

                if (visualLine == null)
                    continue;

                var y = visualLine.VisualTop - textView.VerticalOffset;
                var center = y + visualLine.Height / 2;

                DrawChevron(
                    context,
                    new Point(Width / 2, center),
                    folding.IsFolded,
                    line.LineNumber == _hoveredLine);
            }
        }

        private void DrawChevron(
            DrawingContext context,
            Point center,
            bool collapsed,
            bool hovered)
        {
            var geometry = new StreamGeometry();

            using (var builder = geometry.Open())
            {
                if (collapsed)
                {
                    builder.BeginFigure(
                        new Point(center.X - 2, center.Y - 4),
                        false);

                    builder.LineTo(
                        new Point(center.X + 2, center.Y));

                    builder.LineTo(
                        new Point(center.X - 2, center.Y + 4));
                }
                else
                {
                    builder.BeginFigure(
                        new Point(center.X - 3, center.Y - 2),
                        false);

                    builder.LineTo(
                        new Point(center.X, center.Y + 2));

                    builder.LineTo(
                        new Point(center.X + 3, center.Y - 2));
                }
            }

            var brush = hovered
                ? Brushes.White
                : Brushes.Gray;

            context.DrawGeometry(
                null,
                new Pen(brush, 1.5),
                geometry);
        }

        private void OnPointerMoved(
            object? sender,
            PointerEventArgs e)
        {
            var point = e.GetPosition(this);

            var line = TextView?.GetDocumentLineByVisualTop(
                point.Y + TextView.VerticalOffset);

            var newLine = line?.LineNumber ?? -1;

            if (newLine == _hoveredLine)
                return;

            _hoveredLine = newLine;
            InvalidateVisual();
        }

        private void OnPointerExited(
            object? sender,
            PointerEventArgs e)
        {
            _hoveredLine = -1;
            InvalidateVisual();
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (TextView == null)
                return;

            var point = e.GetPosition(this);

            var line = TextView.GetDocumentLineByVisualTop(
                point.Y + TextView.VerticalOffset);

            if (line == null)
                return;

            FoldingSection? target = null;

            foreach (var folding in _foldingManager.AllFoldings)
            {
                var foldingLine =
                    TextView.Document.GetLineByOffset(
                        folding.StartOffset);

                if (foldingLine.LineNumber == line.LineNumber)
                {
                    target = folding;
                    break;
                }
            }

            if (target == null)
                return;

            target.IsFolded = !target.IsFolded;

            e.Handled = true;

            InvalidateVisual();
            TextView.InvalidateVisual();
        }
    }
}
