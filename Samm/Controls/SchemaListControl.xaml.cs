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
        public Application Application { get; set; }

        // Schemas from this space are shown (context)
        protected DcSpace _space;
        public DcSpace Space 
        {
            get { return _space; }
            set
            {
                if (_space == value) return;
                if (_space != null)
                {
                    ((Space)_space).CollectionChanged -= this.CollectionChanged; // Unregister from the old space
                }
                Items.Clear();
                _space = value;
                if (_space == null) return;
                ((Space)_space).CollectionChanged += this.CollectionChanged; // Unregister from the old space

                // Fill the list of items
                foreach (DcTable schema in _space.GetSchemas())
                {
                    Items.Add(schema);
                }
            }
        }

        // What is displayed in the list and bound to it as (ItemsSource)
        public ObservableCollection<DcTable> Items { get; set; }

        // It is what we bind to the list view (SelectedItem)
        private DcSchema _selectedItem;
        public DcSchema SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;

                MainWindow main = (MainWindow)Application.MainWindow;
                main.TableListView.Schema = _selectedItem;

                main.FormulaBarType.Text = "";
                main.FormulaBarName.Text = "";
                main.FormulaBarFormula.Text = "";
            }
        }


        public SchemaListControl()
        {
            Items = new ObservableCollection<DcTable>();

            InitializeComponent();
        }

        // Process events from the space about adding/removing schemas
        protected void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add) // Decide if this node has to add a new child node
            {
                DcSchema sch = e.NewItems != null && e.NewItems.Count > 0 && e.NewItems[0] is DcSchema ? (DcSchema)e.NewItems[0] : null;
                if (sch == null) return;
                if (Items.Contains(sch)) return;

                Items.Add(sch);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                DcSchema sch = e.OldItems != null && e.OldItems.Count > 0 && e.OldItems[0] is DcSchema ? (DcSchema)e.OldItems[0] : null;
                if (sch == null) return;
                if (!Items.Contains(sch)) return;

                Items.Remove(sch);
            }
        }

    }
}
