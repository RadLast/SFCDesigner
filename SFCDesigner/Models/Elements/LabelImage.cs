using System.ComponentModel.DataAnnotations;

namespace SFCDesigner.Models.Elements
{
    /// <summary>
    /// Reprezentuje obrázkový prvek na štítku.
    /// Obsahuje Base64Data, ImagePath, Opacity a Title.
    /// </summary>
    public class LabelImage : LabelBase
    {
        #region Fields

        private string? _base64Data;
        private string _imagePath = string.Empty;
        private double _opacity = 1.0;
        private string _title = string.Empty;

        #endregion

        #region Properties

        [Required]
        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        /// <summary>
        /// Base64 kódovaný obsah obrázku (pokud se nepoužívá ImagePath přímo).
        /// </summary>
        public string? Base64Data
        {
            get => _base64Data;
            set => SetProperty(ref _base64Data, value);
        }

        /// <summary>
        /// Průhlednost obrázku v rozmezí 0.0 - 1.0.
        /// </summary>
        [Range(0.0, 1.0)]
        public double Opacity
        {
            get => _opacity;
            set => SetProperty(ref _opacity, value);
        }

        /// <summary>
        /// Uživatelský (editovatelný) název obrázku (např. "Logo firmy").
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        #endregion
    }
}