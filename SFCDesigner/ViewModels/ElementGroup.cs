using System.Collections.ObjectModel;

namespace SFCDesigner.ViewModels
{
    /// <summary>
    /// Reprezentuje skupinu objektů v TreeView (např. "Texts", "Images").
    /// Každá skupina má své jméno a kolekci prvků (IElementViewModel).
    /// </summary>
    public class ElementGroup
    {
        #region Properties

        /// <summary>
        /// Název skupiny, zobrazovaný v TreeView (např. "Texts", "Images").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Kolekce prvků patřících do této skupiny.
        /// </summary>
        public ObservableCollection<IElementViewModel> Items { get; } = new ObservableCollection<IElementViewModel>();

        #endregion
    }
}
