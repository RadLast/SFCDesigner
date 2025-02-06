using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Windows.Controls;

namespace LabelDesigner.Services
{
    /// <summary>
    /// Adorner, který vykreslí modrý rámeček kolem prvku 
    /// + 8 úchopových bodů pro změnu velikosti.
    /// </summary>
    public class SelectionResizeAdorner : Adorner
    {
        private const double HandleSize = 10;

        // Seznam "úchopů" = malé Rectangle + jejich pozice (budeme kreslit ručně)
        private readonly List<Rect> _handles = new List<Rect>(8);

        // Uchovává info, který handle uživatel táhne.
        private int? _activeHandleIndex = null;

        private Point _startDragPoint;
        private Rect _startElementRect; // levý/horní + šířka/výška

        // Konstruktor
        public SelectionResizeAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            // Chceme přijímat myší události (pro resize)
            this.IsHitTestVisible = true;

            // Registrujeme "Preview" události, abychom měli šanci
            // odchytit myš i když by se jinde nastavovalo e.Handled.
            this.AddHandler(MouseLeftButtonDownEvent,
                            new MouseButtonEventHandler(OnMouseLeftButtonDown),
                            true);
            this.AddHandler(MouseMoveEvent,
                            new MouseEventHandler(OnMouseMove),
                            true);
            this.AddHandler(MouseLeftButtonUpEvent,
                            new MouseButtonEventHandler(OnMouseLeftButtonUp),
                            true);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var element = this.AdornedElement as FrameworkElement;
            if (element == null) return;

            // Zjistíme velikost reálného prvku
            double width = element.ActualWidth;
            double height = element.ActualHeight;

            // Nakreslíme modrý obdélník kolem
            var pen = new Pen(Brushes.Blue, 2);
            Rect adornedRect = new Rect(0, 0, width, height);
            drawingContext.DrawRectangle(null, pen, adornedRect);

            // Určíme polohy 8 úchopů
            _handles.Clear();
            // Každý úchop definujeme jako obdélník (Rect) v souřadnicích Adorneru
            // Např. TopLeft:
            _handles.Add(new Rect(-HandleSize / 2, -HandleSize / 2, HandleSize, HandleSize));               // 0: TopLeft
            _handles.Add(new Rect(width / 2 - HandleSize / 2, -HandleSize / 2, HandleSize, HandleSize));      // 1: TopCenter
            _handles.Add(new Rect(width - HandleSize / 2, -HandleSize / 2, HandleSize, HandleSize));        // 2: TopRight
            _handles.Add(new Rect(-HandleSize / 2, height / 2 - HandleSize / 2, HandleSize, HandleSize));     // 3: MiddleLeft
            _handles.Add(new Rect(width - HandleSize / 2, height / 2 - HandleSize / 2, HandleSize, HandleSize)); // 4: MiddleRight
            _handles.Add(new Rect(-HandleSize / 2, height - HandleSize / 2, HandleSize, HandleSize));       // 5: BottomLeft
            _handles.Add(new Rect(width / 2 - HandleSize / 2, height - HandleSize / 2, HandleSize, HandleSize));  // 6: BottomCenter
            _handles.Add(new Rect(width - HandleSize / 2, height - HandleSize / 2, HandleSize, HandleSize));    // 7: BottomRight

            // Vykreslíme úchopové body (šedé čtverečky)
            foreach (var handleRect in _handles)
            {
                drawingContext.DrawRectangle(Brushes.Gray,
                                              new Pen(Brushes.Black, 1),
                                              handleRect);
            }
        }

        // Zjistíme, zda uživatel klikl do některého z úchopů
        private int? GetHandleIndexUnderPoint(Point p)
        {
            for (int i = 0; i < _handles.Count; i++)
            {
                if (_handles[i].Contains(p)) return i;
            }
            return null;
        }

        // Myší události:
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = AdornedElement as FrameworkElement;
            if (element == null) return;

            var pos = e.GetPosition(this); // Lokální souřadnice v adorneru
            var handleIndex = GetHandleIndexUnderPoint(pos);
            if (handleIndex.HasValue)
            {
                _activeHandleIndex = handleIndex.Value;

                // Souřadnice v Canvasu
                var canvas = VisualTreeHelper.GetParent(element) as Canvas;
                if (canvas != null)
                {
                    _startDragPoint = e.GetPosition(canvas);
                    _startElementRect = new Rect(
                        Canvas.GetLeft(element),
                        Canvas.GetTop(element),
                        element.Width,
                        element.Height
                    );
                }

                this.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_activeHandleIndex.HasValue) return;

            var element = AdornedElement as FrameworkElement;
            if (element == null) return;

            // Souřadnice v Canvasu
            var canvas = VisualTreeHelper.GetParent(element) as Canvas;
            if (canvas == null) return;

            var currentPos = e.GetPosition(canvas);

            double deltaX = currentPos.X - _startDragPoint.X;
            double deltaY = currentPos.Y - _startDragPoint.Y;

            double newLeft = _startElementRect.Left;
            double newTop = _startElementRect.Top;
            double newWidth = _startElementRect.Width;
            double newHeight = _startElementRect.Height;

            switch (_activeHandleIndex.Value)
            {
                case 0: // TopLeft
                    newLeft += deltaX;
                    newTop += deltaY;
                    newWidth -= deltaX;
                    newHeight -= deltaY;
                    break;
                case 1: // TopCenter
                    newTop += deltaY;
                    newHeight -= deltaY;
                    break;
                case 2: // TopRight
                    newTop += deltaY;
                    newWidth += deltaX;
                    newHeight -= deltaY;
                    break;
                case 3: // MiddleLeft
                    newLeft += deltaX;
                    newWidth -= deltaX;
                    break;
                case 4: // MiddleRight
                    newWidth += deltaX;
                    break;
                case 5: // BottomLeft
                    newLeft += deltaX;
                    newWidth -= deltaX;
                    newHeight += deltaY;
                    break;
                case 6: // BottomCenter
                    newHeight += deltaY;
                    break;
                case 7: // BottomRight
                    newWidth += deltaX;
                    newHeight += deltaY;
                    break;
            }

            // Zabránit záporné velikosti
            if (newWidth < 10) newWidth = 10;
            if (newHeight < 10) newHeight = 10;

            // Nastavit Canvas
            Canvas.SetLeft(element, newLeft);
            Canvas.SetTop(element, newTop);

            // Nastavit rozměry elementu (využíváme to, že W=H=0 => text by se 
            // mohl roztahovat podle obsahu, tak to radši držíme takto).
            element.Width = newWidth;
            element.Height = newHeight;

            e.Handled = true;

            // Po změně rozměru si vyžádáme překreslení Adorneru
            this.InvalidateVisual();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_activeHandleIndex.HasValue)
            {
                _activeHandleIndex = null;
                this.ReleaseMouseCapture();
                e.Handled = true;
            }
        }
    }
}
