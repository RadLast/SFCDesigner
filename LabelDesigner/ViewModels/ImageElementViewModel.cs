using CommunityToolkit.Mvvm.ComponentModel;
using LabelDesigner.Models.Elements;
using System.Windows;
using System.Windows.Controls;

namespace LabelDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro obrázkový prvek na plátně.
    /// Používá kompozici: Model (LabelImage) + WPF Image.
    /// </summary>
    public class ImageElementViewModel : ObservableObject, IElementViewModel
    {
        private readonly Image _image;
        private readonly LabelImage _imageModel;

        /// <summary>
        /// Konstruktor vyžaduje WPF Image a model LabelImage.
        /// </summary>
        public ImageElementViewModel(Image image, LabelImage imageModel)
        {
            _image = image;
            _imageModel = imageModel;

            UnderlyingElement = image;

            // Kdykoli se fyzicky změní rozměr _image, upozorníme na změnu
            _image.SizeChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
            };
        }

        /// <summary>
        /// UIElement, který reprezentuje obrázek na plátně (WPF Image).
        /// </summary>
        public UIElement UnderlyingElement { get; }

        /// <summary>
        /// Určuje, zda je panel viditelný (u obrázku ho chceme zobrazit).
        /// </summary>
        public Visibility PanelVisibility => Visibility.Visible;

        /// <summary>
        /// Zobrazené jméno v bočním panelu, obsahuje ID i Title.
        /// </summary>
        public string DisplayName => $"[{_imageModel.ID}] Image: {_imageModel.Title}";

        public int ID => _imageModel.ID; // jestli chcete upravit ID, dejte set a ID generujte

        /// <summary>
        /// Číslo vrstvy (ZIndex). Uložíme do modelu i do Canvas.
        /// </summary>
        public int Layer
        {
            get => _imageModel.Layer;
            set
            {
                if (_imageModel.Layer != value)
                {
                    _imageModel.Layer = value;
                    Canvas.SetZIndex(_image, value); // Změna v UI
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Pozice X (Canvas.Left) + uložení do modelu.
        /// </summary>
        public double LocationX
        {
            get => Canvas.GetLeft(_image);
            set
            {
                if (Canvas.GetLeft(_image) != value)
                {
                    Canvas.SetLeft(_image, value);
                    _imageModel.LocationX = value; // do modelu
                    OnPropertyChanged();
                }
            }
        }
        public double LocationY
        {
            get => Canvas.GetTop(_image);
            set
            {
                if (Canvas.GetTop(_image) != value)
                {
                    Canvas.SetTop(_image, value);
                    _imageModel.LocationY = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Šířka obrázku v UI. 
        /// Při nastavení se aktualizuje i v modelu.
        /// </summary>
        public double Width
        {
            get => _image.Width;
            set
            {
                if (_image.Width != value)
                {
                    _image.Width = value;
                    _imageModel.Width = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Výška obrázku v UI.
        /// Při nastavení se aktualizuje i v modelu.
        /// </summary>
        public double Height
        {
            get => _image.Height;
            set
            {
                if (_image.Height != value)
                {
                    _image.Height = value;
                    _imageModel.Height = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Průhlednost obrázku (0-1). 
        /// Synchronizováno s WPF Image i s modelem.
        /// </summary>
        public double Opacity
        {
            get => _image.Opacity;
            set
            {
                if (_image.Opacity != value)
                {
                    _image.Opacity = value;
                    _imageModel.Opacity = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Uživatelský (editovatelný) název obrázku, uložený v modelu.
        /// </summary>
        public string Title
        {
            get => _imageModel.Title;
            set
            {
                if (_imageModel.Title != value)
                {
                    _imageModel.Title = value;
                    OnPropertyChanged();
                    // Aktualizace DisplayName:
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }
    }
}
