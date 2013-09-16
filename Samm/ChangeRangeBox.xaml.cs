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
    /// Interaction logic for ChangeRangeBox.xaml
    /// </summary>
    public partial class ChangeRangeBox : Window
    {

        public MatchTree MatchTreeModel { get; set; }

        public void RefreshAll()
        {
            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            sourceColumn.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            matchTree.MatchTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();

            // !!! Use it for other controls where we update the data context and need to refresh the view
            matchTree.GetBindingExpression(TreeView.DataContextProperty).UpdateTarget();
        }
        public ChangeRangeBox()
        {
            InitializeComponent();
        }
    }
}
