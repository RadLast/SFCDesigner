using System.Windows;
using System.Windows.Controls;

namespace SFCDesigner.Helpers
{
    /// <summary>
    /// Attached Property pro synchronizaci SelectedItem v TreeView s ViewModelem.
    /// Umožňuje zpracovat dvoucestný výběr mezi TreeView a SelectedElementViewModel.
    /// </summary>
    public static class TreeViewHelper
    {
        #region Dependency Properties

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached("SelectedItem", typeof(object), typeof(TreeViewHelper), new PropertyMetadata(null, OnSelectedItemChanged));

        #endregion

        #region Get/Set Methods

        public static object GetSelectedItem(DependencyObject obj)
        {
            return (object)obj.GetValue(SelectedItemProperty);
        }

        public static void SetSelectedItem(DependencyObject obj, object value)
        {
            obj.SetValue(SelectedItemProperty, value);
        }

        #endregion

        #region Private Methods

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tree = d as TreeView;
            if (tree == null) return;

            var vm = e.NewValue;
            if (vm == null)
            {
                // Žádný prvek není vybraný, zrušíme výběr
                ClearSelection(tree);
                return;
            }

            // Najdeme odpovídající TreeViewItem
            var item = FindTreeViewItem(tree, vm);
            if (item != null)
            {
                item.IsSelected = true;
                item.BringIntoView(); // volitelné
            }
        }

        private static TreeViewItem? FindTreeViewItem(ItemsControl container, object item)
        {
            if (container == null) return null;

            foreach (var i in container.Items)
            {
                if (i == item)
                {
                    var tvItem = container.ItemContainerGenerator.ContainerFromItem(i) as TreeViewItem;
                    return tvItem;
                }

                var subContainer = container.ItemContainerGenerator.ContainerFromItem(i) as ItemsControl;
                var result = FindTreeViewItem(subContainer, item);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static void ClearSelection(TreeView tree)
        {
            ClearSelectionRecursive(tree);
        }

        private static void ClearSelectionRecursive(ItemsControl container)
        {
            if (container == null) return;

            foreach (var i in container.Items)
            {
                var tvItem = container.ItemContainerGenerator.ContainerFromItem(i) as TreeViewItem;
                if (tvItem != null && tvItem.IsSelected)
                {
                    tvItem.IsSelected = false;
                }

                if (tvItem != null && tvItem.Items.Count > 0)
                {
                    ClearSelectionRecursive(tvItem);
                }
            }
        }

        #endregion
    }
}