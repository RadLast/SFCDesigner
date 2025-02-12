using SFCDesigner.Models.Elements;
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

namespace SFCDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro čárový kód (Model <see cref="LabelBarcode"/> + WPF <see cref="Image"/>).
    /// </summary>
    public class BarcodeElementViewModel : IElementViewModel, INotifyPropertyChanged
    {
        #region Fields

        private readonly Image _barcodeImage;
        private readonly LabelBarcode _barcodeModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Vytváří nový ViewModel pro <see cref="LabelBarcode"/> a příslušný WPF <see cref="Image"/>.
        /// </summary>
        /// <param name="barcodeImage">WPF prvek typu Image, který zobrazuje čárový kód.</param>
        /// <param name="barcodeModel">Model s daty o čárovém kódu (Data, BarcodeType, rozměry atp.).</param>
        public BarcodeElementViewModel(Image barcodeImage, LabelBarcode barcodeModel)
        {
            _barcodeImage = barcodeImage;
            _barcodeModel = barcodeModel;

            UnderlyingElement = barcodeImage;

            // Kdykoli se změní skutečná velikost v UI, přegenerujeme kód
            _barcodeImage.SizeChanged += (_, _) => UpdateBarcode();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion

        #region IElementViewModel

        /// <summary>
        /// UI prvek (WPF Image), který se vykresluje na Canvasu.
        /// </summary>
        public UIElement UnderlyingElement { get; }

        /// <summary>
        /// Viditelnost panelu s vlastnostmi. Pro čárový kód je obvykle <see cref="Visibility.Visible"/>.
        /// </summary>
        public Visibility PanelVisibility => Visibility.Visible;

        /// <summary>
        /// Popis zobrazený v seznamu prvků. Obsahuje ID, typ a Data.
        /// </summary>
        public string DisplayName => $"[{_barcodeModel.ID}] Barcode ({_barcodeModel.BarcodeType}): {_barcodeModel.Data}";

        /// <summary>
        /// Jedinečný ID prvku (z modelu).
        /// </summary>
        public int ID => _barcodeModel.ID;

        #endregion

        #region Properties - Position, Size, Lock

        /// <summary>
        /// Vrstvení (Canvas.ZIndex). Vyšší hodnota = „navrchu“.
        /// </summary>
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

        /// <summary>
        /// Vodorovná souřadnice (Canvas.Left).
        /// </summary>
        public double LocationX
        {
            get => Canvas.GetLeft(_barcodeImage);
            set
            {
                if (Math.Abs(Canvas.GetLeft(_barcodeImage) - value) > 0.001)
                {
                    Canvas.SetLeft(_barcodeImage, value);
                    _barcodeModel.LocationX = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Svislá souřadnice (Canvas.Top).
        /// </summary>
        public double LocationY
        {
            get => Canvas.GetTop(_barcodeImage);
            set
            {
                if (Math.Abs(Canvas.GetTop(_barcodeImage) - value) > 0.001)
                {
                    Canvas.SetTop(_barcodeImage, value);
                    _barcodeModel.LocationY = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Šířka čárového kódu (v pixelech).
        /// Změna vyvolá znovuvygenerování kódu.
        /// </summary>
        public double Width
        {
            get => _barcodeImage.Width;
            set
            {
                if (Math.Abs(_barcodeImage.Width - value) > 0.001)
                {
                    _barcodeImage.Width = value;
                    _barcodeModel.Width = value;
                    UpdateBarcode();
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Výška čárového kódu (v pixelech).
        /// Změna vyvolá znovuvygenerování kódu.
        /// </summary>
        public double Height
        {
            get => _barcodeImage.Height;
            set
            {
                if (Math.Abs(_barcodeImage.Height - value) > 0.001)
                {
                    _barcodeImage.Height = value;
                    _barcodeModel.Height = value;
                    UpdateBarcode();
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Označuje, zda je prvek uzamčen (nelze s ním hýbat ani měnit velikost).
        /// </summary>
        public bool Locked
        {
            get => _barcodeModel.Locked;
            set
            {
                if (_barcodeModel.Locked != value)
                {
                    _barcodeModel.Locked = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Properties - Data & Type

        /// <summary>
        /// Samotná data čárového kódu (např. "1234567890128").
        /// Při změně znovu generujeme kód.
        /// </summary>
        public string Data
        {
            get => _barcodeModel.Data ?? string.Empty;
            set
            {
                if (_barcodeModel.Data != value)
                {
                    _barcodeModel.Data = value;
                    UpdateBarcode();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// Typ kódu (např. "EAN", "Code39"). 
        /// Při změně znovu generujeme kód.
        /// </summary>
        public string BarcodeType
        {
            get => _barcodeModel.BarcodeType ?? string.Empty;
            set
            {
                if (_barcodeModel.BarcodeType != value)
                {
                    _barcodeModel.BarcodeType = value;
                    UpdateBarcode();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Přegeneruje bitmapu čárového kódu a zobrazí ji v <see cref="_barcodeImage"/>.
        /// </summary>
        private void UpdateBarcode()
        {
            try
            {
                // Rozhodneme formát podle BarcodeType
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
                // Pokud generování selže (např. nepodporovaný typ), 
                // vynulujeme zobrazený obrázek.
                _barcodeImage.Source = null;
            }
        }

        /// <summary>
        /// Konvertuje <see cref="System.Drawing.Bitmap"/> na WPF <see cref="BitmapImage"/>.
        /// </summary>
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

        #endregion
    }
}