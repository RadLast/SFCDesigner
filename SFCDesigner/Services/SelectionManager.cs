using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SFCDesigner.Services
{
    /// <summary>
    /// Spravuje výběr (označení) objektů na Canvasu a jejich zvýraznění 
    /// pomocí <see cref="SelectionResizeAdorner"/>.
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
        /// Předává nově vybraný objekt (nebo null).
        /// </summary>
        public event Action<UIElement?>? SelectionChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Vrátí aktuálně vybraný (označený) prvek.
        /// </summary>
        public UIElement? GetSelectedObject()
        {
            return selectedObject;
        }

        /// <summary>
        /// Vybere (označí) daný UIElement a zvýrazní ho pomocí <see cref="SelectionResizeAdorner"/>.
        /// Pokud byl již vybraný jiný objekt, odebere se z něj starý adorner.
        /// </summary>
        public void Select(UIElement element)
        {
            // Pokud existoval starý adorner, odstraníme ho.
            if (selectionAdorner != null && selectedObject != null)
            {
                var oldAdornerLayer = AdornerLayer.GetAdornerLayer(selectedObject);
                oldAdornerLayer?.Remove(selectionAdorner);
                selectionAdorner = null;
            }

            // Vytvoříme nový adorner pro vybraný prvek
            selectionAdorner = new SelectionResizeAdorner(element);
            var adornerLayer = AdornerLayer.GetAdornerLayer(element);

            if (adornerLayer != null)
            {
                adornerLayer.Add(selectionAdorner);
                selectedObject = element;
                SelectionChanged?.Invoke(selectedObject);
            }
            else
            {
                // Pokud se nepodařilo najít AdornerLayer (nestandardní situace)
                SelectionChanged?.Invoke(null);
            }
        }

        /// <summary>
        /// Aktualizuje pozici (Left/Top) vybraného objektu 
        /// - typicky voláno při drag & drop.
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
        /// Zruší výběr (označení) aktuálního prvku a odstraní adorner.
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