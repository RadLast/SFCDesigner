using System.ComponentModel.DataAnnotations;

namespace SFCDesigner.Models.Elements
{
    /// <summary>
    /// Reprezentuje čárový kód jako prvek štítku.
    /// Obsahuje vlastnosti Data a BarcodeType.
    /// </summary>
    public class LabelBarcode : LabelBase
    {
        #region Fields

        private string? _data;
        private string? _barcodeType;

        #endregion

        #region Properties

        /// <summary>
        /// Obsah čárového kódu (např. "1234567890128").
        /// </summary>
        [Required]
        public string? Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        /// <summary>
        /// Typ čárového kódu (např. "EAN", "Code39").
        /// </summary>
        [Required]
        public string? BarcodeType
        {
            get => _barcodeType;
            set => SetProperty(ref _barcodeType, value);
        }

        #endregion
    }
}