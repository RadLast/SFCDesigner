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

namespace LabelDesigner.Services
{
    /// <summary>
    /// Spravuje ukládání a načítání rozložení prvků z/do XML.
    /// Převádí UIElementy na datové modely (LabelBase) a naopak.
    /// </summary>
    public class LayoutManager
    {
        #region Public Methods - Layout Operations

        /// <summary>
        /// Uloží kolekci objektů do XML souboru pomocí serializace.
        /// </summary>
        public void SaveLayout<T>(List<T> objects, string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<T>));
            using var writer = new StreamWriter(filePath);
            serializer.Serialize(writer, objects);
        }

        /// <summary>
        /// Načte kolekci objektů z XML souboru pomocí deserializace.
        /// </summary>
        public List<T> LoadLayout<T>(string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<T>));
            using var reader = new StreamReader(filePath);
            return (List<T>)serializer.Deserialize(reader);
        }

        /// <summary>
        /// Uloží prvky z CanvasElements do XML souboru jako LabelBase modely.
        /// </summary>
        public void SaveCanvasLabels(IEnumerable<UIElement> elements)
        {
            // Zobrazit dialog pro uložení souboru
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
                            label.Layer = Canvas.GetZIndex(fe); // Nastavení vrstvy
                            labels.Add(label);
                        }
                    }
                }

                SaveLayout(labels, filePath);
            }
        }

        /// <summary>
        /// Načte LabelBase objekty z XML a vrátí je jako kolekci.
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
                return LoadLayout<LabelBase>(filePath);
            }

            // Pokud uživatel zruší dialog, vrátíme prázdný seznam
            return new List<LabelBase>();
        }

        #endregion

        #region Private Methods - Conversion

        /// <summary>
        /// Převod FrameworkElementu na LabelBase model (serializace).
        /// </summary>
        private LabelBase? ConvertToLabel(FrameworkElement element)
        {
            int layer = Canvas.GetZIndex(element);

            int id = (element.Tag is string tag && int.TryParse(tag, out var existingId))
                     ? existingId
                     : IdGenerator.GetNextId();

            switch (element)
            {
                case TextBlock textBlock:
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
                case System.Windows.Controls.Image image:
                    if (image.Source is BitmapSource bitmap)
                    {
                        return new LabelImage
                        {
                            ID = id,
                            Layer = layer,
                            Base64Data = ConvertBitmapToBase64(bitmap),
                            LocationX = Canvas.GetLeft(image),
                            LocationY = Canvas.GetTop(image),
                            Width = image.Width,
                            Height = image.Height
                        };
                    }
                    break;
            }
            return null;
        }

        /// <summary>
        /// Převod LabelBase modelu zpět na UIElement (deserializace).
        /// </summary>
        public UIElement? ConvertToUIElement(LabelBase label, ElementFactory factory)
        {
            switch (label)
            {
                case LabelText text:
                    var textElement = factory.CreateTextBlock(text);

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

                case LabelImage image:
                    if (!string.IsNullOrEmpty(image.Base64Data))
                    {
                        var bitmap = ConvertBase64ToBitmap(image.Base64Data);
                        var imageElement = new System.Windows.Controls.Image
                        {
                            Source = bitmap,
                            Width = image.Width,
                            Height = image.Height
                        };

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
            }
            return null;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Konvertuje Brush na hexadecimální barvu (string).
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
        /// Konvertuje BitmapSource na Base64.
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
        /// Konvertuje Base64 řetězec na BitmapImage.
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
