using CommunityToolkit.Mvvm.ComponentModel;
using SFCDesigner.Models.Elements;
using System.Windows;
using System.Windows.Controls;

namespace SFCDesigner.ViewModels
{
    /// <summary>
    /// ViewModel, který spravuje rozložení (layout) štítku.
    /// Kombinuje <see cref="LabelLayout"/> a WPF <see cref="FrameworkElement"/>.
    /// </summary>
    public class LayoutViewModel : ObservableObject, IElementViewModel
    {
        #region Fields

        private readonly FrameworkElement _layoutControl;
        private readonly LabelLayout _layoutModel;

        // Uchovává název vybraného layoutu z <see cref="AvailableLayouts"/>.
        private string _selectedLayout;

        // Informace, zda je zvolen „vlastní“ layout, který neodpovídá předdefinovaným.
        private bool _isCustomSelected;

        #endregion

        #region Constructor

        /// <summary>
        /// Vytváří nový ViewModel pro layout (LabelLayout) a jeho WPF prvek (FrameworkElement).
        /// </summary>
        /// <param name="layoutControl">WPF prvek (typicky Border), který se zobrazuje jako pozadí štítku.</param>
        /// <param name="layoutModel">Model s údaji o layoutu (CornerRadius, Width, apod.).</param>
        public LayoutViewModel(FrameworkElement layoutControl, LabelLayout layoutModel)
        {
            _layoutControl = layoutControl ?? new Border();
            _layoutModel = layoutModel;

            UnderlyingElement = _layoutControl;

            // Nastavení výchozího výběru z <see cref="AvailableLayouts"/>
            var firstLayout = AvailableLayouts.FirstOrDefault();
            _selectedLayout = firstLayout.Key ?? "Custom";

            // Aktualizujeme zvolený layout
            UpdateSelection();

            // Kdykoli se změní velikost _layoutControl v UI, upozorníme na Width/Height
            _layoutControl.SizeChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
            };
        }

        #endregion

        #region IElementViewModel Implementation

        /// <summary>
        /// UI prvek (např. Border) zobrazovaný na Canvasu.
        /// </summary>
        public UIElement UnderlyingElement { get; }

        /// <summary>
        /// Název zobrazený v bočním panelu (zde "Label").
        /// </summary>
        public string DisplayName => $"Label";

        /// <summary>
        /// Určuje, zda zobrazit panel s vlastnostmi (zde vždy <see cref="Visibility.Visible"/>).
        /// </summary>
        public Visibility PanelVisibility => Visibility.Visible;

        /// <summary>
        /// Jedinečný identifikátor layoutu (z modelu).
        /// </summary>
        public int ID => _layoutModel.ID;

        #endregion

        #region Position / Size / Lock

        public int Layer
        {
            get => _layoutModel.Layer;
            set
            {
                if (_layoutModel.Layer != value)
                {
                    _layoutModel.Layer = value;
                    Canvas.SetZIndex(_layoutControl, value);
                    OnPropertyChanged();
                }
            }
        }

        public double LocationX
        {
            get => Canvas.GetLeft(_layoutControl);
            set
            {
                if (!double.IsNaN(value) && Canvas.GetLeft(_layoutControl) != value)
                {
                    Canvas.SetLeft(_layoutControl, value);
                    _layoutModel.LocationX = value;
                    OnPropertyChanged();
                }
            }
        }

        public double LocationY
        {
            get => Canvas.GetTop(_layoutControl);
            set
            {
                if (!double.IsNaN(value) && Canvas.GetTop(_layoutControl) != value)
                {
                    Canvas.SetTop(_layoutControl, value);
                    _layoutModel.LocationY = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Width
        {
            get => _layoutControl.Width;
            set
            {
                if (value > 0 && _layoutControl.Width != value)
                {
                    _layoutControl.Width = value;
                    _layoutModel.Width = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Height
        {
            get => _layoutControl.Height;
            set
            {
                if (value > 0 && _layoutControl.Height != value)
                {
                    _layoutControl.Height = value;
                    _layoutModel.Height = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Poloměr zakulacených rohů. Projevuje se pouze, pokud <see cref="_layoutControl"/> je <see cref="Border"/>.
        /// </summary>
        public double CornerRadius
        {
            get => _layoutModel.CornerRadius;
            set
            {
                if (_layoutModel.CornerRadius != value)
                {
                    _layoutModel.CornerRadius = value;
                    if (_layoutControl is Border border)
                    {
                        border.CornerRadius = new CornerRadius(value);
                    }
                    OnPropertyChanged();
                }
            }
        }

        public bool Locked
        {
            get => _layoutModel.Locked;
            set
            {
                if (_layoutModel.Locked != value)
                {
                    _layoutModel.Locked = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Predefined Layouts

        /// <summary>
        /// Seznam předdefinovaných rozměrů (nazvaných layoutů).
        /// Key: název, Value: (width, height).
        /// </summary>
        public static Dictionary<string, (double w, double h)> AvailableLayouts { get; } = new()
        {
            { "SILVER LABEL 110×75", (300, 350) },
            { "SILVER LABEL 80×46",  (400, 450) },
            { "CUSTOM LABEL 150×100",(500, 550) },
            { "PC LABEL 210×59",     (600, 650) },
            { "WIFI LABEL 90×37",    (700, 770) }
        };

        /// <summary>
        /// Název aktuálně vybraného layoutu. Pokud neodpovídá <see cref="AvailableLayouts"/>, je "Custom".
        /// </summary>
        public string SelectedLayout
        {
            get => _selectedLayout;
            set
            {
                if (SetProperty(ref _selectedLayout, value))
                {
                    // Pokud je v dictionary, nastavíme šířku a výšku
                    if (AvailableLayouts.TryGetValue(value, out var dimensions))
                    {
                        Width = dimensions.w;
                        Height = dimensions.h;
                        IsCustomSelected = false;
                    }
                    else
                    {
                        // Jinak je 'Custom' => ponecháme rozměry uživatele
                        IsCustomSelected = true;
                    }
                }
            }
        }

        /// <summary>
        /// Určuje, zda je zvolen vlastní (custom) layout, nebo jeden z předdefinovaných.
        /// </summary>
        public bool IsCustomSelected
        {
            get => _isCustomSelected;
            private set => SetProperty(ref _isCustomSelected, value);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Nastaví počáteční hodnoty rozměrů podle <see cref="SelectedLayout"/>.
        /// Pokud model ještě nemá rozměry, použije se z dictionary.
        /// Pokud layout není v dictionary, považujeme za custom.
        /// </summary>
        private void UpdateSelection()
        {
            if (AvailableLayouts.ContainsKey(SelectedLayout))
            {
                var dimensions = AvailableLayouts[SelectedLayout];

                // Pokud v modelu není nastaveno (šířka/výška <= 0), vezmeme z dictionary
                if (_layoutModel.Width <= 0 || _layoutModel.Height <= 0)
                {
                    Width = dimensions.w;
                    Height = dimensions.h;
                }
                IsCustomSelected = false;
            }
            else
            {
                // Pokud klíč neexistuje, jedná se o custom
                Width = _layoutModel.Width;
                Height = _layoutModel.Height;
                Locked = _layoutModel.Locked;
                IsCustomSelected = true;
            }
        }

        #endregion
    }
}
