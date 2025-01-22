using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelDesigner.Models.Elements;
using LabelDesigner.Services;

namespace LabelDesigner.ViewModels
{
    /// <summary>
    /// Hlavní ViewModel aplikace, který zajišťuje funkčnost:
    ///  - Vedení kolekce prvků pro Canvas (CanvasElements)
    ///  - Stromovou strukturu pro TreeView (GroupedElements)
    ///  - Výběr (SelectedElementViewModel) v Canvasu i v TreeView
    ///  - Příkazy pro přidávání prvků (texty, obrázky), ukládání a načítání layoutu
    ///  - Drag & Drop, Resize, klávesové posuny apod.
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        #region Fields

        // Továrna na tvorbu UI prvků (TextBlock, Image, atd.)
        private readonly ElementFactory _elementFactory = new ElementFactory();

        // Správce výběru (vytváří/vykresluje rámeček výběru, udržuje stav vybraného prvku)
        private readonly SelectionManager _selectionManager = new SelectionManager();

        // Správce ukládání a načítání layoutu do / z XML
        private readonly LayoutManager _layoutManager = new LayoutManager();

        // Zajišťuje drag & drop prvků po Canvasu
        private readonly DragAndDropManager _dragManager;

        // Zajišťuje resize vybraného prvku na Canvasu
        private readonly ResizeManager _resizeManager;

        /// <summary>
        /// Vlajka k předcházení "infinitní smyčky" při synchronizaci
        /// SelectedElementViewModel (TreeView) a vybraného UI prvku na Canvasu.
        /// </summary>
        private bool _isProgrammaticallySelecting = false;

        /// <summary>
        /// Proměnná pro přidělování ZIndex (vrstvy) nově vytvářeným prvkům.
        /// </summary>
        private int _nextLayer = 1;

        /// <summary>
        /// Vybraný ViewModel prvku (TextElementViewModel / ImageElementViewModel).
        /// Je-li null, není nic vybráno.
        /// </summary>
        private IElementViewModel? _selectedElementViewModel;

        /// <summary>
        /// Mapa (slovník) pro rychlé dohledání, který ViewModel odpovídá danému UIElementu na Canvasu.
        /// Slouží k synchronizaci výběru Canvas <-> TreeView.
        /// </summary>
        private readonly Dictionary<UIElement, IElementViewModel> _elementMap = new Dictionary<UIElement, IElementViewModel>();

        #endregion

        #region Collections

        /// <summary>
        /// Kolekce UI prvků (TextBlock, Image atd.), které se zobrazují na Canvasu.
        /// </summary>
        public ObservableCollection<UIElement> CanvasElements { get; } = new ObservableCollection<UIElement>();

        /// <summary>
        /// Skupiny objektů (Texty, Obrázky) pro zobrazení v TreeView.
        /// </summary>
        public ObservableCollection<ElementGroup> GroupedElements { get; } = new ObservableCollection<ElementGroup>();

        // Vnitřní skupina pro textové prvky
        private readonly ElementGroup _textsGroup = new ElementGroup { Name = "Texts" };

        // Vnitřní skupina pro obrázky
        private readonly ElementGroup _imagesGroup = new ElementGroup { Name = "Images" };

        #endregion

        #region Properties

        /// <summary>
        /// Seznam dostupných fontů (rodin písem), který se pak používá v ComboBoxu ve View.
        /// </summary>
        public ObservableCollection<FontFamily> AvailableFontFamilies { get; }

