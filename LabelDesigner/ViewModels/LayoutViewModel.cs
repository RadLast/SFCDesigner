using CommunityToolkit.Mvvm.ComponentModel;
using LabelDesigner.Models.Elements;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LabelDesigner.ViewModels
{
    public class LayoutViewModel : ObservableObject, IElementViewModel
    {
        private readonly FrameworkElement _layoutControl;
        private readonly LabelLayout _layoutModel;

        private string _selectedLayout;
        private bool _isCustomSelected;

        public LayoutViewModel(FrameworkElement layoutControl, LabelLayout layoutModel)
        {
            _layoutControl = layoutControl ?? new Border();
            _layoutModel = layoutModel;

            UnderlyingElement = _layoutControl;

            // Nastav výchozí výběr
            var firstLayout = AvailableLayouts.FirstOrDefault();
            _selectedLayout = firstLayout.Key ?? "Custom";

            UpdateSelection();

            _layoutControl.SizeChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Height));
            };
        }

        public UIElement UnderlyingElement { get; }

        public string DisplayName => $"Label";

        public Visibility PanelVisibility => Visibility.Visible;

        public int ID => _layoutModel.ID;

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

        public static Dictionary<string, (double w, double h)> AvailableLayouts { get; } = new()
        {
            { "SILVER LABEL 110×75", (300, 350) },
            { "SILVER LABEL 80×46", (400, 450) },
            { "CUSTOM LABEL 150×100", (500, 550) },
            { "PC LABEL 210×59", (600, 650) },
            { "WIFI LABEL 90×37", (700, 770) }
        };

        public string SelectedLayout
        {
            get => _selectedLayout;
            set
            {
                if (SetProperty(ref _selectedLayout, value))
                {
                    // Aktualizuj šířku a výšku podle výběru uživatele
                    if (AvailableLayouts.TryGetValue(value, out var dimensions))
                    {
                        Width = dimensions.w;
                        Height = dimensions.h;
                        IsCustomSelected = false;
                    }
                    else
                    {
                        IsCustomSelected = true;
                    }
                }
            }
        }

        public bool IsCustomSelected
        {
            get => _isCustomSelected;
            private set => SetProperty(ref _isCustomSelected, value);
        }

        private void UpdateSelection()
        {
            Debug.WriteLine($"UpdateSelection: Before Width={_layoutModel.Width}, Height={_layoutModel.Height}");

            if (AvailableLayouts.ContainsKey(SelectedLayout))
            {
                var dimensions = AvailableLayouts[SelectedLayout];

                // Přepisuj hodnoty pouze pokud jsou výchozí nebo prázdné
                if (_layoutModel.Width <= 0 || _layoutModel.Height <= 0)
                {
                    Width = dimensions.w;
                    Height = dimensions.h;
                }
                IsCustomSelected = false;
            }
            else
            {
                Width = _layoutModel.Width;
                Height = _layoutModel.Height;
                IsCustomSelected = true;
            }

            Debug.WriteLine($"UpdateSelection: After Width={Width}, Height={Height}");
        }
    }
}
