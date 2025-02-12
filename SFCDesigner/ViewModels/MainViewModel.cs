using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SFCDesigner.Models;
using SFCDesigner.Models.Elements;
using SFCDesigner.Services;
using SFCDesigner.Views;
using Microsoft.Win32;

namespace SFCDesigner.ViewModels
{
    /// <summary>
    /// Hlavní ViewModel aplikace. Starající se o:
    /// <para>- Kolekci prvků na Canvasu (<see cref="CanvasElements"/>).</para>
    /// <para>- Skupiny prvků pro TreeView (<see cref="GroupedElements"/>).</para>
    /// <para>- Výběr (SelectedElementViewModel) v Canvasu i TreeView.</para>
    /// <para>- Příkazy pro přidávání prvků (texty, obrázky, kódy), ukládání a načítání layoutu atd.</para>
    /// <para>- Drag & Drop, Resize, klávesové posuny apod.</para>
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        #region Fields

        private readonly ElementFactory _elementFactory = new(); // Továrna na UI prvky (TextBlock, Image, apod.)
        private readonly SelectionManager _selectionManager = new(); // Správce výběru (rámeček, stavy)
        private readonly DragAndDropManager _dragManager; // Správce drag&drop
        private readonly LayoutManager _layoutManager;    // Zajišťuje ukládání/načítání do XML

        private readonly ElementGroup _layoutGroup = new() { Name = "Layout" };

        private bool _isProgrammaticallySelecting = false;
        private int _nextLayer = 1; // Určuje další volnou vrstvu (ZIndex)

        private IElementViewModel? _selectedElementViewModel;
        private readonly Dictionary<UIElement, IElementViewModel> _elementMap = new(); // UIElement -> ViewModel

        #endregion

        #region Collections

        /// <summary>
        /// UI prvky vykreslené na Canvasu.
        /// </summary>
        public ObservableCollection<UIElement> CanvasElements { get; } = new();

        /// <summary>
        /// Skupiny prvků (Layout, Texts, Images, Barcodes, QRs) pro TreeView.
        /// </summary>
        public ObservableCollection<ElementGroup> GroupedElements { get; } = new();

        private readonly ElementGroup _textsGroup = new() { Name = "Texts" };
        private readonly ElementGroup _imagesGroup = new() { Name = "Images" };
        private readonly ElementGroup _barcodesGroup = new() { Name = "Barcodes" };
        private readonly ElementGroup _qrGroup = new() { Name = "QR Codes" };

        #endregion

        #region Properties

        /// <summary>
        /// Obsahuje informace o aktuálním dokumentu (název, autor, atd.).
        /// </summary>
        public MetaDataViewModel MetaData { get; }

        /// <summary>
        /// Kolekce dostupných písem pro comboBox atd.
        /// </summary>
        public ObservableCollection<FontFamily> AvailableFontFamilies { get; }

        /// <summary>
        /// Aktuálně vybraný prvek (ViewModel) na Canvasu i v TreeView.
        /// Když se změní, informuje o tom SelectionManager.
        /// </summary>
        public IElementViewModel? SelectedElementViewModel
        {
            get => _selectedElementViewModel;
            set
            {
                if (SetProperty(ref _selectedElementViewModel, value))
                {
                    if (!_isProgrammaticallySelecting)
                    {
                        _isProgrammaticallySelecting = true;

                        if (value == null)
                        {
                            _selectionManager.ClearSelection();
                        }
                        else
                        {
                            _selectionManager.Select(value.UnderlyingElement);
                        }

                        _isProgrammaticallySelecting = false;
                    }
                }
            }
        }

        /// <summary>
        /// Najde ViewModel podle UIElementu (Canvas -> UIElement -> ViewModel).
        /// </summary>
        public IElementViewModel? GetViewModel(UIElement element)
        {
            _elementMap.TryGetValue(element, out var vm);
            return vm;
        }

        #endregion

        #region Commands

