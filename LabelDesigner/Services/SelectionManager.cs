using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LabelDesigner.Services
{
    /// <summary>
    /// Spravuje výběr objektů na plátně a jejich zvýraznění.
    /// Při výběru prvku vytvoří SelectionAdorner.
    /// Při zrušení výběru adorner odstraní.
    /// </summary>
    public class SelectionManager
    {
        #region Fields

        private UIElement? selectedObject;
        private Adorner? selectionAdorner;

        #endregion

        #region Events

        /// <summary>
        /// Událost vyvolaná při změně výběru.
        /// UIElement? je nově vybraný objekt, nebo null.
        /// </summary>
        public event Action<UIElement?>? SelectionChanged;

        #endregion

        #region Public Methods

        public UIElement? GetSelectedObject()
        {
            return selectedObject;
        }

        /// <summary>
        /// Vybere daný UIElement a zvýrazní ho pomocí SelectionAdorner.
        /// </summary>
        public void Select(UIElement element)
        {
            Debug.WriteLine("Select method called.");

            // Odstraníme starý adorner, pokud byl
            if (selectionAdorner != null && selectedObject != null)
            {
                Debug.WriteLine("Removing old selectionAdorner.");
                var oldAdornerLayer = AdornerLayer.GetAdornerLayer(selectedObject);
                oldAdornerLayer?.Remove(selectionAdorner);
                selectionAdorner = null;
            }

            Debug.WriteLine("Creating new SelectionAdorner.");
            selectionAdorner = new SelectionAdorner(element);
            var adornerLayer = AdornerLayer.GetAdornerLayer(element);

            if (adornerLayer != null)
            {
                Debug.WriteLine("Adding SelectionAdorner to layer.");
                adornerLayer.Add(selectionAdorner);
                selectedObject = element;
                SelectionChanged?.Invoke(selectedObject);
            }
            else
            {
                Debug.WriteLine("AdornerLayer not found!");
                SelectionChanged?.Invoke(null);
            }
        }

        /// <summary>
        /// Aktualizuje pozici zvýraznění, pokud se prvek posune.
        /// </summary>
        public void UpdateSelectionBorder(double newLeft, double newTop)
        {
            if (selectedObject != null)
            {
                Canvas.SetLeft(selectedObject, newLeft);
                Canvas.SetTop(selectedObject, newTop);
            }
        }

        /// <summary>
        /// Zruší výběr a odstraní SelectionAdorner.
        /// </summary>
        public void ClearSelection()
        {
            if (selectionAdorner != null && selectedObject != null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(selectedObject);
                adornerLayer?.Remove(selectionAdorner);
                selectionAdorner = null;
                selectedObject = null;
                SelectionChanged?.Invoke(null);
            }
        }

        #endregion
    }
}
