using CommunityToolkit.Mvvm.ComponentModel;
using LabelDesigner.Models.Elements;
using System.Windows;
using System.Windows.Controls;

namespace LabelDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro QR kód (model LabelQrCode + WPF prvek).
    /// </summary>
    public class QrCodeElementViewModel : ObservableObject, IElementViewModel
    {
        private readonly FrameworkElement _qrControl;
        private readonly LabelQrCode _qrModel;

        public QrCodeElementViewModel(FrameworkElement qrControl, LabelQrCode qrModel)
        {
            _qrControl = qrControl;
            _qrModel = qrModel;

            UnderlyingElement = qrControl;

            // Při změně velikosti v UI ohlásíme změnu (kvůli Bindingům)
            _qrControl.SizeChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
            };
        }

        public UIElement UnderlyingElement { get; }
        public Visibility PanelVisibility => Visibility.Visible;
        public string DisplayName => $"[{_qrModel.ID}] QR: {_qrModel.Data}";

        // ---------------------------------------------------------------------
        //  NOVÉ: Vlastnosti z LabelBase (ID, Layer, X, Y, Width, Height)
        // ---------------------------------------------------------------------

        public int ID => _qrModel.ID;

        public int Layer
        {
            get => _qrModel.Layer;
            set
            {
                if (_qrModel.Layer != value)
                {
                    _qrModel.Layer = value;
                    Canvas.SetZIndex(_qrControl, value);
                    OnPropertyChanged();
                }
            }
        }

        public double LocationX
        {
            get => Canvas.GetLeft(_qrControl);
            set
            {
                if (Canvas.GetLeft(_qrControl) != value)
                {
                    Canvas.SetLeft(_qrControl, value);
                    _qrModel.LocationX = value;
                    OnPropertyChanged();
                }
            }
        }

        public double LocationY
        {
            get => Canvas.GetTop(_qrControl);
            set
            {
                if (Canvas.GetTop(_qrControl) != value)
                {
                    Canvas.SetTop(_qrControl, value);
                    _qrModel.LocationY = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Width
        {
            get => _qrControl.Width;
            set
            {
                if (_qrControl.Width != value)
                {
                    _qrControl.Width = value;
                    _qrModel.Width = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Height
        {
            get => _qrControl.Height;
            set
            {
                if (_qrControl.Height != value)
                {
                    _qrControl.Height = value;
                    _qrModel.Height = value;
                    OnPropertyChanged();
                }
            }
        }

        // ---------------------------------------------------------------------
        //  Původní vlastnost: Data (QR text)
        // ---------------------------------------------------------------------

        private string _dataCache = "";
        public string Data
        {
            get => _qrModel.Data;
            set
            {
                if (_qrModel.Data != value)
                {
                    _qrModel.Data = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                    // Případně volat update QR -> "Redraw"
                }
            }
        }
    }
}
