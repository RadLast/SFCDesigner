using SFCDesigner.Models.Elements;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace SFCDesigner.Services
{
    /// <summary>
    /// Vytváří UIElementy (TextBlock, Image apod.) z datových modelů (LabelBase).
    /// Dodržuje princip oddělené zodpovědnosti (SRP).
    /// </summary>
    public class ElementFactory
    {
        #region Public Methods

        /// <summary>
        /// Vytvoří UIElement podle typu předaného LabelBase (Text, Image, Barcode, ...).
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
                    // Můžeš sem doplnit další typy, např. LabelQrCode atd.
                    // Nebo vracet "Unknown" text, pokud třída není podporována.
                    return new TextBlock { Text = "Neznámý prvek" };
            }
        }

        /// <summary>
        /// Vytvoří TextBlock z <see cref="LabelText"/>.
        /// </summary>
        public TextBlock CreateTextBlock(LabelText textElement)
        {
            try
            {
                return new TextBlock
                {
                    Text = textElement.Text,
                    FontSize = textElement.FontSize,
                    FontFamily = new FontFamily(textElement.FontFamily),
                    FontStyle = textElement.FontStyle,
                    FontWeight = textElement.FontWeight,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textElement.FontColorName)),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(5),
                    DataContext = textElement
                };
            }
            catch (Exception)
            {
                // Pokud selže konverze barvy atd., padne sem.
                // Vrací fallback s černým textem.
                return new TextBlock
                {
                    Text = textElement.Text,
                    FontSize = textElement.FontSize,
                    FontFamily = new FontFamily(textElement.FontFamily),
                    FontStyle = textElement.FontStyle,
                    FontWeight = textElement.FontWeight,
                    Foreground = Brushes.Black,
                    Background = Brushes.Transparent,
                    Padding = new Thickness(5),
                    DataContext = textElement
                };
            }
        }

        /// <summary>
        /// Vytvoří Image z <see cref="LabelImage"/>, načtením bitmapy z ImagePath nebo Base64.
        /// </summary>
        public Image CreateImage(LabelImage imageElement)
        {
            var bitmap = new BitmapImage();

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
                Opacity = imageElement.Opacity,
                DataContext = imageElement
            };
        }

        /// <summary>
        /// Vytvoří barcode prvek z <see cref="LabelBarcode"/>.
        /// </summary>
        public UIElement CreateBarcode(LabelBarcode model)
        {
            var image = new Image
            {
                Width = 150,
                Height = 50,
                DataContext = model
            };

            try
            {
                // Můžeš přidat další varianty (Code128, Code93, EAN8...) 
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
                    Renderer = new BitmapRenderer()
                };

                var bmp = writer.Write(model.Data);
                image.Source = ConvertToBitmapImage(bmp);
            }
            catch (Exception ex)
            {
                // Pokud dojde k chybě při generování barcode, můžeme ukázat info uživateli.
                MessageBox.Show("Chyba při generování barcode: " + ex.Message);
            }

            return image;
        }

        /// <summary>
        /// Vytvoří QR kód z <see cref="LabelQrCode"/>.
        /// </summary>
        /// <param name="model">Model reprezentující QR kód.</param>
        public UIElement CreateQrCode(LabelQrCode model)
        {
            var image = new Image
            {
                Width = 150,
                Height = 150,
                DataContext = model
            };

            var writer = new BarcodeWriter<System.Drawing.Bitmap>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = (int)image.Width,
                    Height = (int)image.Height,
                    Margin = 0
                },
                Renderer = new BitmapRenderer()
            };

            var bmp = writer.Write(model.Data);
            image.Source = ConvertToBitmapImage(bmp);
            return image;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Konvertuje Base64 řetězec na <see cref="BitmapImage"/>.
        /// </summary>
        private BitmapImage ConvertBase64ToBitmap(string base64)
        {
            var bitmap = new BitmapImage();
            var binaryData = Convert.FromBase64String(base64);
            using var memoryStream = new MemoryStream(binaryData);

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();

            return bitmap;
        }

        /// <summary>
        /// Konvertuje <see cref="System.Drawing.Bitmap"/> na WPF <see cref="BitmapImage"/>.
        /// </summary>
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