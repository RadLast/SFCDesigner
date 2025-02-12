using CommunityToolkit.Mvvm.ComponentModel;
using SFCDesigner.Models.Elements;
using System.Windows;
using System.Windows.Controls;

namespace SFCDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro obrázkový prvek na plátně (Model <see cref="LabelImage"/> + WPF <see cref="Image"/>).
    /// </summary>
    public class ImageElementViewModel : ObservableObject, IElementViewModel
    {
        #region Fields

        private readonly Image _image;
        private readonly LabelImage _imageModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Vytváří nový ViewModel pro <see cref="LabelImage"/> s WPF <see cref="Image"/>.
        /// </summary>
        /// <param name="image">WPF prvek typu Image (reálně vykreslený na plátně).</param>
        /// <param name="imageModel">Datový model obrázku (obsah, rozměry, atd.).</param>
        public ImageElementViewModel(Image image, LabelImage imageModel)
        {
            _image = image;
            _imageModel = imageModel;
            UnderlyingElement = image;

            // Pokaždé, když se v UI změní rozměr (např. uživatel roztáhne),
            // upozorníme na to, abychom mohli zareagovat ve viewmodelu (Width/Height).
            _image.SizeChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
            };
        }

        #endregion

        #region IElementViewModel

        /// <summary>
        /// UIElement (WPF Image) pro plátno.
        /// </summary>
        public UIElement UnderlyingElement { get; }

        /// <summary>
        /// Určuje, zda zobrazit panel vlastností. 
        /// U obrázku obvykle <see cref="Visibility.Visible"/>.
        /// </summary>
        public Visibility PanelVisibility => Visibility.Visible;

        /// <summary>
        /// Název prvku pro boční panel: obsahuje ID i název (<see cref="Title"/>).
        /// </summary>
        public string DisplayName => $"[{_imageModel.ID}] Image: {_imageModel.Title}";

        /// <summary>
        /// Jedinečný identifikátor prvku (z modelu).
        /// </summary>
        public int ID => _imageModel.ID;

        #endregion

        #region Properties - Position, Size, Lock

        /// <summary>
        /// Vrstvení (ZIndex) na plátně.
        /// </summary>
        public int Layer
        {
            get => _imageModel.Layer;
            set
            {
                if (_imageModel.Layer != value)
                {
                    _imageModel.Layer = value;
                    Canvas.SetZIndex(_image, value);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Vodorovná pozice (Canvas.Left).
        /// </summary>
        public double LocationX
        {
            get => Canvas.GetLeft(_image);
            set
            {
                if (Canvas.GetLeft(_image) != value)
                {
                    Canvas.SetLeft(_image, value);
                    _imageModel.LocationX = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Svislá pozice (Canvas.Top).
        /// </summary>
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
        /// Šířka obrázku v pixelech, ukládaná do modelu.
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
        /// Výška obrázku v pixelech, ukládaná do modelu.
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
        /// Určuje, zda je prvek uzamčen (nelze jej posouvat ani měnit velikost).
        /// </summary>
        public bool Locked
        {
            get => _imageModel.Locked;
            set
            {
                if (_imageModel.Locked != value)
                {
                    _imageModel.Locked = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Properties - Image-Specific

        /// <summary>
        /// Průhlednost obrázku (0.0 až 1.0), synchronizovaná s WPF Image i s datovým modelem.
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
        /// Uživatelský (editovatelný) název obrázku (např. "Logo firmy"), ukládaný do modelu.
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
                    // Změna se projeví i v DisplayName
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        #endregion
    }
}