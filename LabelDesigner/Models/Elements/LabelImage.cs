using System.ComponentModel.DataAnnotations;

namespace LabelDesigner.Models.Elements
{
    /// <summary>
    /// Reprezentuje obrázkový prvek na štítku.
    /// Obsahuje ImagePath nebo Base64Data pro obrázek a Opacity.
    /// </summary>
    public class LabelImage : LabelBase
    {
        #region Fields

        private string? _base64Data;
        private string _imagePath = string.Empty;
        private double _opacity = 1.0;

        #endregion

        #region Properties

        public string? Base64Data
        {
            get => _base64Data;
            set { _base64Data = value; OnPropertyChanged(); }
        }

        [Required]
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(); }
        }

        [Range(0.0, 1.0)]
        public double Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(); }
        }

        #endregion
    }
}
