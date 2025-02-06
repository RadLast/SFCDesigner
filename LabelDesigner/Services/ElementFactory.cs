using LabelDesigner.Models.Elements;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

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
        public UIElement CreateBarcode(LabelBarcode model)
        {
            var image = new Image
            {
                Width = 150,
                Height = 50
            };

            try
            {
                // Rozhodneme se podle model.BarcodeType
                var barcodeFormat = BarcodeFormat.EAN_13;
                if (model.BarcodeType == "Code39")
                    barcodeFormat = BarcodeFormat.CODE_39;

                var writer = new BarcodeWriter<System.Drawing.Bitmap>()
                {
                    Format = barcodeFormat,
                    Options = new EncodingOptions
                    {
                        Width = (int)image.Width,
                        Height = (int)image.Height,
                        Margin = 0
                    },
                    Renderer = new BitmapRenderer() // z ZXing.Windows.Compatibility
                };

                var bmp = writer.Write(model.Data);
                image.Source = ConvertToBitmapImage(bmp);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba při generování barcode: " + ex.Message);
            }

            return image;
        }

        public UIElement CreateQrCode(LabelQrCode model)
        {
            var image = new Image
            {
                Width = 150,
                Height = 150
            };

            // Nyní definujte writer s typem <System.Drawing.Bitmap>
            var writer = new BarcodeWriter<System.Drawing.Bitmap>()
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = (int)image.Width,
                    Height = (int)image.Height,
                    Margin = 0
                },
                // DŮLEŽITÉ: Nastavte Renderer!
                Renderer = new BitmapRenderer()
            };

            // Teď už Write nebude házet výjimku
            var bmp = writer.Write(model.Data);

            image.Source = ConvertToBitmapImage(bmp);
            return image;
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

        private BitmapImage ConvertToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;

            var wpfBitmap = new BitmapImage();
            wpfBitmap.BeginInit();
            wpfBitmap.CacheOption = BitmapCacheOption.OnLoad;
            wpfBitmap.StreamSource = memoryStream;
            wpfBitmap.EndInit();
            return wpfBitmap;
        }

        #endregion
    }
}
