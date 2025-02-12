using SFCDesigner.Helpers;
using SFCDesigner.Models;
using SFCDesigner.Models.Elements;
using SFCDesigner.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SFCDesigner.Services
{
    /// <summary>
    /// Spravuje ukládání a načítání rozložení (LabelBase modelů) z/do XML.
    /// Konvertuje také mezi UIElement a datovým modelem.
    /// </summary>
    public class LayoutManager
    {
        #region Fields

        private readonly MetaDataViewModel _metaDataViewModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Vytváří novou instanci <see cref="LayoutManager"/> 
        /// s referencí na <see cref="MetaDataViewModel"/>.
        /// </summary>
        public LayoutManager(MetaDataViewModel metaDataViewModel)
        {
            _metaDataViewModel = metaDataViewModel;
        }

        #endregion

        #region Public Methods - Saving/Loading

        /// <summary>
        /// Uloží kolekci objektů (obecný typ T) do zadaného XML souboru.
        /// </summary>
        public void SaveLayout<T>(List<T> objects, string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<T>));
            using var writer = new StreamWriter(filePath);
            serializer.Serialize(writer, objects);

            // Aktualizujeme Info o názvu souboru v MetaDataViewModel
            _metaDataViewModel.CurrentFileName = Path.GetFileName(filePath);
        }

        /// <summary>
        /// Načte kolekci objektů z XML souboru.
        /// </summary>
        public List<T> LoadLayout<T>(string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<T>));
            using var reader = new StreamReader(filePath);

            // Nastavíme do metadataViewModelu, aby UI vědělo,
            // s jakým souborem právě pracujeme.
            _metaDataViewModel.CurrentFileName = Path.GetFileName(filePath);

            return (List<T>)serializer.Deserialize(reader);
        }

        /// <summary>
        /// Nabídne uložení prvků Canvasu (UIElementy) do XML souboru.
        /// Převádí je na modely <see cref="LabelBase"/>.
        /// </summary>
        public void SaveCanvasLabels(IEnumerable<UIElement> elements)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                DefaultExt = ".xml",
                AddExtension = true,
                FileName = _metaDataViewModel.CurrentFileName,
                Title = "Uložit rozložení"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var filePath = saveFileDialog.FileName;
                var labels = new List<LabelBase>();

                // Převedeme jednotlivé UIElementy na datové modely:
                foreach (var element in elements)
                {
                    if (element is FrameworkElement fe)
                    {
                        var label = ConvertToLabel(fe);
                        if (label != null)
                        {
                            // Zjistíme ZIndex (vrstvu) a uložíme do modelu
                            label.Layer = Canvas.GetZIndex(fe);
                            labels.Add(label);
                        }
                    }
                }

                // Uložíme do XML
                SaveLayout(labels, filePath);
            }
        }

        /// <summary>
        /// Nabídne dialog pro načtení <see cref="LabelBase"/> z XML
        /// a vrátí kolekci LabelBase.
        /// </summary>
        /// <returns>Seznam načtených modelů (LabelBase), nebo prázdný seznam.</returns>
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

                // Předáme seznam existujících ID do IdGeneratoru,
                // aby nedošlo ke kolizi ID při přidávání nových prvků.
                var existingIds = labels
                    .Select(l => (l.GetType().Name, l.ID))
                    .Where(x => x.ID > 0);

                IdGenerator.Initialize(existingIds);

                return labels;
            }

            return new List<LabelBase>();
        }

        #endregion

        #region Public Methods - Conversions

        /// <summary>
        /// Převede model (LabelBase) zpět na UIElement pomocí <see cref="ElementFactory"/>.
        /// </summary>
        /// <param name="label">Model prvku (Text, Image, Barcode atd.).</param>
        /// <param name="factory">Továrna vytvářející konkrétní UIElementy.</param>
        /// <returns>Vytvořený UIElement, nebo null.</returns>
        public UIElement? ConvertToUIElement(LabelBase label, ElementFactory factory)
        {
            switch (label)
            {
                case LabelLayout layoutModel:
                    return CreateBorderForLayout(layoutModel);

                case LabelText textModel:
                    var textBlock = factory.CreateTextBlock(textModel);
                    textBlock.Width = textModel.Width;
                    textBlock.Height = textModel.Height;
                    FinalizeFrameworkElementPosition(textBlock, textModel);
                    return textBlock;

                case LabelImage imageModel:
                    var image = factory.CreateImage(imageModel);
                    FinalizeFrameworkElementPosition(image, imageModel);
                    return image;

                case LabelBarcode barcodeModel:
                    var barcodeEl = factory.CreateBarcode(barcodeModel);
                    FinalizeFrameworkElementPosition(barcodeEl, barcodeModel);
                    return barcodeEl;

                case LabelQrCode qrModel:
                    var qrEl = factory.CreateQrCode(qrModel);
                    FinalizeFrameworkElementPosition(qrEl, qrModel);
                    return qrEl;
            }

            // Pokud typ neznáme
            return null;
        }

        #endregion

        #region Private Methods - Internal Conversions

        /// <summary>
        /// Vytvoří <see cref="Border"/> reprezentující layout (pozadí, rohy atd.).
        /// </summary>
        private UIElement CreateBorderForLayout(LabelLayout layoutModel)
        {
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

            // Nastavíme jeho pozici a vrstvu na Canvasu
            Canvas.SetLeft(border, layoutModel.LocationX);
            Canvas.SetTop(border, layoutModel.LocationY);
            Canvas.SetZIndex(border, layoutModel.Layer);
            border.Tag = layoutModel.ID.ToString();

            return border;
        }

        /// <summary>
        /// Převede <see cref="FrameworkElement"/> z Canvasu zpět na model <see cref="LabelBase"/>.
        /// </summary>
        private LabelBase? ConvertToLabel(FrameworkElement element)
        {
            int layer = Canvas.GetZIndex(element);

            // Layout -> Border s DataContextem = LabelLayout
            if (element is Border border && border.DataContext is LabelLayout layoutModel)
            {
                return new LabelLayout
                {
                    ID = layoutModel.ID,
                    Layer = layer,
                    LocationX = Canvas.GetLeft(border),
                    LocationY = Canvas.GetTop(border),
                    Width = border.Width,
                    Height = border.Height,
                    CornerRadius = layoutModel.CornerRadius,
                    Locked = layoutModel.Locked
                };
            }

            // TextBlock -> LabelText
            if (element is TextBlock textBlock)
            {
                if (textBlock.DataContext is LabelText textModel)
                {
                    return new LabelText
                    {
                        ID = textModel.ID,
                        Layer = layer,
                        Text = textBlock.Text,
                        FontSize = textBlock.FontSize,
                        FontFamily = textBlock.FontFamily.Source,
                        FontStyle = textBlock.FontStyle,
                        FontWeight = textBlock.FontWeight,
                        FontColorName = BrushToColorName(textBlock.Foreground),
                        LocationX = Canvas.GetLeft(textBlock),
                        LocationY = Canvas.GetTop(textBlock),
                        Width = textBlock.ActualWidth,
                        Height = textBlock.ActualHeight,
                        Locked = textModel.Locked
                    };
                }
            }

            // Image -> Barcode / QrCode / LabelImage
            if (element is Image wpfImage)
            {
                // Barcode
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
                        Height = wpfImage.Height,
                        Locked = lb.Locked
                    };
                }
                // QR Code
                if (wpfImage.DataContext is LabelQrCode lq)
                {
                    return new LabelQrCode
                    {
                        ID = lq.ID,
                        Layer = layer,
                        Data = lq.Data,
                        LocationX = Canvas.GetLeft(wpfImage),
                        LocationY = Canvas.GetTop(wpfImage),
                        Width = wpfImage.Width,
                        Height = wpfImage.Height,
                        Locked = lq.Locked
                    };
                }
                // LabelImage
                if (wpfImage.DataContext is LabelImage li)
                {
                    return new LabelImage
                    {
                        ID = li.ID,
                        Layer = layer,
                        Base64Data = li.Base64Data,
                        ImagePath = li.ImagePath,
                        Title = li.Title,
                        Opacity = li.Opacity,
                        LocationX = Canvas.GetLeft(wpfImage),
                        LocationY = Canvas.GetTop(wpfImage),
                        Width = wpfImage.Width,
                        Height = wpfImage.Height,
                        Locked = li.Locked
                    };
                }
            }

            // Nic neodpovídá -> null
            return null;
        }

        /// <summary>
        /// Nastaví pozici (Left, Top, ZIndex) a Tag pro nově vytvořený UIElement.
        /// </summary>
        private void FinalizeFrameworkElementPosition(UIElement element, LabelBase model)
        {
            if (element is FrameworkElement fe)
            {
                Canvas.SetLeft(fe, model.LocationX);
                Canvas.SetTop(fe, model.LocationY);
                Canvas.SetZIndex(fe, model.Layer);
                fe.Tag = model.ID.ToString();
            }
        }

        /// <summary>
        /// Převede <see cref="Brush"/> na řetězcový název barvy
        /// pomocí <see cref="ColorHelper.GetNameFromColor"/>.
        /// </summary>
        private string BrushToColorName(Brush brush)
        {
            if (brush is SolidColorBrush scb)
            {
                return ColorHelper.GetNameFromColor(scb.Color);
            }
            // Fallback: "Black"
            return "Black";
        }

        #endregion
    }
}