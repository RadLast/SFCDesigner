using System.Windows;

namespace LabelDesigner.ViewModels
{
    /// <summary>
    /// Rozhraní pro ViewModely jednotlivých prvků (Text, Image).
    /// Umožňuje View a MainViewModelu pracovat s prvky abstraktně.
    /// Splňuje OCP – přidání nového typu prvku vyžaduje jen implementaci tohoto rozhraní.
    /// </summary>
    public interface IElementViewModel
    {
        #region Properties

        /// <summary>
        /// Viditelnost panelu vlastností v UI. Obvykle Visible, pokud je prvek vybrán.
        /// </summary>
        Visibility PanelVisibility { get; }

        /// <summary>
        /// Zobrazované jméno prvku (např. "Text: Hello", "Image").
        /// Zobrazuje se v TreeView (DisplayName).
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Odkaz na skutečný UIElement na plátně (TextBlock, Image...),
        /// abychom mohli zvýraznit vybraný objekt (SelectionManager).
        /// </summary>
        UIElement UnderlyingElement { get; }

        #endregion
    }
}
