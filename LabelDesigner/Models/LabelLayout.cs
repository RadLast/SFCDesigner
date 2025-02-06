using System;

namespace LabelDesigner.Models.Elements
{
    /// <summary>
    /// Speciální prvek představující fyzické rozměry štítku (layout).
    /// </summary>
    public class LabelLayout : LabelBase
    {
        public double CornerRadius { get; set; } = 20;

        public override double LocationX
        {
            get => 0;
            set => base.LocationX = 0;
        }

        public override double LocationY
        {
            get => 0;
            set => base.LocationY = 0;
        }
    }
}