using System;
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
    /// Interaction logic for MatchTreeControl.xaml
    /// </summary>
    public partial class MatchTreeControl : UserControl
    {
        public MatchTreeControl()
        {
            InitializeComponent();
        }

        private void MatchTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            MatchTreeNode treeItem = (MatchTreeNode)treeView.SelectedItem;

            ((MatchTree)DataContext).SelectedNode = treeItem;


            MappingModel MappingModel = ((MatchTree)treeItem.Root).MappingModel;
            // Redraw both trees. Use property name like "CanMatch" or String.Empty for all properties (but it does not work because selection is also updated)
            MappingModel.SourceTree.NotifyAllOnPropertyChanged("CanMatch");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("CanMatch");

            MappingModel.SourceTree.NotifyAllOnPropertyChanged("IsMatched");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("IsMatched");

            this.UpdateLayout();
            this.UpdateLayout();

            treeView.UpdateLayout();
            treeView.UpdateLayout();

            // treeView.Items.Refresh(); // Error: stack overflow
            
            //GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();

            //ForceUIToUpdate();
            //this.OnPropertyChanged("IsSelected");
        }

    }

    // http://msdn.microsoft.com/en-us/library/system.windows.controls.datatemplateselector.aspx
    public class MatchTreeDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // Here we can also choose appropriate templates depending on item properties (like item.IsPrimitive) and not only on its class as it is done in XAML
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null)
            {
                if (item is MatchTree)
                {
                    return element.FindResource("defaultItemTemplate") as DataTemplate;
                }
                if (item is MatchTreeNode)
                {
                    MatchTreeNode nodeItem = item as MatchTreeNode;

                    // Choose template depending on the role and status of this item (root, source, target, selected etc.)
                    if (((MatchTree)nodeItem.Root).IsSource)
                    {
                        return element.FindResource("sourceItemTemplate") as DataTemplate;
                    }
                    else if (((MatchTree)nodeItem.Root).IsTarget) 
                    {
                        return element.FindResource("targetItemTemplate") as DataTemplate;
                    }
                    else
                    {
                        // ERROR: Something went wrong. 
                    }
                }
                else if (item is Set)
                {
                    Set setItem = item as Set;
                }
                else if (item is Dim)
                {
                    Dim dimItem = item as Dim;
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
