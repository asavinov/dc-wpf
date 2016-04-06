using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Com.Schema;

namespace Samm.Controls
{
    /// <summary>
    /// Interaction logic for ColumnListControl.xaml
    /// </summary>
    public partial class ColumnListControl : UserControl
    {
        public ColumnListControl()
        {
            InitializeComponent();

            MainWindow vm = ((MainWindow)DataContext);
            if (vm != null)
            {
                ((Space)vm.Space).CollectionChanged += this.CollectionChanged;
            }
        }

        // Process events from the table about adding/removing tables
        [Obsolete("We listen for space events directly in the view model (MainWindow).")]
        public void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MainWindow vm = ((MainWindow)DataContext);

            if (e.Action == NotifyCollectionChangedAction.Add) // Decide if this node has to add a new child node
            {
                DcColumn column = e.NewItems != null && e.NewItems.Count > 0 && e.NewItems[0] is DcColumn ? (DcColumn)e.NewItems[0] : null;
                if (column == null) return;
                if (column.Input != vm.SelectedTable) return;
                if (vm.ColumnList.Contains(column)) return;

                vm.ColumnList.Add(column);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                DcColumn column = e.OldItems != null && e.OldItems.Count > 0 && e.OldItems[0] is DcColumn ? (DcColumn)e.OldItems[0] : null;
                if (column == null) return;
                if (column.Input != vm.SelectedTable) return;
                if (!vm.ColumnList.Contains(column)) return;

                vm.ColumnList.Remove(column);
            }
        }
    }
}
