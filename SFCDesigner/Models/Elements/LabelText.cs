using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SFCDesigner.Models.Elements
{
    /// <summary>
    /// Reprezentuje textový prvek na štítku.
    /// Obsahuje text, font, velikost písma a barvu.
    /// </summary>
    public class LabelText : LabelBase
    {
        #region Fields

        private string? _text;
        private string _fontFamily = "Segoe UI";
        private double _fontSize = 12;
        private FontWeight _fontWeight = FontWeights.Normal;
        private string _fontStyle = "Normal";
        private string _fontColorName = "Black";

        #endregion

        #region Properties

        /// <summary>
        /// Textový obsah (např. "Název produktu").
        /// </summary>
        [Required]
        public string? Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        /// <summary>
        /// Název rodiny písma (např. "Segoe UI").
        /// </summary>
        [Required]
        public string FontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        /// <summary>
        /// Velikost písma (1-1000).
        /// </summary>
        [Range(1, 1000)]
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        /// <summary>
        /// Slouží pouze ke serializaci do XML. Uchovává řetězec "Italic" / "Normal" apod.
        /// Změna se promítne do <see cref="FontStyle"/>.
        /// </summary>
        [XmlElement("FontStyle")]
        public string FontStyleString
        {
            get => _fontStyle;
            set
            {
                // Když se hodnota v _fontStyle změní, nahlásíme i změnu reálné WPF property
                if (SetProperty(ref _fontStyle, value))
                    OnPropertyChanged(nameof(FontStyle));
            }
        }

        /// <summary>
        /// Reálný <see cref="FontStyle"/> pro WPF (nabývá hodnot Normal / Italic atp.).
        /// V kódu ho používáme, v XML se neukládá (má k tomu <see cref="FontStyleString"/>).
        /// </summary>
        [XmlIgnore]
        public FontStyle FontStyle
        {
            get => _fontStyle == "Italic" ? FontStyles.Italic : FontStyles.Normal;
            set
            {
                // Převedeme FontStyle -> string
                var newValue = (value == FontStyles.Italic) ? "Italic" : "Normal";
                // Pokud se změní, SetProperty vrátí true a vyvolá OnPropertyChanged
                if (SetProperty(ref _fontStyle, newValue))
                {
                    // Změnila se i FontStyleString, tak to nahlásíme
                    OnPropertyChanged(nameof(FontStyleString));
                }
            }
        }

        /// <summary>
        /// Slouží pouze ke serializaci do XML. Ukládá "Bold" / "Normal" atd.
        /// Při nastavení ovlivňuje <see cref="FontWeight"/>.
        /// </summary>
        [XmlElement("FontWeight")]
        public string FontWeightXml
        {
            get => _fontWeight == FontWeights.Bold ? "Bold" : "Normal";
            set
            {
                // Konverze řetězce "Bold"/"Normal" na FontWeight
                var newWeight = (value == "Bold") ? FontWeights.Bold : FontWeights.Normal;
                // Pokud dojde ke změně _fontWeight, nahlásíme i "FontWeight"
                if (SetProperty(ref _fontWeight, newWeight))
                    OnPropertyChanged(nameof(FontWeight));
            }
        }

        /// <summary>
        /// Reálný <see cref="FontWeight"/> (Normal, Bold) pro WPF. 
        /// V kódu ho používáme, do XML se uloží <see cref="FontWeightXml"/>.
        /// </summary>
        [XmlIgnore]
        public FontWeight FontWeight
        {
            get => _fontWeight;
            set
            {
                if (SetProperty(ref _fontWeight, value))
                    OnPropertyChanged(nameof(FontWeightXml));
            }
        }

        /// <summary>
        /// Název/barva písma jako řetězec (např. "Black", "Orange"), ukládá se do XML.
        /// Případně může obsahovat i hex formát (#RRGGBB).
        /// </summary>
        [XmlElement("FontColor")]
        public string FontColorName
        {
            get => _fontColorName;
            set => SetProperty(ref _fontColorName, value);
        }

        #endregion
    }
}