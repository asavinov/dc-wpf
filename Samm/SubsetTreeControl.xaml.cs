﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Com.Model;

namespace Samm
{
    /// <summary>
    /// Interaction logic for SubsetTreeControl.xaml
    /// 
    /// Ideally, the control should be able to show both typed elements (Set, Dim) and generic nodes (TreeNode<Set>, TreeNode<Dim>)
    /// The control should also have some properties which specify what kind of schema elements have to be displayed: NoPrimitive, OnlyPrimitive, etc. (maybe as a mask)
    /// It should be stored in properties and the property can be used by the (child) collection builder class. 
    /// Thus instead of producing a completely new tree using generic nodes (referencing filtered schema elements), we can simply represent the filter as part of the view. Then this filter object will define what to display and what not.
    /// 
    /// Advantages of displaying generic TreeNode<Set/Dim/...>: 
    /// 1. Possibility to select items we want to display (say, only one set); 
    /// 2. TreeNodes are observable (instead of making observable schema elements - it is conceptually difficult becuase it is not a tree); 
    /// 
    /// Disadvantages of using generic nodes:
    /// 1. Tree has to be updated after each change in the schema (so if we add/remove/update dim/set then the corresponding trees have to be also updated)
    /// 1.1. There can be many trees and hence we still have to implement some notification
    /// 1.2. Changes in a poset need to be converted into tree changes and it can be difficult because we must know the semantics of the tree, say, greater dim and lesser dim trees make different updates. In any case, if we implement notifications from the schema then why not to notify directly the TreeViews using built-in mechanisms?
    /// 
    /// </summary>
    public partial class SubsetTreeControl : UserControl
    {
  /*
        - Multi-selection in tree view: http://stackoverflow.com/questions/459375/customizing-the-treeview-to-allow-multi-select
        - p675. Modify visual reprsentation of an item depending on property including data properties and visual properties (e.g., selected dim/set can show more information)
          - Use a data trigger. Set a property in a template depending on a property in the data object. 
            The object (like Set) has to implement INotifyPropertyChanged if the property can change and it is necessary to update the view.
            http://stackoverflow.com/questions/5010511/wpf-datatemplate-binding-depending-on-the-type-of-a-property
          - Specify value converter as a parameter of binding by implementing IValueConverter or IMultiValueConverter (for converting from multiple properties):
            <Image Source="{Binding Path=ProductImagePath, Converter={StaticResource ImagePathConverter}}"/>
            Background="{Binding Path=CategoryName, Converter={StaticResource CategoryToColorConverter}"
            This approach is good when converters are reused with other templates (say, colors, images etc.)
            http://stackoverflow.com/questions/790896/selecting-datatemplate-based-on-sub-object-type
          - p676. Use a template selector which examines the bound data object and chooses among several templates (works precisely as style selectors). 
            Derive the selector from DataTemplateSelector: http://stackoverflow.com/questions/790896/selecting-datatemplate-based-on-sub-object-type
            Can be used, for instance, to display more information for selected items like Set or Dim. 
          - p.683 Changing visual properties depending on selection property of the whole item (for expanding selected items)
        
        - How to add artificial folder by grouping special items? For example, primitive sets (in the root), identity dimensions, entity dimensions etc.
        - p.691 (ch.21) Data views for sorting, filtering items. Data source has to implement IList (e.g. ObservableCollection), IBindingList or IEnumerable (bad, low performance).
          - Using flags/properties for choosing what to visualize: visualize also lesser (incoming) dimensions, visualize also dimension expansion (expand dimension range set), show only identity dims or only entity dims etc.
          - Custom sorting. One general way is to implement binding properties specially for visualization purposes which will return what is needed for the tree view (and other controls) taking into account flags, filters, properties, dedicated folders for special elements etc.
        - Putting nodes of one kind in a separate folder (say, primitive sets, subsets, identity/entity dimensions etc.)
          - Different types of dimensions (identity/entiy/greater/lesser etc.) either hide or in separate folders 
          Solution: http://www.codeproject.com/Articles/36451/Organizing-Heterogeneous-Data-on-a-WPF-TreeView
          http://stackoverflow.com/questions/2248346/grouping-child-objects-in-wpf-treeview
          Alternative solution: http://www.zagstudio.com/blog/365#.Ufgt3W1Enmg  http://www.zagstudio.com/blog/367#.UfguTm1Enmg
        
            // Criteria to the tree view: 
            // - Conditional visualization (item rendering):
            //   - Primitive concepts either not visualized or visualized in a separate folder (also other kinds of folders either with special class or with special properties)
            //   - Dimension structure visualized (so we need to anayze the identity tree) -> Use multibinding by expanding dimension items (only identities up to the primitive sets)
            //   - Alligning various elements of items across the whole tree like in a table, say, names could be alligned (although it might not be possible for deep children)
            //   - TreeView header (it is probably needed only if we have allignment).
            // - The root (SetRoot) is either shown or hidden so that its direct child sets are listed in tree view at the very first level
            // - Touchable actions: context menu, drag-n-drop, DnD icon changing its appearance depending on the drop area, scrolling etc.
            // - Visualizations: animations during actions (touching, DnD), pitching, external events like process updates or property ^s etc. 
            // - Selection and highlighting: multi-selection (including via touch), conditional selection when not all combinations are possible (with warning animation or other visualization). 
            // - Getting unique representation for a (selected or arbitrary) item visualized by a tree item (dim id, set id etc. including root)
*/

