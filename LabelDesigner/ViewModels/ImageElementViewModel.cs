using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace LabelDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro obrázkový prvek na plátně.
    /// Poskytuje vlastnosti pro Width, Height, Opacity, a DisplayName.
    /// </summary>
    public class ImageElementViewModel : ObservableObject, IElementViewModel
    {
        #region Fields

        private readonly Image _image;

        #endregion

        #region Constructor

        /// <summary>
        /// Inicializuje nový ImageElementViewModel s daným Image UIElementem.
        /// </summary>
        /// <param name="image">UIElement obrázku na plátně.</param>
        public ImageElementViewModel(Image image)
        {
            _image = image;
            UnderlyingElement = image;
        }

        #endregion

        #region IElementViewModel Properties

        /// <inheritdoc/>
        public UIElement UnderlyingElement { get; }

        /// <inheritdoc/>
        public Visibility PanelVisibility => Visibility.Visible;

        /// <inheritdoc/>
        public string DisplayName => "Image";

        #endregion

        #region Properties

        /// <summary>
        /// Šířka obrázku v UI.
        /// </summary>
        public double Width
        {
            get => _image.Width;
            set
            {
                if (_image.Width != value)
                {
                    _image.Width = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Výška obrázku v UI.
        /// </summary>
        public double Height
        {
            get => _image.Height;
            set
            {
                if (_image.Height != value)
                {
                    _image.Height = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Průhlednost obrázku (0-1).
        /// </summary>
        public double Opacity
        {
            get => _image.Opacity;
            set
            {
                if (_image.Opacity != value)
                {
                    _image.Opacity = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion
    }
}
