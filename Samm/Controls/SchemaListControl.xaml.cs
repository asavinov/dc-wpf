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
    /// Interaction logic for SchemaListControl.xaml
    /// </summary>
    public partial class SchemaListControl : UserControl
    {
        public SchemaListControl()
        {
            InitializeComponent();

            MainWindow vm = ((MainWindow)DataContext);
            if (vm != null)
            {
                ((Space)vm.Space).CollectionChanged += this.CollectionChanged;
            }
        }

        // Process events from the space about adding/removing schemas
        [Obsolete("We listen for space events directly in the view model (MainWindow).")]
        public void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MainWindow vm = ((MainWindow)DataContext);

            if (e.Action == NotifyCollectionChangedAction.Add) // Decide if this node has to add a new child node
            {
                DcSchema sch = e.NewItems != null && e.NewItems.Count > 0 && e.NewItems[0] is DcSchema ? (DcSchema)e.NewItems[0] : null;
                if (sch == null) return;
                if (vm.SchemaList.Contains(sch)) return;

                vm.SchemaList.Add(sch);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                DcSchema sch = e.OldItems != null && e.OldItems.Count > 0 && e.OldItems[0] is DcSchema ? (DcSchema)e.OldItems[0] : null;
                if (sch == null) return;
                if (!vm.SchemaList.Contains(sch)) return;

                vm.SchemaList.Remove(sch);
            }
        }

    }
}
