using SFCDesigner.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SFCDesigner.Services
{
    /// <summary>
    /// Zajišťuje logiku drag-and-drop (přesunu) UI prvků po Canvasu.
    /// </summary>
    public class DragAndDropManager
    {
        #region Fields

        private UIElement? _selectedElement;
        private Point _startPoint;
        private bool _isDragging = false;

        private readonly SelectionManager _selectionManager;
        private readonly MainViewModel _mainViewModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Vytváří novou instanci <see cref="DragAndDropManager"/>.
        /// </summary>
        public DragAndDropManager(SelectionManager selectionManager, MainViewModel mainViewModel)
        {
            _selectionManager = selectionManager;
            _mainViewModel = mainViewModel;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Připojí k danému UIElementu události myši potřebné pro drag-and-drop.
        /// </summary>
        public void AttachEvents(UIElement element)
        {
            element.MouseLeftButtonDown += StartDrag;
            element.MouseMove += Drag;
            element.MouseLeftButtonUp += EndDrag;
        }

        /// <summary>
        /// Odpojí od daného UIElementu události myši potřebné pro drag-and-drop.
        /// </summary>
        public void DetachEvents(UIElement element)
        {
            element.MouseLeftButtonDown -= StartDrag;
            element.MouseMove -= Drag;
            element.MouseLeftButtonUp -= EndDrag;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Začátek přesunu (drag) prvku, pokud není locked a nekliklo se na resize úchop.
        /// </summary>
        private void StartDrag(object sender, MouseButtonEventArgs e)
        {
            // Pokud je událost již označena jako zpracovaná, nic neděláme.
            if (e.Handled)
                return;

            // Pokud byl kliknut Rectangle (úchop pro resize), neprovádíme drag.
            if (e.OriginalSource is Rectangle)
                return;

            if (sender is UIElement element)
            {
                // Pokud je prvek uzamčen, drag se nespustí.
                if (element is FrameworkElement fe && fe.DataContext is Models.Elements.LabelBase lb && lb.Locked)
                    return;

                // Označíme prvek jako vybraný.
                _selectionManager.Select(element);
                _selectedElement = element;

                var canvas = GetParentCanvas(element);
                if (canvas == null)
                    return;

                // Zapamatujeme si počáteční souřadnici myši v rámci Canvasu.
                _startPoint = e.GetPosition(canvas);
                _isDragging = true;
                element.CaptureMouse();

                // Zabráníme dalšímu zpracování této události.
                e.Handled = true;
            }
        }

        /// <summary>
        /// Provádí samotný posun prvku, pokud je zapnutý režim drag.
        /// </summary>
        private void Drag(object sender, MouseEventArgs e)
        {
            // Pokud je událost již označena jako zpracovaná, nic neděláme.
            if (e.Handled)
                return;

            if (_isDragging && _selectedElement != null)
            {
                var canvas = GetParentCanvas(_selectedElement);
                if (canvas == null)
                    return;

                // Vypočítáme rozdíl mezi aktuální a počáteční pozicí.
                var currentPoint = e.GetPosition(canvas);
                double offsetX = currentPoint.X - _startPoint.X;
                double offsetY = currentPoint.Y - _startPoint.Y;

                double newLeft = Canvas.GetLeft(_selectedElement) + offsetX;
                double newTop = Canvas.GetTop(_selectedElement) + offsetY;

                // Najdeme příslušný ViewModel a aktualizujeme v něm polohu.
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
                else if (vm is LayoutViewModel layoutVm)
                {
                    layoutVm.LocationX = newLeft;
                    layoutVm.LocationY = newTop;
                }

                // Aktualizujeme také rámeček označení.
                _selectionManager.UpdateSelectionBorder(newLeft, newTop);

                // Posuneme "start point" na současnou pozici.
                _startPoint = currentPoint;
            }
        }

        /// <summary>
        /// Ukončuje drag režim při uvolnění levého tlačítka myši.
        /// </summary>
        private void EndDrag(object sender, MouseButtonEventArgs e)
        {
            // Pokud je událost již označena jako zpracovaná, nic neděláme.
            if (e.Handled)
                return;

            if (_selectedElement != null)
            {
                _selectedElement.ReleaseMouseCapture();
                _selectedElement = null;
                _isDragging = false;
            }
        }

        /// <summary>
        /// Najde nadřazený Canvas pro daný UIElement.
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

        #endregion
    }
}