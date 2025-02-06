using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using LabelDesigner.Models.Elements;
using Microsoft.Win32;
using System.Diagnostics;

namespace LabelDesigner.Services
{
    /// <summary>
    /// Spravuje ukládání a načítání rozložení prvků z/do XML.
    /// Provádí konverzi mezi UIElement (Canvas) a datovými modely (LabelBase).
    /// </summary>
    public class LayoutManager
    {
        #region Public Methods - Layout Operations

        /// <summary>
        /// Uloží kolekci objektů (obecný typ T) do XML souboru pomocí serializace.
        /// </summary>
        /// <typeparam name="T">Typ objektů, které se budou ukládat.</typeparam>
        public void SaveLayout<T>(List<T> objects, string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<T>));
            using var writer = new StreamWriter(filePath);
            serializer.Serialize(writer, objects);
        }

        /// <summary>
        /// Načte kolekci objektů z XML souboru pomocí deserializace.
        /// </summary>
        /// <typeparam name="T">Typ objektů, které se budou načítat.</typeparam>
        public List<T> LoadLayout<T>(string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<T>));
            using var reader = new StreamReader(filePath);
            return (List<T>)serializer.Deserialize(reader);
        }

        /// <summary>
        /// Uloží prvky (UIElementy) z CanvasElements do XML souboru jako modely LabelBase.
        /// </summary>
        /// <param name="elements">Kolekce UIElementů (TextBlock, Image apod.).</param>
        public void SaveCanvasLabels(IEnumerable<UIElement> elements)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                DefaultExt = ".xml",
                AddExtension = true,
                Title = "Uložit rozložení"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var filePath = saveFileDialog.FileName;

                var labels = new List<LabelBase>();

                foreach (var element in elements)
                {
                    if (element is FrameworkElement fe)
                    {
                        var label = ConvertToLabel(fe);
                        if (label != null)
                        {
                            // Nastavení vrstvy (Canvas.ZIndex)
                            label.Layer = Canvas.GetZIndex(fe);
                            labels.Add(label);
                        }
                    }
                }

                SaveLayout(labels, filePath);
            }
        }

        /// <summary>
        /// Načte LabelBase objekty z XML a vrátí je jako kolekci.
        /// Automaticky volá IdGenerator.Initialize pro unikátní číslování.
        /// </summary>
        public List<LabelBase> LoadCanvasLabels()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                DefaultExt = ".xml",
                Title = "Načíst rozložení"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                var labels = LoadLayout<LabelBase>(filePath);

                // Získáme všechny ID > 0, abychom inicializovali IdGenerator
                var existingIds = labels
                    .Select(l => (l.GetType().Name, l.ID))
                    .Where(l => l.ID > 0);

                IdGenerator.Initialize(existingIds);
                return labels;
            }

            return new List<LabelBase>();
        }

        #endregion

        #region Private Methods - Conversion

        /// <summary>
        /// Převod FrameworkElementu na LabelBase model (serializace).
        /// Rozhoduje podle typu elementu (TextBlock / Image).
        /// </summary>
        private LabelBase? ConvertToLabel(FrameworkElement element)
        {
            int layer = Canvas.GetZIndex(element);

            switch (element)
            {
                // --- Layout prvek (Border) ---
                case Border border:
                    if (border.DataContext is LabelLayout layoutModel)
                    {
                        return new LabelLayout
                        {
                            ID = layoutModel.ID,
                            Layer = layer,
                            LocationX = Canvas.GetLeft(border),
                            LocationY = Canvas.GetTop(border),
                            Width = border.Width,
                            Height = border.Height,
                        };
                    }
                    break;
                // ====== TEXTBLOCK (LabelText) ======
                case TextBlock textBlock:
                    // Zkusíme DataContext, jestli tam je LabelText
                    if (textBlock.DataContext is LabelText textModel)
                    {
                        // Použijeme ID a další data z modelu
                        return new LabelText
                        {
                            ID = textModel.ID,
                            Layer = layer,
                            Text = textBlock.Text,
                            FontSize = textBlock.FontSize,
                            FontFamily = textBlock.FontFamily.Source,
                            FontWeight = textBlock.FontWeight,
                            FontColor = GetFontColor(textBlock.Foreground),
                            LocationX = Canvas.GetLeft(textBlock),
                            LocationY = Canvas.GetTop(textBlock),
                            Width = textBlock.ActualWidth,
                            Height = textBlock.ActualHeight
                        };
                    }
                    else
                    {
                        // Fallback: Tag / vygenerovat nové ID
                        int id = element.Tag is string tagString
                                  && int.TryParse(tagString, out var existingId)
                                  && existingId > 0
                            ? existingId
                            : IdGenerator.GetNextId(nameof(LabelText));

                        return new LabelText
                        {
                            ID = id,
                            Layer = layer,
                            Text = textBlock.Text,
                            FontSize = textBlock.FontSize,
                            FontFamily = textBlock.FontFamily.Source,
                            FontWeight = textBlock.FontWeight,
                            FontColor = GetFontColor(textBlock.Foreground),
                            LocationX = Canvas.GetLeft(textBlock),
                            LocationY = Canvas.GetTop(textBlock),
                            Width = textBlock.ActualWidth,
                            Height = textBlock.ActualHeight
                        };
                    }

                // ====== IMAGE (Barcode / QrCode / Image) ======
                case Image wpfImage:

                    // 1) Barcode
                    if (wpfImage.DataContext is LabelBarcode lb)
                    {
                        return new LabelBarcode
                        {
                            ID = lb.ID,
                            Layer = layer,
                            Data = lb.Data,
                            BarcodeType = lb.BarcodeType,
                            LocationX = Canvas.GetLeft(wpfImage),
                            LocationY = Canvas.GetTop(wpfImage),
                            Width = wpfImage.Width,
                            Height = wpfImage.Height
                        };
                    }
                    // 2) QrCode
                    else if (wpfImage.DataContext is LabelQrCode lq)
                    {
                        return new LabelQrCode
                        {
                            ID = lq.ID,
                            Layer = layer,
                            Data = lq.Data,
                            LocationX = Canvas.GetLeft(wpfImage),
                            LocationY = Canvas.GetTop(wpfImage),
                            Width = wpfImage.Width,
                            Height = wpfImage.Height
                        };
                    }
                    // 3) LabelImage
                    else if (wpfImage.DataContext is LabelImage li)
                    {
                        return new LabelImage
                        {
                            ID = li.ID,
                            Layer = layer,
                            LocationX = Canvas.GetLeft(wpfImage),
                            LocationY = Canvas.GetTop(wpfImage),
                            Width = wpfImage.Width,
                            Height = wpfImage.Height,
                            Base64Data = li.Base64Data,
                            Title = li.Title,
                            Opacity = li.Opacity,
                            ImagePath = li.ImagePath
                        };
                    }
                    // 4) Pokud DataContext není z výše uvedených,
                    //    ale Source je bitmapa, vytvoříme nový LabelImage
                    else if (wpfImage.Source is BitmapSource bmp)
                    {
                        int fallbackId = element.Tag is string tagStr
                                         && int.TryParse(tagStr, out var exId)
                                         && exId > 0
                            ? exId
                            : IdGenerator.GetNextId(nameof(LabelImage));

                        return new LabelImage
                        {
                            ID = fallbackId,
                            Layer = layer,
                            LocationX = Canvas.GetLeft(wpfImage),
                            LocationY = Canvas.GetTop(wpfImage),
                            Width = wpfImage.Width,
                            Height = wpfImage.Height,
                            Base64Data = ConvertBitmapToBase64(bmp)
                        };
                    }
                    break;
            }

            // Pokud žádný case nepasuje
            return null;
        }

        /// <summary>
        /// Převod LabelBase modelu zpět na UIElement (deserializace).
        /// Volá se z LoadCanvasLabels().
        /// </summary>
        public UIElement? ConvertToUIElement(LabelBase label, ElementFactory factory)
        {
            switch (label)
            {
                // ----- Layout -----
                case LabelLayout layoutModel:
                    // Vytvoříme Border
                    var border = new Border
                    {
                        Width = layoutModel.Width,
                        Height = layoutModel.Height,
                        Background = Brushes.WhiteSmoke,
                        BorderBrush = Brushes.DarkGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(layoutModel.CornerRadius),
                        DataContext = layoutModel
                    };
                    Canvas.SetLeft(border, layoutModel.LocationX);
                    Canvas.SetTop(border, layoutModel.LocationY);
                    Canvas.SetZIndex(border, layoutModel.Layer);
                    border.Tag = layoutModel.ID.ToString();
                    return border;

                // ----- Text -------
                case LabelText text:
                    var textElement = factory.CreateTextBlock(text);

                    // Doplníme velikost pro korektní vykreslení
                    textElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    textElement.Width = textElement.DesiredSize.Width;
                    textElement.Height = textElement.DesiredSize.Height;

                    Canvas.SetZIndex(textElement, label.Layer);

                    if (textElement is FrameworkElement textFrameworkElement)
                    {
                        textFrameworkElement.Tag = label.ID.ToString();
                        Canvas.SetLeft(textFrameworkElement, label.LocationX);
                        Canvas.SetTop(textFrameworkElement, label.LocationY);
                    }

                    return textElement;

                // ----- Obrázek -----
                case LabelImage image:
                    if (!string.IsNullOrEmpty(image.Base64Data))
                    {
                        var bitmap = ConvertBase64ToBitmap(image.Base64Data);
                        var imageElement = new Image
                        {
                            Source = bitmap,
                            Width = image.Width,
                            Height = image.Height,
                            DataContext = image // Nastavíme DataContext
                        };

                        // Pokud šířka/výška jsou 0 nebo NaN, změříme
                        if (imageElement.Width <= 0 || double.IsNaN(imageElement.Width) ||
                            imageElement.Height <= 0 || double.IsNaN(imageElement.Height))
                        {
                            imageElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                            imageElement.Width = imageElement.DesiredSize.Width;
                            imageElement.Height = imageElement.DesiredSize.Height;
                        }

                        Canvas.SetZIndex(imageElement, image.Layer);

                        if (imageElement is FrameworkElement imageFrameworkElement)
                        {
                            imageFrameworkElement.Tag = image.ID.ToString();
                            Canvas.SetLeft(imageFrameworkElement, image.LocationX);
                            Canvas.SetTop(imageFrameworkElement, image.LocationY);
                        }

                        return imageElement;
                    }
                    break;

                // ----- Barcode -----
                case LabelBarcode barcode:
                    // Vytvoříme control pro barcode (Image s vygenerovaným kódem)
                    var barcodeUi = factory.CreateBarcode(barcode);
                    if (barcodeUi is FrameworkElement bcFe)
                    {
                        bcFe.DataContext = barcode;
                        Canvas.SetLeft(bcFe, barcode.LocationX);
                        Canvas.SetTop(bcFe, barcode.LocationY);
                        Canvas.SetZIndex(bcFe, barcode.Layer);
                        bcFe.Tag = barcode.ID.ToString();
                    }
                    return barcodeUi;

                // ----- QrCode -----
                case LabelQrCode qr:
                    var qrUi = factory.CreateQrCode(qr);
                    if (qrUi is FrameworkElement qrFe)
                    {
                        qrFe.DataContext = qr;
                        Canvas.SetLeft(qrFe, qr.LocationX);
                        Canvas.SetTop(qrFe, qr.LocationY);
                        Canvas.SetZIndex(qrFe, qr.Layer);
                        qrFe.Tag = qr.ID.ToString();
                    }
                    return qrUi;
            }
            return null;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Konvertuje Brush (pouze SolidColorBrush) na hexadecimální řetězec.
        /// </summary>
        private string GetFontColor(Brush brush)
        {
            if (brush is SolidColorBrush solidColorBrush)
            {
                return solidColorBrush.Color.ToString();
            }
            return "#000000"; // Výchozí černá
        }

        /// <summary>
        /// Konvertuje BitmapSource na Base64 (PNG formát).
        /// </summary>
        private string ConvertBitmapToBase64(BitmapSource bitmap)
        {
            using var memoryStream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(memoryStream);
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        /// <summary>
        /// Konvertuje Base64 řetězec zpět na BitmapImage.
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

        #endregion
    }
}