        // TODO: Implement a filter object (propertíes with mask) which determines what schema elements will be displayed. 
        // One option is to use this property from the CompositeCollectionConverter (check if it is possible - for example, how to access the filter from the converter - the converter has to know the model/view it converts for). 
        // Another option is to use a templete selector or elements selector (also not clear if it is possible).

        public SubsetTreeControl()
        {
            InitializeComponent();
        }

        // Drag and drop sources: 
        // http://msdn.microsoft.com/en-us/library/ms742859.aspx
        // http://www.wpftutorial.net/DragAndDrop.html, 
        // http://dotnet-experience.blogspot.de/2011/04/wpf-treeview-drag-n-drop.html
        // http://www.codeproject.com/Articles/55168/Drag-and-Drop-Feature-in-WPF-TreeView-Control
        // Auto-expand node (use DragOver): http://stackoverflow.com/questions/1709581/whilst-using-drag-and-drop-can-i-cause-a-treeview-to-expand-the-node-over-which?rq=1

        private Point dragStartingPoint;

        private void SubsetTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartingPoint = e.GetPosition(null); // Store the mouse position
        }

        private void SubsetTree_MouseMove(object sender, MouseEventArgs e)
        {
            Point dragCurrentPoint = e.GetPosition(null); // Get the current mouse position
            Vector diff = dragStartingPoint - dragCurrentPoint;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                TreeView treeView = sender as TreeView;
                TreeViewItem treeViewItem = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource); // Get the dragged TreeViewItem

                if (treeView == null || treeViewItem == null) return;

                // Find the data behind the TreeViewItem
                object data1 = (object)treeView.ItemContainerGenerator.ItemFromContainer(treeViewItem);
                object data2 = treeViewItem.DataContext; // Alternative
                object data3 = treeView.SelectedItem; // Alternative
                object data = data2;

