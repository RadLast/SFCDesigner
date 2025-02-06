using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using LabelDesigner.Views;
using Microsoft.Win32;

namespace LabelDesigner.ViewModels
{
    /// <summary>
    /// Hlavní ViewModel aplikace, který zajišťuje funkčnost:
    ///  - Vedení kolekce prvků pro Canvas (CanvasElements)
    ///  - Stromovou strukturu pro TreeView (GroupedElements)
    ///  - Výběr (SelectedElementViewModel) v Canvasu i v TreeView
    ///  - Příkazy pro přidávání prvků (texty, obrázky, čárové kódy), ukládání a načítání layoutu
    ///  - Drag & Drop, Resize, klávesové posuny apod.
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        #region Fields

        // Továrna na tvorbu UI prvků (TextBlock, Image, apod.)
        private readonly ElementFactory _elementFactory = new ElementFactory();

        // Správce výběru (vytváří / vykresluje rámeček výběru, udržuje stav vybraného prvku)
        private readonly SelectionManager _selectionManager = new SelectionManager();

        // Správce ukládání a načítání layoutu do / z XML
        private readonly LayoutManager _layoutManager = new LayoutManager();

        // Zajišťuje drag & drop prvků po Canvasu
        private readonly DragAndDropManager _dragManager;

        private readonly ElementGroup _layoutGroup = new ElementGroup() { Name = "Layout" };

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
        /// Vybraný ViewModel prvku (TextElementViewModel / ImageElementViewModel / ...).
        /// Je-li null, není nic vybráno.
        /// </summary>
        private IElementViewModel? _selectedElementViewModel;

        /// <summary>
        /// Mapa (slovník) pro rychlé dohledání, který ViewModel odpovídá danému UIElementu na Canvasu.
        /// Slouží k synchronizaci výběru Canvas <-> TreeView.
        /// </summary>
        private readonly Dictionary<UIElement, IElementViewModel> _elementMap = new();

        #endregion

        #region Collections

        /// <summary>
        /// Kolekce UI prvků (TextBlock, Image, atd.), které se zobrazují na Canvasu.
        /// </summary>
        public ObservableCollection<UIElement> CanvasElements { get; } = new();

        /// <summary>
        /// Skupiny objektů (Texty, Obrázky, Barcodes, QRs) pro zobrazení v TreeView.
        /// </summary>
        public ObservableCollection<ElementGroup> GroupedElements { get; } = new();

        // Vnitřní skupina pro textové prvky
        private readonly ElementGroup _textsGroup = new() { Name = "Texts" };

        // Vnitřní skupina pro obrázky
        private readonly ElementGroup _imagesGroup = new() { Name = "Images" };

        // Skupina pro čárové kódy
        private readonly ElementGroup _barcodesGroup = new() { Name = "Barcodes" };

        // Skupina pro QR kódy
        private readonly ElementGroup _qrGroup = new() { Name = "QR Codes" };

        #endregion

        #region Properties

        /// <summary>
        /// Seznam dostupných fontů (rodin písem), který se pak používá v ComboBoxu ve View.
        /// </summary>
        public ObservableCollection<FontFamily> AvailableFontFamilies { get; }

