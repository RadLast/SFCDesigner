using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SFCDesigner.Helpers
{
    /// <summary>
    /// Poskytuje mapování pojmenovaných stylů písma (string)
    /// na reálné WPF typy <see cref="FontStyle"/> a <see cref="FontWeight"/>.
    /// </summary>
    public static class FontHelper
    {
        #region Fields

        /// <summary>
        /// Slovník, který mapuje název stylu (např. "Bold Italic")
        /// na dvojici (FontStyle, FontWeight), např. (Italic, Bold).
        /// </summary>
        private static readonly Dictionary<string, (FontStyle style, FontWeight weight)> NamedStyles = new()
        {
            { "Normal",      (FontStyles.Normal,  FontWeights.Normal) },
            { "Italic",      (FontStyles.Italic,  FontWeights.Normal) },
            { "Bold",        (FontStyles.Normal,  FontWeights.Bold)   },
            { "Bold Italic", (FontStyles.Italic,  FontWeights.Bold)   },
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Vrátí všechny pojmenované styly (např. "Normal", "Italic", "Bold", "Bold Italic").
        /// </summary>
        public static IEnumerable<string> GetAllStyleNames()
        {
            return NamedStyles.Keys;
        }

        /// <summary>
        /// Z řetězce (např. "Bold Italic") vrátí tuple (FontStyle, FontWeight).
        /// Pokud název nezná, vrátí (Normal, Normal) jako fallback.
        /// </summary>
        public static (FontStyle style, FontWeight weight) GetWpfFontStyle(string styleName)
        {
            if (NamedStyles.TryGetValue(styleName, out var wpfStyle))
            {
                return wpfStyle;
            }

            // Fallback, když neznáme název
            return (FontStyles.Normal, FontWeights.Normal);
        }

        /// <summary>
        /// Naopak vezme existující <see cref="FontStyle"/> a <see cref="FontWeight"/>
        /// a najde k nim pojmenovaný styl (např. "Bold Italic"). 
        /// Pokud se nenajde v mapě, vrátí "Normal".
        /// </summary>
        public static string GetNameFromWpfStyle(FontStyle style, FontWeight weight)
        {
            foreach (var kvp in NamedStyles)
            {
                if (kvp.Value.style == style && kvp.Value.weight == weight)
                {
                    return kvp.Key;
                }
            }

            return "Normal"; // fallback
        }

        #endregion
    }
}