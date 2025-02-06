using LabelDesigner.Models.Elements;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace LabelDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro čárový kód (kompozice modelu LabelBarcode a WPF Image).
    /// </summary>
    public class BarcodeElementViewModel : IElementViewModel, INotifyPropertyChanged
    {
        private readonly Image _barcodeImage;
        private readonly LabelBarcode _barcodeModel;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public BarcodeElementViewModel(Image barcodeImage, LabelBarcode barcodeModel)
        {
            _barcodeImage = barcodeImage;
            _barcodeModel = barcodeModel;
            UnderlyingElement = barcodeImage;

            // Po každé změně rozměrů vyvoláme UpdateBarcode()
            _barcodeImage.SizeChanged += (_, _) => UpdateBarcode();
        }

        /// <summary> UIElement (WPF Image) pro vykreslení čárového kódu. </summary>
        public UIElement UnderlyingElement { get; }

        /// <summary> Zobrazuje panel vlastností v UI (Visible). </summary>
        public Visibility PanelVisibility => Visibility.Visible;

        /// <summary> Popis v bočním TreeView: ID, typ kódu a data. </summary>
        public string DisplayName => $"[{_barcodeModel.ID}] Barcode ({_barcodeModel.BarcodeType}): {_barcodeModel.Data}";

        /// <summary> ID čárového kódu (pouze ke čtení). </summary>
        public int ID => _barcodeModel.ID;

        // ---------------------------------------------------------------------
        //   NOVĚ: Vlastnosti z LabelBase (Layer, X, Y, Width, Height)
        // ---------------------------------------------------------------------

        /// <summary> Vrstvení (ZIndex). </summary>
        public int Layer
        {
            get => _barcodeModel.Layer;
            set
            {
                if (_barcodeModel.Layer != value)
                {
                    _barcodeModel.Layer = value;
                    Canvas.SetZIndex(_barcodeImage, value);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary> Pozice X (Canvas.Left). </summary>
        public double LocationX
        {
            get => Canvas.GetLeft(_barcodeImage);
            set
            {
                if (Canvas.GetLeft(_barcodeImage) != value)
                {
                    Canvas.SetLeft(_barcodeImage, value);
                    _barcodeModel.LocationX = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary> Pozice Y (Canvas.Top). </summary>
        public double LocationY
        {
            get => Canvas.GetTop(_barcodeImage);
            set
            {
                if (Canvas.GetTop(_barcodeImage) != value)
                {
                    Canvas.SetTop(_barcodeImage, value);
                    _barcodeModel.LocationY = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary> Šířka (pixely). Nastavení vždy přegeneruje obrázek kódu. </summary>
        public double Width
        {
            get => _barcodeImage.Width;
            set
            {
                if (_barcodeImage.Width != value)
                {
                    _barcodeImage.Width = value;
                    _barcodeModel.Width = value; // Uložíme i do modelu
                    UpdateBarcode();
                    OnPropertyChanged();
                }
            }
        }

        /// <summary> Výška (pixely). Nastavení vždy přegeneruje obrázek kódu. </summary>
        public double Height
        {
            get => _barcodeImage.Height;
            set
            {
                if (_barcodeImage.Height != value)
                {
                    _barcodeImage.Height = value;
                    _barcodeModel.Height = value;
                    UpdateBarcode();
                    OnPropertyChanged();
                }
            }
        }

        // ---------------------------------------------------------------------
        //  Data a samotné generování kódu
        // ---------------------------------------------------------------------

        /// <summary> Data kódu (EAN / Code39 apod.). </summary>
        public string Data
        {
            get => _barcodeModel.Data ?? "";
            set
            {
                _barcodeModel.Data = value;
                UpdateBarcode();
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary> Typ kódu (EAN, Code39). </summary>
        public string BarcodeType
        {
            get => _barcodeModel.BarcodeType ?? "";
            set
            {
                _barcodeModel.BarcodeType = value;
                UpdateBarcode();
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        /// <summary> Přegeneruje bitmapu čárového kódu a zobrazí ji ve WPF Image. </summary>
        private void UpdateBarcode()
        {
            try
            {
                var format = _barcodeModel.BarcodeType switch
                {
                    "Code39" => BarcodeFormat.CODE_39,
                    "EAN" => BarcodeFormat.EAN_13,
                    _ => throw new NotSupportedException("Unsupported barcode type.")
                };

                var writer = new BarcodeWriter<System.Drawing.Bitmap>
                {
                    Format = format,
                    Options = new EncodingOptions
                    {
                        Width = (int)Width,
                        Height = (int)Height,
                        Margin = 0
                    },
                    Renderer = new BitmapRenderer()
                };

                using var bmp = writer.Write(_barcodeModel.Data);
                _barcodeImage.Source = ConvertToBitmapImage(bmp);
            }
            catch
            {
                // Pokud formát selže, zobrazíme prázdný obrázek.
                _barcodeImage.Source = null;
            }
        }

        /// <summary> Pomocná metoda pro konverzi System.Drawing.Bitmap => WPF BitmapImage. </summary>
        private BitmapImage ConvertToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }
}