        /// <summary>
        /// Aktuálně vybraný prvek ViewModelu (TextElementViewModel nebo ImageElementViewModel, BarcodeElementViewModel apod.).
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
                        }
                        else
                        {
                            // Nastavit v SelectionManageru vybraný element pro vykreslení rámečku
                            _selectionManager.Select(value.UnderlyingElement);
                        }

                        _isProgrammaticallySelecting = false;
                    }
                }
            }
        }

        public IElementViewModel? GetViewModel(UIElement element)
        {
            _elementMap.TryGetValue(element, out var vm);
            return vm;
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
        /// Příkazy pro přidání různých typů čárových kódů.
        /// </summary>
        public IRelayCommand AddBarcodeEanCommand { get; }
        public IRelayCommand AddBarcodeCode39Command { get; }

        /// <summary>
        /// Příkaz pro přidání QR kódu.
        /// </summary>
        public IRelayCommand AddQrCommand { get; }

        /// <summary>
        /// Příkaz pro uložení layoutu (XML).
        /// </summary>
        public IRelayCommand SaveLayoutCommand { get; }

        /// <summary>
        /// Příkaz pro načtení layoutu (z XML).
        /// </summary>
        public IRelayCommand LoadLayoutCommand { get; }

        public IRelayCommand DeleteElementCommand { get; }

        /// <summary>
        /// Příkaz volaný při kliknutí na prázdnou oblast Canvasu (zrušení výběru).
        /// </summary>
        public IRelayCommand<object> DesignCanvasMouseLeftButtonDownCommand { get; }

        /// <summary>
        /// Příkaz volaný při změně výběru v TreeView.
        /// </summary>
        public IRelayCommand<object> TreeViewSelectionChangedCommand { get; }

        /// <summary>
        /// Příkazy pro ovládání okna (min/max/zavřít) a tažení okna.
        /// </summary>
        public IRelayCommand MinimizeCommand { get; }
        public IRelayCommand MaximizeRestoreCommand { get; }
        public IRelayCommand CloseCommand { get; }
        public IRelayCommand<MouseButtonEventArgs> DragMoveCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Konstruktor hlavního ViewModelu.
        /// Inicializuje: Drag & Drop, Resize, Commandy a kolekce fontů.
        /// </summary>
        public MainViewModel()
        {
            // DragAndDropManager dostane SelectionManager
            _dragManager = new DragAndDropManager(_selectionManager, this);

            // Když se změní výběr v SelectionManageru, volá se OnSelectionChanged
            _selectionManager.SelectionChanged += OnSelectionChanged;

            // Iniciace příkazů
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

            // Příkazy pro ovládání okna
            MinimizeCommand = new RelayCommand(MinimizeWindow);
            MaximizeRestoreCommand = new RelayCommand(MaximizeRestoreWindow);
            CloseCommand = new RelayCommand(CloseWindow);
            DragMoveCommand = new RelayCommand<MouseButtonEventArgs>(OnMouseDragMove);

            // Naplnění seznamu dostupných fontů
            AvailableFontFamilies = new ObservableCollection<FontFamily>(Fonts.SystemFontFamilies);

            // Přidání skupin do TreeView
            GroupedElements.Add(_layoutGroup);
            GroupedElements.Add(_textsGroup);
            GroupedElements.Add(_imagesGroup);
            GroupedElements.Add(_barcodesGroup);
            GroupedElements.Add(_qrGroup);

            // Ochrana před chybou v XAML Designeru
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                // Připojení klávesové obsluhy pro hlavní okno (pokud je potřeba)
                Application.Current.MainWindow.KeyDown += OnKeyDown;
            }
        }

        #endregion

        #region Selection Logic

        /// <summary>
        /// Reakce na změnu výběru v TreeView (volá se TreeViewSelectionChangedCommand).
        /// Podle vybraného IElementViewModel nastaví SelectedElementViewModel.
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
        /// Kliknutí na prázdné místo v Canvasu = zrušení výběru.
        /// </summary>
        private void OnDesignCanvasMouseLeftButtonDown(object? parameter)
        {
            ClearSelection();
        }

        /// <summary>
        /// Zruší výběr v SelectionManageru a také SelectedElementViewModel.
        /// </summary>
        public void ClearSelection()
        {
            _selectionManager.ClearSelection();
            SelectedElementViewModel = null;
        }

        /// <summary>
        /// Událost z SelectionManager – při změně vybraného UIElementu na Canvasu.
        /// Najdeme příslušný ViewModel a nastavíme ho do SelectedElementViewModel.
        /// </summary>
        private void OnSelectionChanged(UIElement? selectedElement)
        {
            if (_isProgrammaticallySelecting)
                return;

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
        /// Obsluha stisku kláves (např. posun vybraného prvku šipkami).
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (SelectedElementViewModel?.UnderlyingElement is not UIElement element)
                return;

            const double step = 1; // Posun v pixelech
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

            _selectionManager.UpdateSelectionBorder(Canvas.GetLeft(element), Canvas.GetTop(element));
        }

        #endregion

        #region Adding Elements

        /// <summary>
        /// Přidá nový textový prvek (TextBlock).
        /// </summary>
        private void AddTextElement()
        {
            if (!EnsureLayoutExists())
                return;

            var textModel = new LabelText
            {
                ID = IdGenerator.GetNextId(nameof(LabelText)),
                Text = "Nový text",
                FontSize = 14,
                FontFamily = "Segoe UI",
                FontWeight = FontWeights.Normal
            };

            var textBlock = _elementFactory.CreateTextBlock(textModel);

            // +++ DŮLEŽITÉ +++
            ((FrameworkElement)textBlock).DataContext = textModel;

            // Změříme atd.
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textBlock.Width = textBlock.DesiredSize.Width;
            textBlock.Height = textBlock.DesiredSize.Height;
            Canvas.SetZIndex(textBlock, _nextLayer++);

            AddElementToCanvas(textBlock, 50, 50);

            // Kompozice: ViewModel (textBlock, textModel)
            var textVm = new TextElementViewModel(textBlock, textModel);
            _textsGroup.Items.Add(textVm);
            _elementMap[textBlock] = textVm;

        }


        /// <summary>
        /// Přidá nový obrázkový prvek (Image) ze souboru.
        /// </summary>
        private void AddImageElement()
        {
            if (!EnsureLayoutExists())
                return;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.emf)|*.png;*.jpg;*.jpeg;*.emf|All files (*.*)|*.*"
            };

            // Pokud uživatel nevybere, končíme
            if (openFileDialog.ShowDialog() != true)
                return;

            // Zjistíme, zda se jedná o EMF (Enhanced Metafile)
            bool isEmf = string.Equals(
                Path.GetExtension(openFileDialog.FileName),
                ".emf",
                StringComparison.OrdinalIgnoreCase);

            // Vytvoříme WPF Image (buď z EMF, nebo standardní bitmapy)
            var imageElement = isEmf
                ? LoadEmf(openFileDialog.FileName)
                : CreateStandardImage(openFileDialog.FileName);

            // Pokud není EMF, načteme binární data a uložíme do base64
            string? base64Data = null;
            if (!isEmf)
            {
                var fileBytes = File.ReadAllBytes(openFileDialog.FileName);
                base64Data = Convert.ToBase64String(fileBytes);
            }

            // Získáme název souboru bez cesty a přípony (např. "logo")
            string fileNameNoExt = Path.GetFileNameWithoutExtension(openFileDialog.FileName);

            // Vytvoříme model LabelImage, včetně binárních dat a Title = jméno souboru
            var labelImage = new LabelImage
            {
                ID = IdGenerator.GetNextId(nameof(LabelImage)),
                Base64Data = base64Data,
                LocationX = 100,
                LocationY = 100,
                Width = imageElement.Width,
                Height = imageElement.Height,
                Opacity = 1.0,
                Title = fileNameNoExt // Nastavíme výchozí název
            };

            // Nastavíme DataContext => ukládání i čtení ID a Title
            imageElement.DataContext = labelImage;

            // Přidání na Canvas
            AddElementToCanvas(imageElement, labelImage.LocationX, labelImage.LocationY);

            // Vytvoříme ViewModel s kompozicí (obraz + model)
            var imageVm = new ImageElementViewModel(imageElement, labelImage);

            // Přidáme do skupiny a do mapy
            _imagesGroup.Items.Add(imageVm);
            _elementMap[imageElement] = imageVm;
        }





        /// <summary>
        /// Příkazy pro přidání čárových kódů (EAN, Code39).
        /// </summary>
        private void AddBarcodeEan()
        {
            AddBarcodeElement("EAN", "1234567890128");
        }

        private void AddBarcodeCode39()
        {
            AddBarcodeElement("Code39", "HELLOCODE39");
        }

        private void AddBarcodeElement(string barcodeType, string data)
        {
            if (!EnsureLayoutExists())
                return;

            var labelBarcode = new LabelBarcode
            {
                ID = IdGenerator.GetNextId(nameof(LabelBarcode)), // Generování ID
                Data = data,
                BarcodeType = barcodeType
            };
            var barcodeUi = _elementFactory.CreateBarcode(labelBarcode);

            // Nastavíme DataContext, pokud chcete (není podmínkou, ale občas užitečné)
            ((FrameworkElement)barcodeUi).DataContext = labelBarcode;

            AddElementToCanvas(barcodeUi, 200, 50);

            // Převedeme na FrameworkElement
            var frameworkElement = (FrameworkElement)barcodeUi;

            // Nově constructor: BarcodeElementViewModel(image, model)
            var barcodeVm = new BarcodeElementViewModel((Image)barcodeUi, labelBarcode);

            // Nastavíme počáteční šířku/výšku, aby se zobrazil
            barcodeVm.Width = frameworkElement.Width;
            barcodeVm.Height = frameworkElement.Height;

            _barcodesGroup.Items.Add(barcodeVm);
            _elementMap[barcodeUi] = barcodeVm;
        }






        /// <summary>
        /// Přidá QR kód na Canvas.
        /// </summary>
        private void AddQrElement()
        {
            EnsureLayoutExists();

            // Vytvoříme model s ID a výchozími daty
            var labelQr = new LabelQrCode
            {
                ID = IdGenerator.GetNextId(nameof(LabelQrCode)),
                Data = "HelloWorld"
            };

            // Vytvoříme WPF prvek (např. Image) s QR
            var qrUi = _elementFactory.CreateQrCode(labelQr);

            // Nastavíme DataContext => uloží se správné ID
            ((FrameworkElement)qrUi).DataContext = labelQr;

            // Přidáme na Canvas, např. do pozice (120,120)
            AddElementToCanvas(qrUi, 120, 120);

            // Vytvoříme ViewModel
            var qrVm = new QrCodeElementViewModel((FrameworkElement)qrUi, labelQr);

            // Můžeme nastavit počáteční velikost (Width/Height) 
            // dle UI elementu, pokud to dává smysl:
            qrVm.Width = ((FrameworkElement)qrUi).Width;
            qrVm.Height = ((FrameworkElement)qrUi).Height;

            // Přidáme do skupiny a do mapy
            _qrGroup.Items.Add(qrVm);
            _elementMap[qrUi] = qrVm;
        }

        /// <summary>
        /// Přidá jediný Layout prvek na Canvas s parametry získanými z dialogového okna.
        /// </summary>
        private void AddLayoutElement(double width, double height, string shape)
        {
            var layoutModel = new LabelLayout
            {
                ID = IdGenerator.GetNextId(nameof(LabelLayout)),
                Width = width,
                Height = height,
                Layer = 0
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
                //_dragManager.AttachEvents(fe);

                var layoutVm = new LayoutViewModel(fe, layoutModel);
                _layoutGroup.Items.Add(layoutVm);
                _elementMap[fe] = layoutVm;
            }
        }
        #endregion

        #region Loading Images

        /// <summary>
        /// Načtení EMF (Enhanced Metafile) a vykreslení do Bitmapy.
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

                // Převod do WPF
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
        /// Vytvoří standardní bitmapu (PNG/JPG) jako <see cref="Image"/> z dané cesty.
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

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
            {
                SaveLayout();
            }

            // Vyčistíme Canvas i vnitřní struktury
            CanvasElements.Clear();
            _textsGroup.Items.Clear();
            _imagesGroup.Items.Clear();
            _barcodesGroup.Items.Clear();
            _qrGroup.Items.Clear();
            _elementMap.Clear();

            // Resetujeme ID generátor
            IdGenerator.Initialize(new List<(string, int)>()); // Resetuje seznam ID

            // Reset ZIndex, zrušení výběru
            _nextLayer = 1;
            SelectedElementViewModel = null;
        }


        /// <summary>
        /// Přidá prvek (UIElement) na Canvas v dané pozici (left, top)
        /// a zaregistruje ho do Drag&Drop manageru.
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
            //IdGenerator.Initialize(new List<(string, int)>());
        }

        /// <summary>
        /// Načte rozložení z XML, vymaže stávající prvky a znovu je vytvoří.
        /// </summary>
        private void LoadLayout()
        {
            var labels = _layoutManager.LoadCanvasLabels();
            if (labels.Count == 0)
                return;

            // Seznam typů a ID pro inicializaci ID generátoru
            var existingIds = labels.Select(label => (label.GetType().Name, label.ID));
            IdGenerator.Initialize(existingIds);

            // Vymazání aktuálních dat na plátně
            CanvasElements.Clear();
            _textsGroup.Items.Clear();
            _imagesGroup.Items.Clear();
            _barcodesGroup.Items.Clear();
            _qrGroup.Items.Clear();
            _elementMap.Clear();

            foreach (var label in labels.OrderBy(l => l.Layer))
            {
                var uiElement = _layoutManager.ConvertToUIElement(label, _elementFactory);
                if (uiElement == null)
                    continue;

                CanvasElements.Add(uiElement);
                _dragManager.AttachEvents(uiElement);

                switch (label)
                {
                    // --- Text ---
                    case LabelText textLabel:
                        if (uiElement is TextBlock txt)
                        {
                            var txtVm = new TextElementViewModel(txt, textLabel);

                            _textsGroup.Items.Add(txtVm);
                            _elementMap[txt] = txtVm;
                        }
                        break;

                    // --- Image ---
                    case LabelImage imageLabel:
                        if (uiElement is Image img)
                        {
                            var imgVm = new ImageElementViewModel(img, imageLabel);
                            _imagesGroup.Items.Add(imgVm);
                            _elementMap[img] = imgVm;
                        }
                        break;

                    // --- Barcode ---
                    case LabelBarcode barcodeLabel:
                        if (uiElement is Image barcodeImage)
                        {
                            var barcodeVm = new BarcodeElementViewModel(barcodeImage, barcodeLabel);
                            barcodeVm.Width = barcodeImage.Width;
                            barcodeVm.Height = barcodeImage.Height;

                            _barcodesGroup.Items.Add(barcodeVm);
                            _elementMap[barcodeImage] = barcodeVm;
                        }
                        break;

                    // --- QrCode (ponecháme, jak je) ---
                    case LabelQrCode qrLabel:
                        if (uiElement is FrameworkElement qrElem)
                        {
                            var qrVm = new QrCodeElementViewModel(qrElem, qrLabel);

                            _qrGroup.Items.Add(qrVm);
                            _elementMap[qrElem] = qrVm;
                        }
                        break;
                }
            }
        }

        private bool CanDeleteElement()
        {
            // Povolíme příkaz, jen pokud je něco vybráno
            return SelectedElementViewModel != null;
        }

        private void DeleteSelectedElement()
        {
            if (SelectedElementViewModel == null)
                return;

            // 1) Zjistíme, jaký prvek (UIElement) chceme smazat
            var uiElement = SelectedElementViewModel.UnderlyingElement;

            // 2) Odebereme ho z CanvasElements
            CanvasElements.Remove(uiElement);

            // 3) Odebereme ho i z mapy
            _elementMap.Remove(uiElement);

            // 4) Odebereme ho z příslušné skupiny (Texts, Images, Barcodes, QRs)
            RemoveViewModelFromGroup(SelectedElementViewModel);

            // 5) Zrušíme výběr
            ClearSelection();
        }

        // Pomocná metoda, která najde skupinu, z níž máme VM odstranit
        private void RemoveViewModelFromGroup(IElementViewModel vm)
        {
            if (vm is TextElementViewModel)
                _textsGroup.Items.Remove(vm);
            else if (vm is ImageElementViewModel)
                _imagesGroup.Items.Remove(vm);
            else if (vm is BarcodeElementViewModel)
                _barcodesGroup.Items.Remove(vm);
            else if (vm is QrCodeElementViewModel)
                _qrGroup.Items.Remove(vm);
        }

        /// <summary>
        /// Zajistí, že existuje layout. Pokud neexistuje, zobrazí dialog a vytvoří ho.
        /// </summary>
        /// <returns>True, pokud layout existuje nebo byl úspěšně vytvořen, jinak False.</returns>
        private bool EnsureLayoutExists()
        {
            if (!CanvasElements.OfType<FrameworkElement>().Any(fe => fe.DataContext is LabelLayout))
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

        private void CreateLayout(double width, double height, string shape)
        {
            // Initialize a new layout model
            var layoutModel = new LabelLayout
            {
                ID = IdGenerator.GetNextId(nameof(LabelLayout)),
                Width = width,
                Height = height,
                Layer = 0
            };

            // Convert the model into a UIElement
            var uiElement = _layoutManager.ConvertToUIElement(layoutModel, _elementFactory);
            if (uiElement is FrameworkElement fe)
            {
                // Set initial position, size, and ZIndex
                Canvas.SetLeft(fe, 0);
                Canvas.SetTop(fe, 0);
                Canvas.SetZIndex(fe, 0);

                fe.Width = width;
                fe.Height = height;

                // Add the UI element to the canvas and enable drag-and-drop
                CanvasElements.Add(fe);
                //_dragManager.AttachEvents(fe);

                // Create and register a new LayoutViewModel
                var layoutVm = new LayoutViewModel(fe, layoutModel);
                _layoutGroup.Items.Add(layoutVm);
                _elementMap[fe] = layoutVm;
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

        private void OnMouseDragMove(MouseButtonEventArgs? e)
        {
            if (e == null) return;

            var window = Application.Current?.MainWindow;
            if (window == null) return;

            // Pokud je okno maximalizované a uživatel táhne myší, nejdřív ho zmenšíme
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
