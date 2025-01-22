using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LabelDesigner.ViewModels
{
    public class TextElementViewModel : ObservableObject, IElementViewModel
    {
        private readonly TextBlock _textBlock;

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

        private static readonly Dictionary<string, string> HexToColorNameMap = ColorNameToHexMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        public TextElementViewModel(TextBlock textBlock)
        {
            _textBlock = textBlock;
            UnderlyingElement = textBlock;
            SelectedColor = HexToColorName(FontColor); // Inicializace barvy při vytvoření
        }

        public UIElement UnderlyingElement { get; }
        public Visibility PanelVisibility => Visibility.Visible;
        public string DisplayName => $"Text: {_textBlock.Text}";

        public string TextContent
        {
            get => _textBlock.Text;
            set
            {
                if (_textBlock.Text != value)
                {
                    _textBlock.Text = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public double FontSize
        {
            get => _textBlock.FontSize;
            set
            {
                try
                {
                    // Ověření rozsahu hodnot
                    if (value < 1 || value > 500)
                    {
                        MessageBox.Show("Velikost písma musí být mezi 1 a 500.", "Neplatná hodnota", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Nastavení nové hodnoty pouze, pokud se změnila
                    if (_textBlock.FontSize != value)
                    {
                        _textBlock.FontSize = value;
                        OnPropertyChanged();
                    }
                }
                catch (FormatException)
                {
                    // Ošetření neplatného formátu vstupu
                    MessageBox.Show("Zadejte platné číslo pro velikost písma.", "Chybný vstup", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    // Ostatní neočekávané chyby
                    MessageBox.Show($"Došlo k neočekávané chybě: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        public string FontColor
        {
            get
            {
                if (_textBlock.Foreground is SolidColorBrush brush)
                {
                    return brush.Color.ToString();
                }
                return "#000000";
            }
            set
            {
                try
                {
                    var brush = (Brush)new BrushConverter().ConvertFromString(value);
                    _textBlock.Foreground = brush;
                    OnPropertyChanged();
                }
                catch
                {
                    // Ignorovat neplatné hodnoty barvy
                }
            }
        }

        private string _selectedColor;
        public string SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    FontColor = ColorNameToHex(value); // Aktualizace hexadecimální hodnoty
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> AvailableColors { get; } = new ObservableCollection<string>(ColorNameToHexMap.Keys);

        public FontFamily FontFamily
        {
            get => _textBlock.FontFamily;
            set
            {
                if (_textBlock.FontFamily != value)
                {
                    _textBlock.FontFamily = value;
                    OnPropertyChanged();
                }
            }
        }

        private static string ColorNameToHex(string colorName) => ColorNameToHexMap.TryGetValue(colorName, out var hex) ? hex : "#000000";
        private static string HexToColorName(string hex) => HexToColorNameMap.TryGetValue(hex, out var name) ? name : "Černá";
    }
}
