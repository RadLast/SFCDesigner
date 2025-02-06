using LabelDesigner.Services;
using System.ComponentModel.DataAnnotations;

namespace LabelDesigner.Models.Elements
{
    /// <summary>
    /// Reprezentuje obrázkový prvek na štítku.
    /// Obsahuje Base64Data, ImagePath, Opacity a nově Title.
    /// </summary>
    public class LabelImage : LabelBase
    {
        private string? _base64Data;
        private string _imagePath = string.Empty;
        private double _opacity = 1.0;
        private string _title = string.Empty; // nová vlastnost

        [Required]
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(); }
        }

        public string? Base64Data
        {
            get => _base64Data;
            set { _base64Data = value; OnPropertyChanged(); }
        }

        [Range(0.0, 1.0)]
        public double Opacity
        {
            get => _opacity;
            set { _opacity = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Uživatelský (editovatelný) název obrázku (např. "Logo firmy").
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }
}
