using CommunityToolkit.Mvvm.ComponentModel;
using SFCDesigner.Helpers;       // kvůli ColorHelper a FontHelper
using SFCDesigner.Models.Elements;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SFCDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro textový prvek (TextBlock) na Canvasu.
    ///   - Model: <see cref="LabelText"/> (obsahuje ID, FontSize, ...).
    ///   - UIElement: <see cref="TextBlock"/> (vykreslení na Canvas).
    /// </summary>
    public class TextElementViewModel : ObservableObject, IElementViewModel
    {
        #region Fields

        private readonly TextBlock _textBlock;
        private readonly LabelText _textModel;

        // Uchovává název (pojmenovanou) barvu vybranou v ComboBoxu (např. "Black").
        private string _selectedColor;

        // Uchovává název stylu písma (např. "Bold Italic") vybraný v ComboBoxu.
        private string _selectedFontStyle;

        #endregion

        #region Constructor

        /// <summary>
        /// Vytváří novou instanci ViewModelu pro textový prvek.
        /// </summary>
        /// <param name="textBlock">UI prvek WPF (TextBlock).</param>
        /// <param name="textModel">Model dat (LabelText), obsahující např. fontSize, locked atp.</param>
        public TextElementViewModel(TextBlock textBlock, LabelText textModel)
        {
            _textBlock = textBlock;
            _textModel = textModel;

            UnderlyingElement = textBlock;

            // 1) Inicializace barvy
            _selectedColor = _textModel.FontColorName;
            var color = ColorHelper.GetColorFromName(_selectedColor);
            _textBlock.Foreground = new SolidColorBrush(color);

            // 2) Inicializace stylu (Normal, Italic, Bold, Bold Italic)
            //    Získáme textový název stylu na základě (FontStyle, FontWeight) v modelu
            _selectedFontStyle = FontHelper.GetNameFromWpfStyle(
                _textModel.FontStyle,
                _textModel.FontWeight
            );
        }

        #endregion

        #region IElementViewModel Implementation

        /// <summary>
        /// UIElement pro Canvas, který tento ViewModel spravuje (TextBlock).
        /// </summary>
        public UIElement UnderlyingElement { get; }

        /// <summary>
        /// Panel (s vlastnostmi) se má zobrazit (pro text vždy Visible).
        /// </summary>
        public Visibility PanelVisibility => Visibility.Visible;

        /// <summary>
        /// Unikátní ID prvku, čtené z modelu.
        /// </summary>
        public int ID => _textModel.ID;

        /// <summary>
        /// Popisek pro seznam prvků: ukazuje ID a text.
        /// </summary>
        public string DisplayName => $"[{_textModel.ID}] Text: {_textBlock.Text}";

        #endregion

        #region Position, Size, Lock

        /// <summary>
        /// Vrstvení (ZIndex) na Canvasu.
        /// </summary>
        public int Layer
        {
            get => _textModel.Layer;
            set
            {
                if (_textModel.Layer != value)
                {
                    _textModel.Layer = value;
                    Canvas.SetZIndex(_textBlock, value);
                    OnPropertyChanged();
                }
            }
        }

        public double LocationX
        {
            get => Canvas.GetLeft(_textBlock);
            set
            {
                double oldX = Canvas.GetLeft(_textBlock);
                if (Math.Abs(oldX - value) > 0.001)
                {
                    Canvas.SetLeft(_textBlock, value);
                    _textModel.LocationX = value;
                    OnPropertyChanged();
                }
            }
        }

        public double LocationY
        {
            get => Canvas.GetTop(_textBlock);
            set
            {
                double oldY = Canvas.GetTop(_textBlock);
                if (Math.Abs(oldY - value) > 0.001)
                {
                    Canvas.SetTop(_textBlock, value);
                    _textModel.LocationY = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Width
        {
            get => _textBlock.Width;
            set
            {
                if (Math.Abs(_textBlock.Width - value) > 0.001)
                {
                    _textBlock.Width = value;
                    _textModel.Width = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Height
        {
            get => _textBlock.Height;
            set
            {
                if (Math.Abs(_textBlock.Height - value) > 0.001)
                {
                    _textBlock.Height = value;
                    _textModel.Height = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Locked
        {
            get => _textModel.Locked;
            set
            {
                if (_textModel.Locked != value)
                {
                    _textModel.Locked = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Text & FontSize

        /// <summary>
        /// Obsah textu (synchronizujeme s TextBlock i modelem).
        /// </summary>
        public string TextContent
        {
            get => _textBlock.Text;
            set
            {
                if (_textBlock.Text != value)
                {
                    _textBlock.Text = value;
                    _textModel.Text = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// Velikost písma (1 - 500). Nastavuje se do TextBlocku i do modelu.
        /// </summary>
        public double FontSize
        {
            get => _textBlock.FontSize;
            set
            {
                try
                {
                    if (value < 1 || value > 500)
                    {
                        MessageBox.Show("Velikost písma musí být mezi 1 a 500.",
                                        "Neplatná hodnota",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        return;
                    }

                    if (Math.Abs(_textBlock.FontSize - value) > 0.001)
                    {
                        _textBlock.FontSize = value;
                        _textModel.FontSize = value;
                        OnPropertyChanged();
                    }
                }
                catch (FormatException)
                {
                    MessageBox.Show("Zadejte platné číslo pro velikost písma.",
                                    "Chybný vstup",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Došlo k neočekávané chybě: {ex.Message}",
                                    "Chyba",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Color Handling

        /// <summary>
        /// Dostupné barvy (např. "Black", "White", "Orange"), načítané z <see cref="ColorHelper"/>.
        /// </summary>
        public ObservableCollection<string> AvailableColors { get; }
            = new ObservableCollection<string>(ColorHelper.GetAllColorNames());

        /// <summary>
        /// Aktuálně vybraná barva (pojmenovaná) v ComboBoxu.
        /// Nastavuje <see cref="_textBlock.Foreground"/> a <see cref="_textModel.FontColorName"/>.
        /// </summary>
        public string SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (SetProperty(ref _selectedColor, value))
                {
                    _textModel.FontColorName = value;
                    var realColor = ColorHelper.GetColorFromName(value);
                    _textBlock.Foreground = new SolidColorBrush(realColor);
                }
            }
        }

        #endregion

        #region Font Family & Styles

        /// <summary>
        /// Rodina písma (např. "Segoe UI"). Ukládáme do <see cref="_textModel"/> i <see cref="_textBlock"/>.
        /// </summary>
        public FontFamily FontFamily
        {
            get => _textBlock.FontFamily;
            set
            {
                if (_textBlock.FontFamily != value)
                {
                    _textBlock.FontFamily = value;
                    _textModel.FontFamily = value.Source;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Dostupné styly písma (Normal, Italic, Bold, Bold Italic), načítané z <see cref="FontHelper"/>.
        /// </summary>
        public ObservableCollection<string> AvailableFontStyles { get; }
            = new ObservableCollection<string>(FontHelper.GetAllStyleNames());

        /// <summary>
        /// Aktuálně vybraný styl (např. "Bold Italic"). 
        /// Při změně nastavíme <see cref="_textBlock"/> i <see cref="_textModel"/>.
        /// </summary>
        public string SelectedFontStyle
        {
            get => _selectedFontStyle;
            set
            {
                if (SetProperty(ref _selectedFontStyle, value))
                {
                    // Zavoláme FontHelper, který vrátí (FontStyle, FontWeight)
                    var (fs, fw) = FontHelper.GetWpfFontStyle(value);

                    _textModel.FontStyle = fs;
                    _textModel.FontWeight = fw;
                    _textBlock.FontStyle = fs;
                    _textBlock.FontWeight = fw;
                }
            }
        }

        /// <summary>
        /// Reálný <see cref="FontWeight"/> (např. Normal / Bold). 
        /// Může se změnit i mimo ComboBox.
        /// </summary>
        public FontWeight FontWeight
        {
            get => _textBlock.FontWeight;
            set
            {
                if (_textBlock.FontWeight != value)
                {
                    _textBlock.FontWeight = value;
                    _textModel.FontWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion
    }
}