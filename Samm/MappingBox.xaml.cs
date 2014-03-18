using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

using Com.Model;

namespace Samm
{
    /// <summary>
    /// Interaction logic for MappingBox.xaml
    /// </summary>
    public partial class MappingBox : Window
    {
        public MappingModel MappingModel { get; set; }

        public void RefreshAll()
        {
            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            //sourceColumn.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            sourceTree.MatchTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();

            //targetTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            // !!! Use it for other controls where we update the data context and need to refresh the view
            sourceTree.GetBindingExpression(TreeView.DataContextProperty).UpdateTarget();
            targetTree.GetBindingExpression(TreeView.DataContextProperty).UpdateTarget();

            this.GetBindingExpression(ChangeTypeBox.DataContextProperty).UpdateTarget();
        }

        public MappingBox()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void identityButton_Click(object sender, RoutedEventArgs e)
        {
            if (MappingModel.TargetTree.SelectedNode != null && MappingModel.TargetTree.SelectedNode.Dim != null) 
            {
                if (MappingModel.TargetTree.SelectedNode.Dim.IsIdentity)
                {
                    MappingModel.TargetTree.SelectedNode.Dim.IsIdentity = false;
                }
                else
                {
                    MappingModel.TargetTree.SelectedNode.Dim.IsIdentity = true;
                }
                MappingModel.TargetTree.NotifyAllOnPropertyChanged(""); // OPTIMIZE: notify only about IsIdentity property - not all properties
            }
        }

        private void recommendButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addMatchButton_Click(object sender, RoutedEventArgs e)
        {
            MappingModel.AddMatch();

            // Redraw both trees. Use property name like "CanMatch" or String.Empty for all properties (but it does not work because selection is also updated)
            MappingModel.SourceTree.NotifyAllOnPropertyChanged("CanMatch");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("CanMatch");

            MappingModel.SourceTree.NotifyAllOnPropertyChanged("IsMatched");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("IsMatched");
        }

        private void removeMatchButton_Click(object sender, RoutedEventArgs e)
        {
            MappingModel.RemoveMatch();

            // Redraw both trees. Use property name like "CanMatch" or String.Empty for all properties (but it does not work because selection is also updated)
            MappingModel.SourceTree.NotifyAllOnPropertyChanged("CanMatch");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("CanMatch");

            MappingModel.SourceTree.NotifyAllOnPropertyChanged("IsMatched");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("IsMatched");
        }

    }
}
