using LabelDesigner.Models.Elements;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelDesigner.Services
{
    /// <summary>
    /// Továrna na vytváření UIElementů (TextBlock, Image, ... ) z datových modelů (LabelBase).
    /// Dodržuje SRP: řeší pouze převod datových modelů na UI prvky.
    /// </summary>
    public class ElementFactory
    {
        #region Public Methods

        /// <summary>
        /// Vytvoří UIElement odpovídající typu LabelBase (Text, Image, Barcode).
        /// </summary>
        public UIElement CreateUIElement(LabelBase label)
        {
            switch (label)
            {
                case LabelText text:
                    return CreateTextBlock(text);
                case LabelImage image:
                    return CreateImage(image);
                case LabelBarcode barcode:
                    return CreateBarcode(barcode);
                default:
                    return new TextBlock { Text = "Neznámý prvek" };
            }
        }

        /// <summary>
        /// Vytvoří TextBlock z LabelText modelu.
        /// </summary>
        public TextBlock CreateTextBlock(LabelText textElement)
        {
            try
            {
                Debug.WriteLine($"Původní hodnota barvy: {textElement.FontColor}");
                var adjustedColor = AdjustColorFormat(textElement.FontColor);
                Debug.WriteLine($"Upravená hodnota barvy: {adjustedColor}");

                return new TextBlock
                {
                    Text = textElement.Text,
                    FontSize = textElement.FontSize,
                    FontFamily = new FontFamily(textElement.FontFamily),
                    FontWeight = textElement.FontWeight,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(AdjustColorFormat(textElement.FontColor))),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(5)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chyba při vytváření TextBlocku: {ex.Message}");
                return new TextBlock
                {
                    Text = textElement.Text,
                    FontSize = textElement.FontSize,
                    FontFamily = new FontFamily(textElement.FontFamily),
                    FontWeight = textElement.FontWeight,
                    Foreground = Brushes.Black,
                    Background = Brushes.Transparent,
                    Padding = new Thickness(5)
                };
            }
        }


        /// <summary>
        /// Vytvoří Image z LabelImage modelu, načtením bitmapy z ImagePath nebo Base64.
        /// </summary>
        public Image CreateImage(LabelImage imageElement)
        {
            BitmapImage bitmap = new BitmapImage();
            if (!string.IsNullOrEmpty(imageElement.ImagePath))
            {
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageElement.ImagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
            }
            else if (!string.IsNullOrEmpty(imageElement.Base64Data))
            {
                bitmap = ConvertBase64ToBitmap(imageElement.Base64Data);
            }

            return new Image
            {
                Source = bitmap,
                Width = imageElement.Width,
                Height = imageElement.Height,
                Opacity = imageElement.Opacity
            };
        }

        /// <summary>
        /// Vytvoří prvek pro LabelBarcode. Lze rozšířit pomocí externích knihoven.
        /// Zde jen TextBlock.
        /// </summary>
        public UIElement CreateBarcode(LabelBarcode barcodeElement)
        {
            return new TextBlock
            {
                Text = barcodeElement.Data,
                FontSize = 16,
                Foreground = Brushes.Black
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Konvertuje Base64 řetězec na BitmapImage.
        /// </summary>
        private BitmapImage ConvertBase64ToBitmap(string base64)
        {
            var bitmap = new BitmapImage();
            var binaryData = System.Convert.FromBase64String(base64);
            using var memoryStream = new MemoryStream(binaryData);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            return bitmap;
        }

        /// <summary>
        /// Upraví formát barvy z ARGB na RRGGBB, pokud je potřeba.
        /// </summary>
        private string AdjustColorFormat(string color)
        {
            if (color.StartsWith("#") && color.Length == 9) // Pokud je ve formátu #AARRGGBB
            {
                var adjustedColor = $"#{color.Substring(3)}"; // Odstranění prvních dvou znaků (alfa-kanálu)
                Debug.WriteLine($"AdjustColorFormat - Původní: {color}, Upravená: {adjustedColor}");
                return adjustedColor;
            }

            Debug.WriteLine($"AdjustColorFormat - Nezměněná barva: {color}");
            return color; // Vrací původní hodnotu, pokud není ve formátu #AARRGGBB
        }


        #endregion
    }
}
