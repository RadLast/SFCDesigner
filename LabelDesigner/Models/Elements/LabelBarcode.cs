using System.ComponentModel.DataAnnotations;

namespace LabelDesigner.Models.Elements
{
    /// <summary>
    /// Reprezentuje čárový kód jako prvek štítku.
    /// Obsahuje Data (hodnotu kódu) a BarcodeType (typ kódu, např. Code128).
    /// </summary>
    public class LabelBarcode : LabelBase
    {
        #region Fields

        private string _data = string.Empty;
        private string _barcodeType = "Code128";

        #endregion

        #region Properties

        [Required]
        public string Data
        {
            get => _data;
            set { _data = value; OnPropertyChanged(); }
        }

        [Required]
        public string BarcodeType
        {
            get => _barcodeType;
            set { _barcodeType = value; OnPropertyChanged(); }
        }

        #endregion
    }
}
