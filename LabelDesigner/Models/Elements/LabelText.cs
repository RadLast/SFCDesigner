using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace LabelDesigner.Models.Elements
{
    /// <summary>
    /// Reprezentuje textový prvek na štítku.
    /// Obsahuje text, font, velikost písma a barvu.
    /// </summary>
    public class LabelText : LabelBase, INotifyPropertyChanged
    {
        #region Fields

        private string? _text;
        private string _fontFamily = "Segoe UI";
        private double _fontSize = 12;
        private FontWeight _fontWeight = FontWeights.Normal;
        private string _fontColor = "#000000"; // Výchozí černá barva

        #endregion

        #region Properties

        [Required]
        public string? Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        [Required]
        public string FontFamily
        {
            get => _fontFamily;
            set { _fontFamily = value; OnPropertyChanged(); }
        }

        [Range(1, 1000)]
        public double FontSize
        {
            get => _fontSize;
            set { _fontSize = value; OnPropertyChanged(); }
        }

        public FontWeight FontWeight
        {
            get => _fontWeight;
            set { _fontWeight = value; OnPropertyChanged(); }
        }

        public string FontColor
        {
            get => _fontColor;
            set { _fontColor = value; OnPropertyChanged(); }
        }

        #endregion

        #region INotifyPropertyChanged

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
