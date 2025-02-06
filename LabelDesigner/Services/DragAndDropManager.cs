using LabelDesigner.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LabelDesigner.Services
{
    public class DragAndDropManager
    {
        private UIElement? _selectedElement;
        private Point _startPoint;
        private bool _isDragging = false;

        private readonly SelectionManager _selectionManager;
        private readonly MainViewModel _mainViewModel;

        public DragAndDropManager(SelectionManager selectionManager, MainViewModel mainViewModel)
        {
            _selectionManager = selectionManager;
            _mainViewModel = mainViewModel;
        }

        /// <summary>
        /// Připojí události myši (drag) k danému UIElementu.
        /// </summary>
        public void AttachEvents(UIElement element)
        {
            element.MouseLeftButtonDown += StartDrag;
            element.MouseMove += Drag;
            element.MouseLeftButtonUp += EndDrag;
        }

        /// <summary>
        /// Zahájení drag (přesunu) prvku, pokud nezasáhl ResizeManager (úchopy) a nejde tedy o resize.
        /// </summary>
        private void StartDrag(object sender, MouseButtonEventArgs e)
        {
            // Pokud je událost už "handled" (např. ResizeManager ji zabral), nespouštíme drag
            if (e.Handled) return;

            // Pokud uživatel klikl na Rectangle (úchop pro resize), nespouštíme drag
            if (e.OriginalSource is Rectangle)
            {
                return;
            }

            Debug.WriteLine("StartDrag called.");
            if (sender is UIElement element)
            {
                Debug.WriteLine($"StartDrag: Sender is {element}.");

                // Nastavit element jako vybraný v SelectionManageru
                _selectionManager.Select(element);
                _selectedElement = element;

                var canvas = GetParentCanvas(element);
                if (canvas == null) return;

                // Zapamatovat si počáteční myší pozici v rámci Canvasu
                _startPoint = e.GetPosition(canvas);
                _isDragging = true;
                element.CaptureMouse();

                // Zabráníme další propagaci události
                e.Handled = true;
            }
        }

        /// <summary>
        /// Při pohybu myší, pokud jsme v režimu drag, posunout prvek v Canvasu.
        /// </summary>
        private void Drag(object sender, MouseEventArgs e)
        {
            if (e.Handled) return;
            if (_isDragging && _selectedElement != null)
            {
                var canvas = GetParentCanvas(_selectedElement);
                if (canvas == null) return;

                var currentPoint = e.GetPosition(canvas);
                double offsetX = currentPoint.X - _startPoint.X;
                double offsetY = currentPoint.Y - _startPoint.Y;

                double newLeft = Canvas.GetLeft(_selectedElement) + offsetX;
                double newTop = Canvas.GetTop(_selectedElement) + offsetY;

                // 1) Najdeme ViewModel příslušný k taženému UIElementu
                var vm = _mainViewModel.GetViewModel(_selectedElement);

                if (vm is ImageElementViewModel imageVm)
                {
                    imageVm.LocationX = newLeft;
                    imageVm.LocationY = newTop;
                }
                else if (vm is BarcodeElementViewModel barcodeVm)
                {
                    barcodeVm.LocationX = newLeft;
                    barcodeVm.LocationY = newTop;
                }
                else if (vm is QrCodeElementViewModel qrVm)
                {
                    qrVm.LocationX = newLeft;
                    qrVm.LocationY = newTop;
                }
                else if (vm is TextElementViewModel textVm)
                {
                    textVm.LocationX = newLeft;
                    textVm.LocationY = newTop;
                }

                // 3) Aktualizace rámečku výběru
                _selectionManager.UpdateSelectionBorder(newLeft, newTop);

                _startPoint = currentPoint;
            }
        }


        /// <summary>
        /// Ukončení drag při uvolnění levého tlačítka myši.
        /// </summary>
        private void EndDrag(object sender, MouseButtonEventArgs e)
        {
            // Pokud už resize (úchop) vyřešil událost, nekonfliktujeme
            if (e.Handled) return;

            if (_selectedElement != null)
            {
                // Uvolnit mouse capture
                _selectedElement.ReleaseMouseCapture();
                _selectedElement = null;
                _isDragging = false;
            }
        }

        /// <summary>
        /// Najde rodičovský Canvas pro daný prvek (do kterého pak mapujeme souřadnice myši).
        /// </summary>
        private Canvas? GetParentCanvas(UIElement element)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            while (parent != null && parent is not Canvas)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as Canvas;
        }
    }
}
