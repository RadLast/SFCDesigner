using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LabelDesigner.Services
{
    /// <summary>
    /// Adorner pro zvýraznění vybraného prvku na plátně.
    /// Kolem prvku se vykreslí modrý obdélník.
    /// </summary>
    public class SelectionAdorner : Adorner
    {
        #region Fields

        private readonly UIElement adornedElement;

        #endregion

        #region Constructor

        public SelectionAdorner(UIElement adornedElement) : base(adornedElement)
        {
            this.adornedElement = adornedElement;
            this.IsHitTestVisible = false;
        }

        #endregion

        #region Overridden Methods

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var pen = new Pen(Brushes.Blue, 2);
            var rect = new Rect(0, 0, adornedElement.RenderSize.Width, adornedElement.RenderSize.Height);
            drawingContext.DrawRectangle(null, pen, rect);
        }

        #endregion
    }
}