        /// <summary>
        /// Aktuálně vybraný prvek ViewModelu (TextElementViewModel nebo ImageElementViewModel).
        /// Nastavení této vlastnosti synchronizuje výběr v Canvasu i TreeView.
        /// Pokud je null, panel s vlastnostmi (např. barva textu) se skryje.
        /// </summary>
        public IElementViewModel? SelectedElementViewModel
        {
            get => _selectedElementViewModel;
            set
            {
                if (SetProperty(ref _selectedElementViewModel, value))
                {
                    // Zabránit rekurzi v situaci, kdy nastavení SelectedElement
                    // vede k volání OnSelectionChanged -> které opět nastavuje SelectedElement.
                    if (!_isProgrammaticallySelecting)
                    {
                        _isProgrammaticallySelecting = true;

                        if (value == null)
                        {
                            // Pokud je null, rušíme výběr
                            _selectionManager.ClearSelection();
                            _resizeManager.DetachFromElement();
                        }
                        else
                        {
                            // Nastavit v SelectionManageru vybraný element pro vykreslení rámečku
                            _selectionManager.Select(value.UnderlyingElement);
                            // ResizeManager se naváže v OnSelectionChanged
                        }

                        _isProgrammaticallySelecting = false;
                    }
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Příkaz pro vytvoření nového "dokumentu" (vyprázdnění Canvasu).
        /// </summary>
        public IRelayCommand NewDocumentCommand { get; }

        /// <summary>
        /// Příkaz pro přidání textového prvku (TextBlock) na Canvas.
        /// </summary>
        public IRelayCommand AddTextCommand { get; }

        /// <summary>
        /// Příkaz pro přidání obrázkového prvku (Image) na Canvas.
        /// </summary>
        public IRelayCommand AddImageCommand { get; }

        /// <summary>
        /// Příkaz pro uložení layoutu (XML).
        /// </summary>
        public IRelayCommand SaveLayoutCommand { get; }

        /// <summary>
        /// Příkaz pro načtení layoutu (z XML).
        /// </summary>
        public IRelayCommand LoadLayoutCommand { get; }

        /// <summary>
        /// Příkaz volaný při kliknutí na prázdnou oblast Canvasu (zrušení výběru).
        /// </summary>
        public IRelayCommand<object> DesignCanvasMouseLeftButtonDownCommand { get; }

        /// <summary>
        /// Příkaz volaný při změně výběru v TreeView.
        /// </summary>
        public IRelayCommand<object> TreeViewSelectionChangedCommand { get; }

        /// <summary>
        /// Příkazy pro ovládání okna (min/max/zavřít) a tažení okna
        /// se můžou přidat sem, pokud jste je přenesli z code-behind.
        /// (Pro ukázku viz předchozí příklad.)
        /// </summary>
        public IRelayCommand MinimizeCommand { get; }
        public IRelayCommand MaximizeRestoreCommand { get; }
        public IRelayCommand CloseCommand { get; }
        public IRelayCommand<MouseButtonEventArgs> DragMoveCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Konstruktor hlavního ViewModelu.
        /// Zde se inicializují:
        ///  - Drag & Drop, Resize
        ///  - Commandy
        ///  - Kolekce AvailableFontFamilies
        /// </summary>
        public MainViewModel()
        {
            // Vytvořit ResizeManager dříve, než DragAndDropManager
            _resizeManager = new ResizeManager(CanvasElements);

            // Teď DragAndDropManager dostane i `_resizeManager`
            _dragManager = new DragAndDropManager(_selectionManager, _resizeManager);

            // Když se změní výběr v SelectionManageru, volá se OnSelectionChanged
            _selectionManager.SelectionChanged += OnSelectionChanged;

            // Iniciace příkazů (Commands)
            NewDocumentCommand = new RelayCommand(NewDocument);
            AddTextCommand = new RelayCommand(AddTextElement);
            AddImageCommand = new RelayCommand(AddImageElement);
            SaveLayoutCommand = new RelayCommand(SaveLayout);
            LoadLayoutCommand = new RelayCommand(LoadLayout);
            DesignCanvasMouseLeftButtonDownCommand = new RelayCommand<object>(OnDesignCanvasMouseLeftButtonDown);
            TreeViewSelectionChangedCommand = new RelayCommand<object>(OnTreeViewSelectionChanged);

            // Příkazy pro ovládání okna (pouze pokud je používáte v MVVM)
            MinimizeCommand = new RelayCommand(MinimizeWindow);
            MaximizeRestoreCommand = new RelayCommand(MaximizeRestoreWindow);
            CloseCommand = new RelayCommand(CloseWindow);
            DragMoveCommand = new RelayCommand<MouseButtonEventArgs>(OnMouseDragMove);

            // Naplnění seznamu dostupných fontů
            AvailableFontFamilies = new ObservableCollection<FontFamily>(Fonts.SystemFontFamilies);

            // Přidání dvou základních "skupin" do TreeView
            GroupedElements.Add(_textsGroup);
            GroupedElements.Add(_imagesGroup);

            // Ochrana před chybou v XAML Designeru
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                // Odtud se třeba připojuje klávesová obsluha pro hlavní okno,
                // pokud to děláte přímo z VM
                Application.Current.MainWindow.KeyDown += OnKeyDown;
            }
        }

        #endregion

        #region Selection Logic

        /// <summary>
        /// Reakce na změnu výběru v TreeView (EventTrigger volá TreeViewSelectionChangedCommand).
        /// Podle vybraného objektu v TreeView nastaví SelectedElementViewModel.
        /// </summary>
        private void OnTreeViewSelectionChanged(object parameter)
        {
            if (parameter is IElementViewModel selectedVm)
            {
                SelectedElementViewModel = selectedVm;
            }
            else
            {
                SelectedElementViewModel = null;
            }
        }

        /// <summary>
        /// Metoda volaná při kliknutí na prázdné místo v Canvasu.
        /// Zruší výběr (tj. skryje rámeček, schová Properties panel apod.).
        /// </summary>
        private void OnDesignCanvasMouseLeftButtonDown(object? parameter)
        {
            ClearSelection();
        }

        /// <summary>
        /// Zruší výběr v SelectionManageru a v rámci ViewModelu (SelectedElementViewModel = null).
        /// </summary>
        public void ClearSelection()
        {
            _selectionManager.ClearSelection();
            SelectedElementViewModel = null;
            _resizeManager.DetachFromElement();
        }

        /// <summary>
        /// Událost z <see cref="SelectionManager"/> – volá se při změně vybraného UIElementu na Canvasu.
        /// Najdeme příslušný ViewModel v _elementMap a nastavíme ho do SelectedElementViewModel.
        /// </summary>
        private void OnSelectionChanged(UIElement? selectedElement)
        {
            Debug.WriteLine($"Selection changed to: {selectedElement}");

            // Zabránit rekurzi v nastavení
            if (_isProgrammaticallySelecting)
                return;

            _isProgrammaticallySelecting = true;

            if (selectedElement != null && _elementMap.TryGetValue(selectedElement, out var vm))
            {
                // Nastaví se vybraný ViewModel
                SelectedElementViewModel = vm;

                // ResizeManager se připojí k prvku a zobrazí (pokud to dává smysl)
                if (selectedElement is FrameworkElement fe)
                {
                    _resizeManager.AttachToElement(fe);
                }
            }
            else
            {
                // Nenašel se odpovídající ViewModel => zrušit výběr
                SelectedElementViewModel = null;
                _resizeManager.DetachFromElement();
            }

            _isProgrammaticallySelecting = false;
        }

        /// <summary>
        /// Obsluha stisku kláves. Umožňuje např. posun vybraného prvku šipkami.
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Není-li vybrán žádný element, nic se neděje.
            if (SelectedElementViewModel?.UnderlyingElement is not UIElement element)
                return;

            const double step = 5; // Velikost pohybu v pixelech
            double left = Canvas.GetLeft(element);
            double top = Canvas.GetTop(element);

            switch (e.Key)
            {
                case Key.Left:
                    Canvas.SetLeft(element, left - step);
                    break;
                case Key.Right:
                    Canvas.SetLeft(element, left + step);
                    break;
                case Key.Up:
                    Canvas.SetTop(element, top - step);
                    break;
                case Key.Down:
                    Canvas.SetTop(element, top + step);
                    break;
            }

            // Aktualizace polohy rámečku výběru
            _selectionManager.UpdateSelectionBorder(Canvas.GetLeft(element), Canvas.GetTop(element));
        }

        #endregion

        #region Adding Elements

        /// <summary>
        /// Příkaz pro přidání nového textového prvku. 
        /// Vytvoří se LabelText (model), z něj TextBlock (UI prvek) a TextElementViewModel.
        /// Přidá se do Canvas a do GroupedElements (Texts).
        /// </summary>
        private void AddTextElement()
        {
            var textElement = new LabelText
            {
                Text = "Nový text",
                FontSize = 14,
                FontFamily = "Segoe UI",
                FontWeight = FontWeights.Normal
            };

            // Vytvoří TextBlock dle modelu
            var textBlock = _elementFactory.CreateTextBlock(textElement);

            // Důležité: změřit obsah, nastavit Width/Height, aby nebyly NaN
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textBlock.Width = textBlock.DesiredSize.Width;
            textBlock.Height = textBlock.DesiredSize.Height;

            // Nastavit ZIndex, aby se novější prvky vykreslovaly "navrchu"
            Canvas.SetZIndex(textBlock, _nextLayer++);

            // Přidat na Canvas a do Drag&Drop manageru
            AddElementToCanvas(textBlock, left: 50, top: 50);

            // Vytvořit k němu ViewModel a zařadit do "Texts" skupiny
            var textVm = new TextElementViewModel(textBlock);
            _textsGroup.Items.Add(textVm);

            // Zaregistrovat do slovníku, abychom mohli najít ViewModel podle UIElementu
            _elementMap[textBlock] = textVm;
        }

        /// <summary>
        /// Příkaz pro přidání obrázkového prvku (Image). 
        /// Ze souboru (pomocí dialogu) se vytvoří odpovídající UI prvek, 
        /// následně ImageElementViewModel a přidá se do Canvas + do "Images" skupiny.
        /// </summary>
        private void AddImageElement()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.emf)|*.png;*.jpg;*.jpeg;*.emf|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            // Rozlišení mezi standardním obrázkem a EMF
            var isEmf = string.Equals(
                System.IO.Path.GetExtension(openFileDialog.FileName),
                ".emf",
                StringComparison.OrdinalIgnoreCase);

