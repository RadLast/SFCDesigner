using CommunityToolkit.Mvvm.ComponentModel;
using LabelDesigner.Models.Elements;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LabelDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro textový prvek. Používá kompozici:
    ///   - Model: LabelText (drží ID, FontSize, ...)
    ///   - UIElement: TextBlock (pro reálné vykreslení textu)
    /// </summary>
    public class TextElementViewModel : ObservableObject, IElementViewModel
    {
        private readonly TextBlock _textBlock;
        private readonly LabelText _textModel;

        // Mapování názvů barev na hexadecimální hodnoty
        private static readonly Dictionary<string, string> ColorNameToHexMap = new()
        {
            { "Červená", "#FF0000" },
            { "Zelená", "#00FF00" },
            { "Modrá", "#0000FF" },
            { "Žlutá", "#FFFF00" },
            { "Oranžová", "#FFA500" },
            { "Fialová", "#800080" },
            { "Černá", "#000000" },
            { "Bílá", "#FFFFFF" }
        };

        // Obrácená mapa pro určení názvu barvy podle hex
        private static readonly Dictionary<string, string> HexToColorNameMap
            = ColorNameToHexMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        /// <summary>
        /// Konstruktor, vyžaduje WPF TextBlock a model LabelText.
        /// </summary>
        public TextElementViewModel(TextBlock textBlock, LabelText textModel)
        {
            _textBlock = textBlock;
            _textModel = textModel;

            UnderlyingElement = textBlock;

            // Nastavíme počáteční barvu do "SelectedColor" dle modelu
            //   (aby ComboBoxu/Dropdownu odpovídala správná volba).
            SelectedColor = HexToColorName(_textModel.FontColor);
        }

        /// <summary>
        /// UI prvek, který reprezentuje tento text (TextBlock).
        /// </summary>
        public UIElement UnderlyingElement { get; }

        /// <summary>
        /// Zobrazuje panel s vlastnostmi. Pro text vždy Visible.
        /// </summary>
        public Visibility PanelVisibility => Visibility.Visible;

        /// <summary>
        /// Popisek zobrazovaný v seznamu prvků (např. v bočním panelu).
        /// </summary>
        public string DisplayName => $"[{_textModel.ID}] Text: {_textBlock.Text}";

        /// <summary>
        /// ID textu (čteme z modelu).
        /// </summary>
        public int ID => _textModel.ID;

        public int Layer
        {
            get => _textModel.Layer;
            set
            {
                if (_textModel.Layer != value)
                {
                    _textModel.Layer = value;
                    // Nastavíme do Canvas:
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
                if (Canvas.GetLeft(_textBlock) != value)
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
                if (Canvas.GetTop(_textBlock) != value)
                {
                    Canvas.SetTop(_textBlock, value);
                    _textModel.LocationY = value;
                    OnPropertyChanged();
                }
            }
        }

        // Pokud nechcete nastavovat Width/Height textu ručně, můžete vynechat.
        // Pokud ano:
        public double Width
        {
            get => _textBlock.Width;
            set
            {
                if (_textBlock.Width != value)
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
                if (_textBlock.Height != value)
                {
                    _textBlock.Height = value;
                    _textModel.Height = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Textový obsah (synchronizován mezi TextBlock a modelem).
        /// </summary>
        public string TextContent
        {
            get => _textBlock.Text;
            set
            {
                if (_textBlock.Text != value)
                {
                    _textBlock.Text = value;
                    _textModel.Text = value; // aktualizujeme i model
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// Velikost písma v bodech (1 až 500).
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
                        _textModel.FontSize = value; // do modelu
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

        /// <summary>
        /// Celá hex hodnota (např. "#FF0000"). Ukládáme i do modelu.
        /// </summary>
        public string FontColor
        {
            get => _textModel.FontColor;
            set
            {
                if (_textModel.FontColor != value)
                {
                    _textModel.FontColor = value;

                    // Nastavíme reálně barvu do WPF
                    try
                    {
                        var brush = (Brush)new BrushConverter().ConvertFromString(value);
                        _textBlock.Foreground = brush;
                    }
                    catch
                    {
                        // Ignorovat neplatné hodnoty
                        _textBlock.Foreground = Brushes.Black;
                        _textModel.FontColor = "#000000";
                    }

                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Pomocná property v UI pro ComboBox výběr barev.
        /// </summary>
        private string _selectedColor;
        public string SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    // Když uživatel změní "název" barvy, přepíšeme FontColor (hex).
                    FontColor = ColorNameToHex(value);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Seznam pojmenovaných barev pro ComboBox (Červená, Zelená, Modrá...).
        /// </summary>
        public ObservableCollection<string> AvailableColors { get; }
            = new ObservableCollection<string>(ColorNameToHexMap.Keys);

        /// <summary>
        /// Výběr rodiny písma. Nastavuje se i do modelu.
        /// </summary>
        public FontFamily FontFamily
        {
            get => _textBlock.FontFamily;
            set
            {
                if (_textBlock.FontFamily != value)
                {
                    _textBlock.FontFamily = value;
                    _textModel.FontFamily = value.Source; // uložíme string do modelu
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Přepínání tučného písma. 
        /// (Pokud potřebujete Bold / Normal, ukládáte i do modelu.)
        /// </summary>
        public FontWeight FontWeight
        {
            get => _textBlock.FontWeight;
            set
            {
                if (_textBlock.FontWeight != value)
                {
                    _textBlock.FontWeight = value;
                    _textModel.FontWeight = value; // do modelu
                    OnPropertyChanged();
                }
            }
        }

        // -----------------  Private Helper  -------------------

        /// <summary>
        /// Vrátí hex kód barvy z "názvu" (Červená -> #FF0000).
        /// </summary>
        private static string ColorNameToHex(string colorName)
            => ColorNameToHexMap.TryGetValue(colorName, out var hex)
               ? hex
               : "#000000";

        /// <summary>
        /// Vrátí "název" barvy z hex kódu (#FF0000 -> "Červená").
        /// Pokud neexistuje, vrátí "Černá".
        /// </summary>
        private static string HexToColorName(string hex)
            => HexToColorNameMap.TryGetValue(hex, out var name)
               ? name
               : "Černá";
    }
}
