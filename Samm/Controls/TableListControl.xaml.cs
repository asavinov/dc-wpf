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
    /// Interaction logic for TableListControl.xaml
    /// </summary>
    public partial class TableListControl : UserControl
    {
        // Tables from this schema are shown (context)
        protected DcSchema _schema;
        public DcSchema Schema 
        {
            get { return _schema; }
            set
            {
                if (_schema == value) return;
                if (_schema != null)
                {
                    ((Table)_schema.Root).CollectionChanged -= this.CollectionChanged; // Unregister from the old schema
                }
                Items.Clear();
                _schema = value;
                if (_schema == null) return;
                ((Table)_schema.Root).CollectionChanged += this.CollectionChanged; // Unregister from the old schema

                // Fill the list of items
                foreach (DcTable table in _schema.Root.SubTables)
                {
                    Items.Add(table);
                }
            }
        }

        // What is displayed in the list and bound to it as (ItemsSource)
        public ObservableCollection<DcTable> Items { get; set; }

        // It is what we bind to the list view (SelectedItem)
        private DcTable _selectedItem;
        public DcTable SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                
                MainWindow main = (MainWindow)Application.Current.MainWindow;
                main.ColumnListView.Table = _selectedItem;
            }
        }


        public TableListControl()
        {
            Items = new ObservableCollection<DcTable>();

            InitializeComponent();
        }

        // Process events from the schema about adding/removing tables
        protected void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add) // Decide if this node has to add a new child node
            {
                DcColumn column = e.NewItems != null && e.NewItems.Count > 0 ? (DcColumn)e.NewItems[0] : null;
                if (column == null) return;

                if (column.IsSuper && !Items.Contains(column.Input))
                {
                    Items.Add(column.Input);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                DcColumn column = e.OldItems != null && e.OldItems.Count > 0 ? (DcColumn)e.OldItems[0] : null;
                if (column == null) return;

                if (column.IsSuper && Items.Contains(column.Input))
                {
                    Items.Remove(column.Input);
                }
            }
        }

    }
}
