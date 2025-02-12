using CommunityToolkit.Mvvm.ComponentModel;
using SFCDesigner.Models.Elements;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SFCDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro QR kód (model <see cref="LabelQrCode"/> + WPF prvek).
    /// </summary>
    public class QrCodeElementViewModel : ObservableObject, IElementViewModel
    {
        #region Fields

        private readonly FrameworkElement _qrControl;
        private readonly LabelQrCode _qrModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Vytváří nový ViewModel pro <see cref="LabelQrCode"/>.
        /// </summary>
        /// <param name="qrControl">UI prvek (např. Image nebo Border) pro zobrazení QR kódu na Canvasu.</param>
        /// <param name="qrModel">Model s daty QR (samotný text, ID, rozměry).</param>
        public QrCodeElementViewModel(FrameworkElement qrControl, LabelQrCode qrModel)
        {
            _qrControl = qrControl;
            _qrModel = qrModel;

            UnderlyingElement = qrControl;

            // Při změně velikosti v UI vyvoláme notifikaci
            _qrControl.SizeChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
            };
        }

        #endregion

        #region IElementViewModel

        public UIElement UnderlyingElement { get; }

        public Visibility PanelVisibility => Visibility.Visible;

        /// <summary>
        /// Zobrazovaný název, obsahuje ID a text z <see cref="LabelQrCode.Data"/>.
        /// </summary>
        public string DisplayName => $"[{_qrModel.ID}] QR: {_qrModel.Data}";

        #endregion

        #region Properties - LabelBase

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
                if (Math.Abs(Canvas.GetLeft(_qrControl) - value) > 0.001)
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
                if (Math.Abs(Canvas.GetTop(_qrControl) - value) > 0.001)
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
                if (Math.Abs(_qrControl.Width - value) > 0.001)
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
                if (Math.Abs(_qrControl.Height - value) > 0.001)
                {
                    _qrControl.Height = value;
                    _qrModel.Height = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Locked
        {
            get => _qrModel.Locked;
            set
            {
                if (_qrModel.Locked != value)
                {
                    _qrModel.Locked = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Properties - QR-specific

        /// <summary>
        /// Data (text) obsažená v QR kódu.
        /// </summary>
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
                }
            }
        }

        #endregion
    }
}