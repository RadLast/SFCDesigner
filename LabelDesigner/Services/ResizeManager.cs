using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace LabelDesigner.Services
{
    public class ResizeManager
    {
        private const double HandleSize = 10;

        private readonly ObservableCollection<UIElement> _canvasElements;
        private readonly Rectangle[] _resizeHandles = new Rectangle[8];

        private FrameworkElement? _targetElement;

        private Point _initialMousePosition;
        private Point _initialElementPosition;
        private Size _initialElementSize;
        private ResizeHandle? _activeHandle;

        public ResizeManager(ObservableCollection<UIElement> canvasElements)
        {
            _canvasElements = canvasElements;

            // Vytvoříme 8 handle-obdélníků a přidáme je do CanvasElements (zatím schované)
            for (int i = 0; i < _resizeHandles.Length; i++)
            {
                var handle = new Rectangle
                {
                    Width = HandleSize,
                    Height = HandleSize,
                    Fill = Brushes.Gray,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Visibility = Visibility.Collapsed,
                    Tag = (ResizeHandle)i
                };

                // Události myši (resize)
                handle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
                handle.MouseMove += Handle_MouseMove;
                handle.MouseLeftButtonUp += Handle_MouseLeftButtonUp;

                _resizeHandles[i] = handle;
                _canvasElements.Add(handle);
            }
        }

        /// <summary>
        /// Zviditelní úchopy pro daný prvek (a přepočítá jejich pozice).
        /// </summary>
        public void AttachToElement(FrameworkElement element)
        {
            DetachFromElement();

            _targetElement = element;
            UpdateHandlesPosition();

            // Zviditelníme všech 8 obdélníků (úchopů)
            foreach (var handle in _resizeHandles)
            {
                handle.Visibility = Visibility.Visible;
                Panel.SetZIndex(handle, Int32.MaxValue);
            }
        }

        /// <summary>
        /// Schová úchopové body.
        /// </summary>
        public void DetachFromElement()
        {
            if (_targetElement != null)
            {
                foreach (var handle in _resizeHandles)
                {
                    handle.Visibility = Visibility.Collapsed;
                }
                _targetElement = null;
            }
        }

        /// <summary>
        /// Přepočítá pozici 8 obdélníků kolem aktuálního prvku (_targetElement).
        /// </summary>
        public void UpdateHandlesPosition()
        {
            if (_targetElement == null) return;

            double left = Canvas.GetLeft(_targetElement);
            double top = Canvas.GetTop(_targetElement);
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            double width = _targetElement.Width;
            double height = _targetElement.Height;
            if (double.IsNaN(width)) width = _targetElement.ActualWidth;
            if (double.IsNaN(height)) height = _targetElement.ActualHeight;

            // Rozmístíme úchopy do 8 pozic
            PositionHandle(_resizeHandles[0], left - HandleSize / 2, top - HandleSize / 2);                     // TopLeft
            PositionHandle(_resizeHandles[1], left + width / 2 - HandleSize / 2, top - HandleSize / 2);         // TopCenter
            PositionHandle(_resizeHandles[2], left + width - HandleSize / 2, top - HandleSize / 2);            // TopRight
            PositionHandle(_resizeHandles[3], left - HandleSize / 2, top + height / 2 - HandleSize / 2);        // MiddleLeft
            PositionHandle(_resizeHandles[4], left + width - HandleSize / 2, top + height / 2 - HandleSize / 2);// MiddleRight
            PositionHandle(_resizeHandles[5], left - HandleSize / 2, top + height - HandleSize / 2);           // BottomLeft
            PositionHandle(_resizeHandles[6], left + width / 2 - HandleSize / 2, top + height - HandleSize / 2);// BottomCenter
            PositionHandle(_resizeHandles[7], left + width - HandleSize / 2, top + height - HandleSize / 2);    // BottomRight
        }

        private void PositionHandle(Rectangle handle, double left, double top)
        {
            Canvas.SetLeft(handle, left);
            Canvas.SetTop(handle, top);
        }

        // --- Metody obsluhy myši při resize úchopech ---

        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_targetElement == null) return;

            // Zabránit současnému drag
            e.Handled = true;

            var rect = (Rectangle)sender;
            _activeHandle = (ResizeHandle)rect.Tag;

            var canvas = GetParentCanvas(rect);
            if (canvas == null) return;

            _initialMousePosition = e.GetPosition(canvas);

            // Uložit pozici a velikost prvku
            double left = Canvas.GetLeft(_targetElement);
            double top = Canvas.GetTop(_targetElement);
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            double width = _targetElement.Width;
            double height = _targetElement.Height;
            if (double.IsNaN(width)) width = _targetElement.ActualWidth;
            if (double.IsNaN(height)) height = _targetElement.ActualHeight;

            _initialElementPosition = new Point(left, top);
            _initialElementSize = new Size(width, height);

            rect.CaptureMouse();
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_activeHandle == null || _targetElement == null) return;

            var rect = (Rectangle)sender;
            if (!rect.IsMouseCaptured) return;

            // Opět zamezíme konfliktu s drag
            e.Handled = true;

            var canvas = GetParentCanvas(rect);
            if (canvas == null) return;

            var currentMousePosition = e.GetPosition(canvas);
            double deltaX = currentMousePosition.X - _initialMousePosition.X;
            double deltaY = currentMousePosition.Y - _initialMousePosition.Y;

            ResizeElement(deltaX, deltaY);
        }

        private void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var rect = (Rectangle)sender;
            rect.ReleaseMouseCapture();
            _activeHandle = null;
            e.Handled = true;
        }

        /// <summary>
        /// Provede výpočet a nastavení nové polohy a velikosti prvku (resize).
        /// </summary>
        private void ResizeElement(double deltaX, double deltaY)
        {
            if (_targetElement == null || _activeHandle == null) return;

            double newLeft = _initialElementPosition.X;
            double newTop = _initialElementPosition.Y;
            double newWidth = _initialElementSize.Width;
            double newHeight = _initialElementSize.Height;

            switch (_activeHandle)
            {
                case ResizeHandle.TopLeft:
                    newLeft += deltaX;
                    newTop += deltaY;
                    newWidth -= deltaX;
                    newHeight -= deltaY;
                    break;
                case ResizeHandle.TopCenter:
                    newTop += deltaY;
                    newHeight -= deltaY;
                    break;
                case ResizeHandle.TopRight:
                    newTop += deltaY;
                    newWidth += deltaX;
                    newHeight -= deltaY;
                    break;
                case ResizeHandle.MiddleLeft:
                    newLeft += deltaX;
                    newWidth -= deltaX;
                    break;
                case ResizeHandle.MiddleRight:
                    newWidth += deltaX;
                    break;
                case ResizeHandle.BottomLeft:
                    newLeft += deltaX;
                    newWidth -= deltaX;
                    newHeight += deltaY;
                    break;
                case ResizeHandle.BottomCenter:
                    newHeight += deltaY;
                    break;
                case ResizeHandle.BottomRight:
                    newWidth += deltaX;
                    newHeight += deltaY;
                    break;
            }

            // Zabránit záporné velikosti
            newWidth = Math.Max(newWidth, HandleSize);
            newHeight = Math.Max(newHeight, HandleSize);

            // Aplikovat novou polohu a rozměry
            Canvas.SetLeft(_targetElement, newLeft);
            Canvas.SetTop(_targetElement, newTop);
            _targetElement.Width = newWidth;
            _targetElement.Height = newHeight;

            // Po změně velikosti znovu spočítat polohu handle
            UpdateHandlesPosition();
        }

        private Canvas? GetParentCanvas(UIElement element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null && parent is not Canvas)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as Canvas;
        }

        // Interní číselník, který říká, který "roh" (handle) uživatel táhne.
        private enum ResizeHandle
        {
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }
    }
}
