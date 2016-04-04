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
                    ((Space)_schema.Space).CollectionChanged -= this.CollectionChanged; // Unregister from the old schema
                }
                Items.Clear();
                _schema = value;
                if (_schema == null) return;
                ((Space)_schema.Space).CollectionChanged += this.CollectionChanged; // Unregister from the old schema

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

                main.FormulaBarType.Text = "";
                main.FormulaBarName.Text = "";
                main.FormulaBarFormula.Text = "";
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
                DcTable tab = e.NewItems != null && e.NewItems.Count > 0 && e.NewItems[0] is DcTable ? (DcTable)e.NewItems[0] : null;
                if (tab == null) return;
                if (Items.Contains(tab)) return;

                Items.Add(tab);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                DcTable tab = e.OldItems != null && e.OldItems.Count > 0 && e.OldItems[0] is DcTable ? (DcTable)e.OldItems[0] : null;
                if (tab == null) return;
                if (!Items.Contains(tab)) return;

                Items.Remove(tab);
            }
        }

    }
}
