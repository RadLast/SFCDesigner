using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SFCDesigner.Helpers
{
    /// <summary>
    /// Poskytuje mapování pojmenovaných barev (string) na <see cref="Color"/> a zpět.
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>
        /// Mapa pojmenovaných barev na jejich WPF <see cref="Color"/>.
        /// </summary>
        private static readonly Dictionary<string, Color> NamedColors = new()
        {
            { "Black",  Colors.Black  },
            { "White",  Colors.White  },
            { "Red",    Colors.Red    },
            { "Green",  Colors.Green  },
            { "Blue",   Colors.Blue   },
            { "Orange", Colors.Orange },
            { "Yellow", Colors.Yellow },
            { "Purple", Colors.Purple }
        };

        /// <summary>
        /// Vrať všechny pojmenované barvy (klíče).
        /// </summary>
        public static IEnumerable<string> GetAllColorNames()
        {
            return NamedColors.Keys;
        }

        /// <summary>
        /// Pokusí se najít <see cref="Color"/> pro daný název barvy (např. "Black", "Orange").
        /// Pokud jej nezná, vrátí <see cref="Colors.Black"/>.
        /// </summary>
        public static Color GetColorFromName(string colorName)
        {
            if (NamedColors.TryGetValue(colorName, out var color))
                return color;

            // Fallback
            return Colors.Black;
        }

        /// <summary>
        /// Projde <see cref="NamedColors"/> a zkusí najít klíč podle <see cref="Color"/>.
        /// Pokud není nalezen, vrátí hex ve tvaru #RRGGBB (bez Alfa).
        /// </summary>
        public static string GetNameFromColor(Color color)
        {
            // Pokud je v NamedColors
            foreach (var kvp in NamedColors)
            {
                if (kvp.Value.Equals(color))
                {
                    return kvp.Key;
                }
            }

            // Fallback: vrátíme hex formát #RRGGBB
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}