                // Initialize the drag & drop operation
                string format = null;
                if (data is Set) format = "Set";
                else if (data is Dim) format = "Dim";
                else if (data is SubsetTree)
                {
                    if (((SubsetTree)data).IsSubsetNode) 
                    {
                        data = ((SubsetTree)data).LesserSet;
                        format = "Set";
                    }
                    else if (((SubsetTree)data).IsDimensionNode)
                    {
                        data = ((SubsetTree)data).Dim;
                        format = "Dim";
                    }

                }
                DataObject dragData = new DataObject(format, data);
                DragDrop.DoDragDrop(treeViewItem, dragData, DragDropEffects.Copy);
            }
        }

        private void SubsetTree_DragEnter(object sender, DragEventArgs e)
        {
/*
            if (!e.Data.GetDataPresent(typeof(Set))) // Choose type of data that is about to be dropped
            {
                e.Effects = DragDropEffects.Copy;
            }
*/
        }

        private void SubsetTree_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                Point dragCurrentPoint = e.GetPosition(null);
                Vector diff = dragStartingPoint - dragCurrentPoint;

                if ((Math.Abs(diff.X) > 10.0) || (Math.Abs(diff.Y) > 10.0))
                {
                    // Verify that this is a valid drop and then store the drop target
/*
                    TreeViewItem item = GetNearestContainer(e.OriginalSource as UIElement);
                    if (CheckDropTarget(draggedItem, item))
                    {
                        e.Effects = DragDropEffects.Move;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
*/
                }
                e.Handled = true;
            }
            catch (Exception)
            {
            }
        }

        private void SubsetTree_Drop(object sender, DragEventArgs e)
        {
            // Determine the drop target
            object dropTarget = null;

            var treeViewItem = FindAnchestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (treeViewItem == null) // No item. // Check if it is dropped inside the treeview area (rather than any node)
            {
                var treeView = FindAnchestor<TreeView>((DependencyObject)e.OriginalSource);
                if (treeView == null) return;

                var rootItem = treeView.Items[0];
                var rootItem2 = ((ObservableCollection<SubsetTree>)treeView.DataContext)[0];
                var rootItem3 = ((ObservableCollection<SubsetTree>)treeView.ItemsSource)[0];

                if (rootItem is Set || rootItem is Dim)
                {
                    dropTarget = rootItem;
                }
                else if (rootItem is SubsetTree)
                {
                    if (((SubsetTree)rootItem).IsSubsetNode)
                    {
                        dropTarget = ((SubsetTree)rootItem).LesserSet;
                    }
                    else if (((SubsetTree)rootItem).IsDimensionNode)
                    {
                        dropTarget = ((SubsetTree)rootItem).Dim;
                    }
                }
            }
            else
            {
                dropTarget = treeViewItem.Header;
            }
            if (dropTarget == null) return;
            // Now dropTarget is supposed to be either Set (including SetRoot) or Dim (including primitive dimensions)

            // Determine the source
            if (e.Data.GetDataPresent("Set"))
            {
                var dropSource = e.Data.GetData("Set") as Set;
                if (dropSource == null) return; // No data
                
                // Do action
                if (dropTarget is SetRoot)
                {
                    if (dropTarget == ((MainWindow)App.Current.MainWindow).MashupRoot && ((MainWindow)App.Current.MainWindow).IsInSource(dropSource))
                    {
                        // Some table from a data source is dropped to the mashup: import table
                        ICommand cmd = ((MainWindow)App.Current.MainWindow).Resources["ImportTableCommand"] as ICommand;

                        if (cmd == null) return;

                        if (cmd.CanExecute(null))
                        {
                            cmd.Execute(null);
                        }
                    }

                }
                else if (dropTarget is Set)
                {
                    Set set = dropTarget as Set;
                }
                else if (dropTarget is Dim)
                {
                    Dim dim = treeViewItem.Header as Dim;
                }
            }
            // Determine the source
            else if (e.Data.GetDataPresent("Dim"))
            {
                var dropSource = e.Data.GetData("Dim") as Dim;
                if (dropSource == null) return; // No data

                // Do action
                if (dropTarget is SetRoot)
                {
                    // Cannot drop column to root
                }
                else if (dropTarget is Set)
                {
                    Set set = dropTarget as Set;
                    if (set.Top == ((MainWindow)App.Current.MainWindow).MashupTop && dropSource.LesserSet.Top == ((MainWindow)App.Current.MainWindow).MashupTop)
                    {
                        // Some dimension from mashup is dropped to a table in the mashup: add aggregated column
                        ICommand cmd = ((MainWindow)App.Current.MainWindow).Resources["AddAggregationCommand"] as ICommand;

                        if (cmd == null) return;

                        if (cmd.CanExecute(null))
                        {
                            cmd.Execute(null);
                        }
                    }

                }
            }

        }

        private void SubsetTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // TODO: This control does not know what to do with actions like double click or drag-n-drop
            // Therefore, it should do its logic (if any) and propagate this event to parent controls where it should be cought by the main window dispatcher or the view dispatcher.

            if (this == ((MainWindow)App.Current.MainWindow).MashupsView)
            {
                ICommand cmd = ((MainWindow)App.Current.MainWindow).Resources["OpenTableCommand"] as ICommand;

                if (cmd == null) return;

                if (cmd.CanExecute(null))
                {
                    cmd.Execute(null);
                }
            }
            else if (this == ((MainWindow)App.Current.MainWindow).SourcesView)
            {
                ;
            }
        }

        // Helper to search up the VisualTree
        private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        // Alternative
        public static T GetVisualParent<T>(Visual referencedVisual) where T : Visual
        {
            Visual parent = referencedVisual;

            while (parent != null && !object.ReferenceEquals(parent.GetType(), typeof(T)))
            {
                parent = VisualTreeHelper.GetParent(parent) as Visual;
            }

            var parent1 = VisualTreeHelper.GetParent(referencedVisual);

            return parent as T;
        }

    }

    // http://msdn.microsoft.com/en-us/library/system.windows.controls.datatemplateselector.aspx
    public class SubsetTreeDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // Here we can also choose appropriate templates depending on item properties (like item.IsPrimitive) and not only on its class as it is done in XAML
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null)
            {
                if (item is SubsetTree)
                {
                    SubsetTree nodeItem = item as SubsetTree;
                    if (nodeItem.Dim.LesserSet.IsRoot)
                    {
                        return element.FindResource("rootItemTemplate") as DataTemplate;
                    }
                    else if (nodeItem.IsSubsetNode)
                    {
                        return element.FindResource("setItemTemplate") as DataTemplate;
                    }
                    else if (nodeItem.IsDimensionNode)
                    {
                        return element.FindResource("dimensionItemTemplate") as DataTemplate;
                    }
                }
                else if (item is Set)
                {
                    Set setItem = item as Set;
                    return element.FindResource("setItemTemplate") as DataTemplate;
                }
                else if (item is Dim)
                {
                    Dim dimItem = item as Dim;
                    return element.FindResource("dimensionItemTemplate") as DataTemplate;
                }
                else if (item.GetType().IsGenericType) // It is used for TreeNode<T> (where T is Set or Dim) or other generic item types
                {
                    // Generic types cannot be used in XAML so we have to choose template in code
                    // http://stackoverflow.com/questions/13804581/generic-classes-dont-bind-to-hierarchicaldatatemplate

                    // Note that primitive dimensions are actually generic classes (like DimPrimitive<int>)
                    var genericTypeDefinition = item.GetType().GetGenericTypeDefinition();
                    var key = new DataTemplateKey(genericTypeDefinition);
                    return element.TryFindResource(key) as DataTemplate;
                }
            }

            return null;
        }
    }

}
