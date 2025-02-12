using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SFCDesigner.Services
{
    /// <summary>
    /// Adorner, který kolem prvku vykresluje modrý rámeček 
    /// a 8 úchopových bodů pro měnění velikosti.
    /// </summary>
    public class SelectionResizeAdorner : Adorner
    {
        #region Constants

        private const double HandleSize = 10;

        #endregion

        #region Fields

        // Rectangle "úchopy" pro resize
        private readonly List<Rect> _handles = new List<Rect>(8);

        // Index úchopu, který je právě táhnut myší (null, pokud žádný)
        private int? _activeHandleIndex;

        // Počáteční bod myši při stisku
        private Point _startDragPoint;

        // Původní polohy prvku (levý, horní okraj, šířka, výška)
        private Rect _startElementRect;

        #endregion

        #region Constructor

        /// <summary>
        /// Vytvoří nový adorner pro daný prvek (adornedElement).
        /// Umožňuje změnu velikosti tažením úchopů.
        /// </summary>
        public SelectionResizeAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            IsHitTestVisible = true;

            // Registrujeme "Preview" události kvůli jistotě,
            // že je zachytíme dřív, než se označí e.Handled.
            AddHandler(MouseLeftButtonDownEvent,
                       new MouseButtonEventHandler(OnMouseLeftButtonDown),
                       true);
            AddHandler(MouseMoveEvent,
                       new MouseEventHandler(OnMouseMove),
                       true);
            AddHandler(MouseLeftButtonUpEvent,
                       new MouseButtonEventHandler(OnMouseLeftButtonUp),
                       true);
        }

        #endregion

        #region Overridden Methods

        /// <summary>
        /// Vykreslí se modrý rámeček a úchopové body.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement is not FrameworkElement element)
                return;

            double width = element.ActualWidth;
            double height = element.ActualHeight;

            // Modrý rámeček
            var pen = new Pen(Brushes.Blue, 2);
            var adornedRect = new Rect(0, 0, width, height);
            drawingContext.DrawRectangle(null, pen, adornedRect);

            // Pro jistotu vyčistíme seznam a znovu naplníme
            _handles.Clear();

            // Definice 8 úchopů po obvodu
            // 0: TopLeft
            _handles.Add(new Rect(-HandleSize / 2, -HandleSize / 2, HandleSize, HandleSize));
            // 1: TopCenter
            _handles.Add(new Rect(width / 2 - HandleSize / 2, -HandleSize / 2, HandleSize, HandleSize));
            // 2: TopRight
            _handles.Add(new Rect(width - HandleSize / 2, -HandleSize / 2, HandleSize, HandleSize));
            // 3: MiddleLeft
            _handles.Add(new Rect(-HandleSize / 2, height / 2 - HandleSize / 2, HandleSize, HandleSize));
            // 4: MiddleRight
            _handles.Add(new Rect(width - HandleSize / 2, height / 2 - HandleSize / 2, HandleSize, HandleSize));
            // 5: BottomLeft
            _handles.Add(new Rect(-HandleSize / 2, height - HandleSize / 2, HandleSize, HandleSize));
            // 6: BottomCenter
            _handles.Add(new Rect(width / 2 - HandleSize / 2, height - HandleSize / 2, HandleSize, HandleSize));
            // 7: BottomRight
            _handles.Add(new Rect(width - HandleSize / 2, height - HandleSize / 2, HandleSize, HandleSize));

            // Vykreslení šedých čtverečků
            foreach (var handleRect in _handles)
            {
                drawingContext.DrawRectangle(
                    Brushes.Gray,
                    new Pen(Brushes.Black, 1),
                    handleRect);
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Zjistí, zda bod myši spadá do některého z _handles.
        /// </summary>
        private int? GetHandleIndexUnderPoint(Point p)
        {
            for (int i = 0; i < _handles.Count; i++)
            {
                if (_handles[i].Contains(p))
                    return i;
            }
            return null;
        }

        #endregion

        #region Mouse Event Handlers

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (AdornedElement is not FrameworkElement element)
                return;

            // Pozice v souřadnicích adorneru
            var pos = e.GetPosition(this);
            _activeHandleIndex = GetHandleIndexUnderPoint(pos);

            if (_activeHandleIndex.HasValue)
            {
                // Zjistíme pozici v Canvasu, abychom měli reálné XY
                if (VisualTreeHelper.GetParent(element) is Canvas canvas)
                {
                    _startDragPoint = e.GetPosition(canvas);
                    _startElementRect = new Rect(
                        Canvas.GetLeft(element),
                        Canvas.GetTop(element),
                        element.Width,
                        element.Height);
                }

                CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_activeHandleIndex.HasValue)
                return;

            if (AdornedElement is not FrameworkElement element)
                return;

            if (VisualTreeHelper.GetParent(element) is not Canvas canvas)
                return;

            var currentPos = e.GetPosition(canvas);

            double deltaX = currentPos.X - _startDragPoint.X;
            double deltaY = currentPos.Y - _startDragPoint.Y;

            double newLeft = _startElementRect.Left;
            double newTop = _startElementRect.Top;
            double newWidth = _startElementRect.Width;
            double newHeight = _startElementRect.Height;

            // Podle indexu handle rozhodujeme, jak měníme polohu a velikost
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

            // Zabránit záporným rozměrům
            if (newWidth < 10) newWidth = 10;
            if (newHeight < 10) newHeight = 10;

            // Nastavíme Canvas
            Canvas.SetLeft(element, newLeft);
            Canvas.SetTop(element, newTop);

            // Nastavíme rozměry elementu
            element.Width = newWidth;
            element.Height = newHeight;

            e.Handled = true;

            // Překreslíme adorner (aby se úchopy správně zobrazily)
            InvalidateVisual();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_activeHandleIndex.HasValue)
            {
                _activeHandleIndex = null;
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        #endregion
    }
}