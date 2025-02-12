namespace SFCDesigner.Models.Elements
{
    /// <summary>
    /// Speciální prvek představující fyzické rozměry štítku (layout).
    /// </summary>
    public class LabelLayout : LabelBase
    {
        #region Fields

        private double _cornerRadius = 20;

        #endregion

        #region Properties

        /// <summary>
        /// Poloměr zakulacení rohů štítku (pokud je relevantní pro vykreslení).
        /// </summary>
        public double CornerRadius
        {
            get => _cornerRadius;
            set => SetProperty(ref _cornerRadius, value);
        }

        /// <summary>
        /// Přebíjíme, aby layout vždy začínal na X=0 (nelze jej posouvat).
        /// </summary>
        public override double LocationX
        {
            get => 0;
            set => base.LocationX = 0;
        }

        /// <summary>
        /// Přebíjíme, aby layout vždy začínal na Y=0 (nelze jej posouvat).
        /// </summary>
        public override double LocationY
        {
            get => 0;
            set => base.LocationY = 0;
        }

        #endregion
    }
}