            var imageElement = isEmf
                ? LoadEmf(openFileDialog.FileName)
                : CreateStandardImage(openFileDialog.FileName);

            // imageElement už by měl mít Width/Height nastavené
            AddElementToCanvas(imageElement, left: 100, top: 100);

            var imageVm = new ImageElementViewModel(imageElement);
            _imagesGroup.Items.Add(imageVm);
            _elementMap[imageElement] = imageVm;
        }

        #endregion

        #region Loading Images

        /// <summary>
        /// Načtení EMF (Enhanced Metafile) a vykreslení do Bitmapy,
        /// aby šlo jednoduše zobrazit v WPF jako <see cref="Image"/>.
        /// </summary>
        private Image LoadEmf(string filePath)
        {
            var image = new Image();
            try
            {
                using var metafile = new System.Drawing.Imaging.Metafile(filePath);
                int dpi = 96; // výchozí DPI pro WPF
                int width = (int)(metafile.Width * dpi / 96.0);
                int height = (int)(metafile.Height * dpi / 96.0);

                using var bitmap = new System.Drawing.Bitmap(width, height);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.Transparent);
                    graphics.DrawImage(metafile, 0, 0, width, height);
                }

                // Převedení na BitmapSource (WPF)
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // Uvolnit GDI handle
                System.Runtime.InteropServices.Marshal.FreeHGlobal(bitmap.GetHbitmap());

                // Nastavit výstup do WPF Image
                image.Source = bitmapSource;
                image.Width = width;
                image.Height = height;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Chyba při načítání EMF: {ex.Message}",
                    "Chyba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return image;
        }

        /// <summary>
        /// Vytvoří standardní bitmapu (např. PNG/JPG) jako <see cref="Image"/> z dané cesty (filePath).
        /// </summary>
        private Image CreateStandardImage(string filePath)
        {
            var bitmap = new BitmapImage(new Uri(filePath));
            var img = new Image
            {
                Source = bitmap,
                Width = bitmap.PixelWidth,
                Height = bitmap.PixelHeight
            };
            return img;
        }

        #endregion

        #region Canvas & Layout

        /// <summary>
        /// Vytvoří nový dokument = vymaže Canvas a znovu nastaví výchozí stav.
        /// </summary>
        private void NewDocument()
        {
            var result = MessageBox.Show(
                "Chcete uložit změny před vytvořením nového dokumentu?",
                "Nový dokument",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            // Pokud zvolí 'Cancel', akci přerušíme
            if (result == MessageBoxResult.Cancel)
                return;

            // Při 'Yes' se pokusíme uložit rozložení
            if (result == MessageBoxResult.Yes)
            {
                SaveLayout();
            }

            // Vyčistíme Canvas i vnitřní struktury
            CanvasElements.Clear();
            _textsGroup.Items.Clear();
            _imagesGroup.Items.Clear();
            _elementMap.Clear();

            // Reset ZIndex, zrušení výběru
            _nextLayer = 1;
            SelectedElementViewModel = null;
        }

        /// <summary>
        /// Přidá prvek (UIElement) na Canvas v dané pozici (left, top)
        /// a zaregistruje ho do drag&drop manageru.
        /// </summary>
        private void AddElementToCanvas(UIElement element, double left, double top)
        {
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
            CanvasElements.Add(element);
            _dragManager.AttachEvents(element);
        }

        /// <summary>
        /// Uloží aktuální rozložení prvků na Canvasu do XML (pomocí LayoutManager).
        /// </summary>
        private void SaveLayout()
        {
            _layoutManager.SaveCanvasLabels(CanvasElements);
        }

        /// <summary>
        /// Načte rozložení z XML, vymaže stávající prvky a znovu je vytvoří.
        /// </summary>
        private void LoadLayout()
        {
            var labels = _layoutManager.LoadCanvasLabels();
            if (labels.Count == 0)
                return;

            // Vymazat staré
            CanvasElements.Clear();
            _textsGroup.Items.Clear();
            _imagesGroup.Items.Clear();
            _elementMap.Clear();

            foreach (var label in labels.OrderBy(l => l.Layer))
            {
                // Konverze z modelu (LabelBase) na UIElement
                var uiElement = _layoutManager.ConvertToUIElement(label, _elementFactory);
                if (uiElement == null)
                    continue;

                CanvasElements.Add(uiElement);
                _dragManager.AttachEvents(uiElement);

                // Vytvoření patřičného ViewModelu a vložení do odpovídající skupiny
                if (uiElement is TextBlock txt)
                {
                    var txtVm = new TextElementViewModel(txt);
                    _textsGroup.Items.Add(txtVm);
                    _elementMap[txt] = txtVm;
                }
                else if (uiElement is Image img)
                {
                    var imgVm = new ImageElementViewModel(img);
                    _imagesGroup.Items.Add(imgVm);
                    _elementMap[img] = imgVm;
                }
            }
        }

        #endregion

        #region Window Commands

        private void MinimizeWindow()
        {
            var window = Application.Current?.MainWindow;
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void MaximizeRestoreWindow()
        {
            var window = Application.Current?.MainWindow;
            if (window == null)
                return;

            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
            }
            else
            {
                var workingArea = SystemParameters.WorkArea;
                window.MaxHeight = workingArea.Height;
                window.MaxWidth = workingArea.Width;

                window.WindowState = WindowState.Maximized;
            }
        }

        private void CloseWindow()
        {
            Application.Current?.MainWindow?.Close();
        }

        private void OnMouseDragMove(MouseButtonEventArgs e)
        {
            var window = Application.Current?.MainWindow;
            if (window == null) return;

            // Pokud je okno maximalizované, při stisku a tahu ho nejdřív zmenšíme.
            if (window.WindowState == WindowState.Maximized)
            {
                var mousePosition = window.PointToScreen(e.GetPosition(window));
                window.WindowState = WindowState.Normal;

                window.Left = mousePosition.X - (window.RestoreBounds.Width / 2);
                window.Top = mousePosition.Y - 10;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                window.DragMove();
            }
        }

        #endregion
    }
}
