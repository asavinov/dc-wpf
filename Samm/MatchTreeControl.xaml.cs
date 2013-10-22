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


            // Redraw both trees. Use property name like "CanMatch" or String.Empty for all properties (but it does not work because selection is also updated)
            ((MatchTree)treeItem.Root).MappingModel.SourceTree.NotifyAllOnPropertyChanged("CanMatch");
            ((MatchTree)treeItem.Root).MappingModel.TargetTree.NotifyAllOnPropertyChanged("CanMatch");

            ((MatchTree)treeItem.Root).MappingModel.SourceTree.NotifyAllOnPropertyChanged("IsMatched");
            ((MatchTree)treeItem.Root).MappingModel.TargetTree.NotifyAllOnPropertyChanged("IsMatched");

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
}