        public IRelayCommand NewDocumentCommand { get; }
        public IRelayCommand AddTextCommand { get; }
        public IRelayCommand AddImageCommand { get; }
        public IRelayCommand AddBarcodeEanCommand { get; }
        public IRelayCommand AddBarcodeCode39Command { get; }
        public IRelayCommand AddQrCommand { get; }
        public IRelayCommand SaveLayoutCommand { get; }
        public IRelayCommand LoadLayoutCommand { get; }
        public IRelayCommand DeleteElementCommand { get; }
        public IRelayCommand<object> DesignCanvasMouseLeftButtonDownCommand { get; }
        public IRelayCommand<object> TreeViewSelectionChangedCommand { get; }
        public IRelayCommand MinimizeCommand { get; }
        public IRelayCommand MaximizeRestoreCommand { get; }
        public IRelayCommand CloseCommand { get; }
        public IRelayCommand<MouseButtonEventArgs> DragMoveCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Vytváří nový hlavní ViewModel aplikace.
        /// Inicializuje Commands, MetaData, LayoutManager, drag&drop, atd.
        /// </summary>
        public MainViewModel()
        {
            // Vytvoříme model pro metadata a zabalíme ho do VM
            var metadata = new Metadata
            {
                TemplateName = "New Document" // výchozí
            };
            MetaData = new MetaDataViewModel(metadata);

            // LayoutManager potřebuje MetaDataViewModel
            _layoutManager = new LayoutManager(MetaData);

            // Drag & Drop manager
            _dragManager = new DragAndDropManager(_selectionManager, this);

            // Přihlásíme se na událost změny výběru
            _selectionManager.SelectionChanged += OnSelectionChanged;

            // Nastavení příkazů
            NewDocumentCommand = new RelayCommand(NewDocument);
            AddTextCommand = new RelayCommand(AddTextElement);
            AddImageCommand = new RelayCommand(AddImageElement);
            AddBarcodeEanCommand = new RelayCommand(AddBarcodeEan);
            AddBarcodeCode39Command = new RelayCommand(AddBarcodeCode39);
            AddQrCommand = new RelayCommand(AddQrElement);
            SaveLayoutCommand = new RelayCommand(SaveLayout);
            LoadLayoutCommand = new RelayCommand(LoadLayout);
            DesignCanvasMouseLeftButtonDownCommand = new RelayCommand<object>(OnDesignCanvasMouseLeftButtonDown);
            TreeViewSelectionChangedCommand = new RelayCommand<object>(OnTreeViewSelectionChanged);
            DeleteElementCommand = new RelayCommand(DeleteSelectedElement, CanDeleteElement);

            MinimizeCommand = new RelayCommand(MinimizeWindow);
            MaximizeRestoreCommand = new RelayCommand(MaximizeRestoreWindow);
            CloseCommand = new RelayCommand(CloseWindow);
            DragMoveCommand = new RelayCommand<MouseButtonEventArgs>(OnMouseDragMove);

            // Načteme dostupné fonty v systému
            AvailableFontFamilies = new ObservableCollection<FontFamily>(Fonts.SystemFontFamilies);

            // Vytvoříme skupiny v TreeView
            GroupedElements.Add(_layoutGroup);
            GroupedElements.Add(_textsGroup);
            GroupedElements.Add(_imagesGroup);
            GroupedElements.Add(_barcodesGroup);
            GroupedElements.Add(_qrGroup);

            // Pokud neběžíme v Designeru (VS/XAML preview), připojíme klávesové zpracování
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Application.Current.MainWindow.KeyDown += OnKeyDown;
            }
        }

        #endregion

        #region Selection Logic

        /// <summary>
        /// Reakce na změnu výběru v TreeView.
        /// Pokud <paramref name="parameter"/> je IElementViewModel, nastaví se SelectedElementViewModel.
        /// </summary>
        private void OnTreeViewSelectionChanged(object? parameter)
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
        /// Kliknutí na prázdné místo Canvasu => zruší výběr.
        /// </summary>
        private void OnDesignCanvasMouseLeftButtonDown(object? parameter)
        {
            ClearSelection();
        }

        /// <summary>
        /// Zruší výběr v SelectionManageru i ve ViewModelu.
        /// </summary>
        public void ClearSelection()
        {
            _selectionManager.ClearSelection();
            SelectedElementViewModel = null;
        }

        /// <summary>
        /// Událost ze <see cref="SelectionManager"/> – když se změní vybraný prvek na Canvasu.
        /// </summary>
        private void OnSelectionChanged(UIElement? selectedElement)
        {
            if (_isProgrammaticallySelecting) return;

            _isProgrammaticallySelecting = true;

            if (selectedElement != null && _elementMap.TryGetValue(selectedElement, out var vm))
            {
                SelectedElementViewModel = vm;
            }
            else
            {
                SelectedElementViewModel = null;
            }

            _isProgrammaticallySelecting = false;
        }

        /// <summary>
        /// Při stisku kláves pohybujeme vybraným prvkem (šipkami).
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (SelectedElementViewModel?.UnderlyingElement is not UIElement element)
                return;

            const double step = 1; // Kolik pixelů posouváme
            double left = Canvas.GetLeft(element);
            double top = Canvas.GetTop(element);

            switch (e.Key)
            {
                case Key.Left: Canvas.SetLeft(element, left - step); break;
                case Key.Right: Canvas.SetLeft(element, left + step); break;
                case Key.Up: Canvas.SetTop(element, top - step); break;
                case Key.Down: Canvas.SetTop(element, top + step); break;
            }

            _selectionManager.UpdateSelectionBorder(Canvas.GetLeft(element), Canvas.GetTop(element));
        }

        #endregion

        #region Adding Elements

        /// <summary>
        /// Přidá nový textový prvek (TextBlock) na Canvas.
        /// </summary>
        private void AddTextElement()
        {
            if (!EnsureLayoutExists()) return;

            var textModel = new LabelText
            {
                ID = IdGenerator.GetNextId(nameof(LabelText)),
                Text = "Nový text",
                FontSize = 14,
                FontFamily = "Segoe UI",
                FontWeight = FontWeights.Normal
            };

            var textBlock = _elementFactory.CreateTextBlock(textModel);
            ((FrameworkElement)textBlock).DataContext = textModel;

            // Změříme kvůli default šířce a výšce
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textBlock.Width = textBlock.DesiredSize.Width;
            textBlock.Height = textBlock.DesiredSize.Height;
            Canvas.SetZIndex(textBlock, _nextLayer++);

            AddElementToCanvas(textBlock, 50, 50);

            var textVm = new TextElementViewModel(textBlock, textModel);
            _textsGroup.Items.Add(textVm);
            _elementMap[textBlock] = textVm;
        }

        /// <summary>
        /// Otevře dialog pro výběr obrázku a přidá ho na Canvas.
        /// </summary>
        private void AddImageElement()
        {
            if (!EnsureLayoutExists()) return;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.emf)|*.png;*.jpg;*.jpeg;*.emf|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() != true) return;

            bool isEmf = string.Equals(
                Path.GetExtension(openFileDialog.FileName),
                ".emf",
                StringComparison.OrdinalIgnoreCase);

            var imageElement = isEmf
                ? LoadEmf(openFileDialog.FileName)
                : CreateStandardImage(openFileDialog.FileName);

            // Pokud není EMF, ukládáme Base64
            string? base64Data = null;
            if (!isEmf)
            {
                var fileBytes = File.ReadAllBytes(openFileDialog.FileName);
                base64Data = Convert.ToBase64String(fileBytes);
            }

            var fileNameNoExt = Path.GetFileNameWithoutExtension(openFileDialog.FileName);

            var labelImage = new LabelImage
            {
                ID = IdGenerator.GetNextId(nameof(LabelImage)),
                Base64Data = base64Data,
                LocationX = 100,
                LocationY = 100,
                Width = imageElement.Width,
                Height = imageElement.Height,
                Opacity = 1.0,
                Title = fileNameNoExt
            };

            imageElement.DataContext = labelImage;
            AddElementToCanvas(imageElement, labelImage.LocationX, labelImage.LocationY);

            var imageVm = new ImageElementViewModel(imageElement, labelImage);
            _imagesGroup.Items.Add(imageVm);
            _elementMap[imageElement] = imageVm;
        }

        /// <summary>
        /// Přidá čárový kód typu EAN.
        /// </summary>
        private void AddBarcodeEan()
        {
            AddBarcodeElement("EAN", "1234567890128");
        }

        /// <summary>
        /// Přidá čárový kód typu Code39.
        /// </summary>
        private void AddBarcodeCode39()
        {
            AddBarcodeElement("Code39", "HELLOCODE39");
        }

        /// <summary>
        /// Vytvoří a přidá nový <see cref="LabelBarcode"/> dle zadaného typu a dat.
        /// </summary>
        private void AddBarcodeElement(string barcodeType, string data)
        {
            if (!EnsureLayoutExists()) return;

            var labelBarcode = new LabelBarcode
            {
                ID = IdGenerator.GetNextId(nameof(LabelBarcode)),
                Data = data,
                BarcodeType = barcodeType
            };

            var barcodeUi = _elementFactory.CreateBarcode(labelBarcode);
            ((FrameworkElement)barcodeUi).DataContext = labelBarcode;

            AddElementToCanvas(barcodeUi, 200, 50);

            var frameworkElement = (FrameworkElement)barcodeUi;
            var barcodeVm = new BarcodeElementViewModel((Image)barcodeUi, labelBarcode);
            barcodeVm.Width = frameworkElement.Width;
            barcodeVm.Height = frameworkElement.Height;

            _barcodesGroup.Items.Add(barcodeVm);
            _elementMap[barcodeUi] = barcodeVm;
        }

        /// <summary>
        /// Přidá nový prvek s QR kódem na Canvas.
        /// </summary>
        private void AddQrElement()
        {
            if (!EnsureLayoutExists()) return;

            var labelQr = new LabelQrCode
            {
                ID = IdGenerator.GetNextId(nameof(LabelQrCode)),
                Data = "HelloWorld"
            };

            var qrUi = _elementFactory.CreateQrCode(labelQr);
            ((FrameworkElement)qrUi).DataContext = labelQr;

            AddElementToCanvas(qrUi, 120, 120);

            var qrVm = new QrCodeElementViewModel((FrameworkElement)qrUi, labelQr);
            qrVm.Width = ((FrameworkElement)qrUi).Width;
            qrVm.Height = ((FrameworkElement)qrUi).Height;

            _qrGroup.Items.Add(qrVm);
            _elementMap[qrUi] = qrVm;
        }

        /// <summary>
        /// Přidá Layout prvek (LabelLayout) na Canvas se zadanou šířkou, výškou, atd.
        /// </summary>
        private void AddLayoutElement(double width, double height, string shape)
        {
            var layoutModel = new LabelLayout
            {
                ID = IdGenerator.GetNextId(nameof(LabelLayout)),
                Width = width,
                Height = height,
                Layer = 0,
                Locked = true // obvykle layout je locked
            };

            var uiElement = _layoutManager.ConvertToUIElement(layoutModel, _elementFactory);
            if (uiElement is FrameworkElement fe)
            {
                fe.Width = layoutModel.Width;
                fe.Height = layoutModel.Height;
                Canvas.SetLeft(fe, layoutModel.LocationX);
                Canvas.SetTop(fe, layoutModel.LocationY);
                Canvas.SetZIndex(fe, layoutModel.Layer);

                CanvasElements.Add(fe);

                // Pokud chceme, aby layout nešel posouvat, je locked => nepřipojujeme Drag&Drop
                if (!layoutModel.Locked)
                {
                    _dragManager.AttachEvents(fe);
                }

                var layoutVm = new LayoutViewModel(fe, layoutModel);
                layoutVm.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(LayoutViewModel.Locked))
                    {
                        if (layoutVm.Locked)
                        {
                            _dragManager.DetachEvents(layoutVm.UnderlyingElement);
                        }
                        else
                        {
                            _dragManager.AttachEvents(layoutVm.UnderlyingElement);
                        }
                    }
                };

                _layoutGroup.Items.Add(layoutVm);
                _elementMap[fe] = layoutVm;
            }
        }

        #endregion

        #region Loading Images

        /// <summary>
        /// Načte EMF (Enhanced Metafile) a vykreslí ho do System.Drawing.Bitmap,
        /// následně převede do WPF <see cref="Image"/>.
        /// </summary>
        private Image LoadEmf(string filePath)
        {
            var image = new Image();
            try
            {
                using var metafile = new System.Drawing.Imaging.Metafile(filePath);
                int dpi = 96;
                int width = (int)(metafile.Width * dpi / 96.0);
                int height = (int)(metafile.Height * dpi / 96.0);

                using var bitmap = new System.Drawing.Bitmap(width, height);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.Transparent);
                    graphics.DrawImage(metafile, 0, 0, width, height);
                }

                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // Uvolnit GDI handle
                System.Runtime.InteropServices.Marshal.FreeHGlobal(bitmap.GetHbitmap());

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
        /// Vytvoří standardní <see cref="Image"/> z bitmapového souboru (PNG, JPG...).
        /// </summary>
        private Image CreateStandardImage(string filePath)
        {
            var bitmap = new BitmapImage(new Uri(filePath));
            return new Image
            {
                Source = bitmap,
                Width = bitmap.PixelWidth,
                Height = bitmap.PixelHeight
            };
        }

        #endregion

        #region Canvas & Layout

        /// <summary>
        /// Vytvoří nový dokument = reset Canvasu a případné uložení starého.
        /// </summary>
        private void NewDocument()
        {
            MetaData.CurrentFileName = "New Document";
            MetaData.LastModified = DateTime.Now;

            var result = MessageBox.Show(
                "Chcete uložit změny před vytvořením nového dokumentu?",
                "Nový dokument",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
                SaveLayout();

            // Smažeme stávající prvky
            CanvasElements.Clear();
            _textsGroup.Items.Clear();
            _imagesGroup.Items.Clear();
            _barcodesGroup.Items.Clear();
            _qrGroup.Items.Clear();
            _elementMap.Clear();

            // Reset ID generátoru
            IdGenerator.Initialize(new List<(string, int)>());

            _nextLayer = 1;
            SelectedElementViewModel = null;
        }

        /// <summary>
        /// Přidá UIElement (např. TextBlock, Image) na Canvas
        /// a zaregistruje ho do DragAndDrop manageru.
        /// </summary>
        private void AddElementToCanvas(UIElement element, double left, double top)
        {
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
            CanvasElements.Add(element);
            _dragManager.AttachEvents(element);
        }

        /// <summary>
        /// Uloží aktuální rozložení prvků do XML souboru (LayoutManager).
        /// </summary>
        private void SaveLayout()
        {
            MetaData.LastModified = DateTime.Now;
            _layoutManager.SaveCanvasLabels(CanvasElements);
        }

        /// <summary>
        /// Načte rozložení z XML, vymaže aktuální prvky a vytvoří je znovu.
        /// </summary>
        private void LoadLayout()
        {
            var labels = _layoutManager.LoadCanvasLabels();
            if (labels.Count == 0) return;

            // Aktualizace ID generátoru
            IdGenerator.Initialize(labels.Select(l => (l.GetType().Name, l.ID)));

            // Vymazání stávajícího stavu
            CanvasElements.Clear();
            _textsGroup.Items.Clear();
            _imagesGroup.Items.Clear();
            _barcodesGroup.Items.Clear();
            _qrGroup.Items.Clear();
            _elementMap.Clear();

            // Vytvoříme UIElementy dle typu
            foreach (var label in labels.OrderBy(l => l.Layer))
            {
                var uiElement = _layoutManager.ConvertToUIElement(label, _elementFactory);
                if (uiElement == null) continue;

                CanvasElements.Add(uiElement);
                _dragManager.AttachEvents(uiElement);

                switch (label)
                {
                    case LabelText textLabel when uiElement is TextBlock txt:
                        var txtVm = new TextElementViewModel(txt, textLabel);
                        _textsGroup.Items.Add(txtVm);
                        _elementMap[txt] = txtVm;
                        break;

                    case LabelImage imageLabel when uiElement is Image img:
                        var imgVm = new ImageElementViewModel(img, imageLabel);
                        _imagesGroup.Items.Add(imgVm);
                        _elementMap[img] = imgVm;
                        break;

                    case LabelBarcode barcodeLabel when uiElement is Image barcodeImage:
                        var barcodeVm = new BarcodeElementViewModel(barcodeImage, barcodeLabel)
                        {
                            Width = barcodeImage.Width,
                            Height = barcodeImage.Height
                        };
                        _barcodesGroup.Items.Add(barcodeVm);
                        _elementMap[barcodeImage] = barcodeVm;
                        break;

                    case LabelQrCode qrLabel when uiElement is FrameworkElement qrElem:
                        var qrVm = new QrCodeElementViewModel(qrElem, qrLabel);
                        _qrGroup.Items.Add(qrVm);
                        _elementMap[qrElem] = qrVm;
                        break;
                }
            }
        }

        /// <summary>
        /// Kontrola, zda na Canvasu existuje layout (LabelLayout). Pokud ne, 
        /// zobrazí dialog (<see cref="LayoutSelectorWindow"/>) pro vytvoření a přidá ho.
        /// </summary>
        /// <returns>True, pokud layout existuje nebo byl úspěšně přidán.</returns>
        private bool EnsureLayoutExists()
        {
            bool hasLayout = CanvasElements.OfType<FrameworkElement>()
                .Any(fe => fe.DataContext is LabelLayout);

            if (!hasLayout)
            {
                var dlgVm = new LayoutViewModel(null, new LabelLayout());
                var dlgWindow = new LayoutSelectorWindow
                {
                    DataContext = dlgVm,
                    Owner = Application.Current?.MainWindow
                };

                bool? result = dlgWindow.ShowDialog();
                if (result == true)
                {
                    AddLayoutElement(dlgVm.Width, dlgVm.Height, dlgVm.SelectedLayout);
                    return true;
                }

                return false;
            }

            return true;
        }

        #endregion

        #region Deleting Element

        private bool CanDeleteElement()
        {
            return SelectedElementViewModel != null;
        }

        private void DeleteSelectedElement()
        {
            if (SelectedElementViewModel == null) return;

            var uiElement = SelectedElementViewModel.UnderlyingElement;
            CanvasElements.Remove(uiElement);
            _elementMap.Remove(uiElement);

            RemoveViewModelFromGroup(SelectedElementViewModel);

            ClearSelection();
        }

        /// <summary>
        /// Najde a odstraní prvek z příslušné skupiny (Texts, Images, Barcodes, QRs).
        /// </summary>
        private void RemoveViewModelFromGroup(IElementViewModel vm)
        {
            switch (vm)
            {
                case TextElementViewModel:
                    _textsGroup.Items.Remove(vm);
                    break;
                case ImageElementViewModel:
                    _imagesGroup.Items.Remove(vm);
                    break;
                case BarcodeElementViewModel:
                    _barcodesGroup.Items.Remove(vm);
                    break;
                case QrCodeElementViewModel:
                    _qrGroup.Items.Remove(vm);
                    break;
            }
        }

        #endregion

        #region Window Commands

        /// <summary>
        /// Minimalizuje hlavní okno aplikace.
        /// </summary>
        private void MinimizeWindow()
        {
            var window = Application.Current?.MainWindow;
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        /// <summary>
        /// Přepíná okno mezi Maximalizovaným a Normálním stavem.
        /// </summary>
        private void MaximizeRestoreWindow()
        {
            var window = Application.Current?.MainWindow;
            if (window == null) return;

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

        /// <summary>
        /// Zavře hlavní okno aplikace.
        /// </summary>
        private void CloseWindow()
        {
            Application.Current?.MainWindow?.Close();
        }

        /// <summary>
        /// Umožňuje pohybovat (DragMove) hlavním oknem, pokud je stisknuté levé tlačítko myši na TitleBaru.
        /// Pokud je okno maximalizované, nejprve ho uvede do normálního stavu.
        /// </summary>
        private void OnMouseDragMove(MouseButtonEventArgs? e)
        {
            if (e == null) return;

            var window = Application.Current?.MainWindow;
            if (window == null) return;

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