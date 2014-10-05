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
using System.Windows.Shapes;

using Com.Model;

namespace Samm.Dialogs
{
    /// <summary>
    /// Interaction logic for ProductTableBox.xaml
    /// </summary>
    public partial class ProductTableBox : Window
    {
        bool IsNew { get; set; }

        public ComSchema Schema { get; set; }

        public ComTable SourceTable { get; set; }

        public string NewTableName { get; set; }

        public List<GreaterTableEntry> GreaterTables { get; set; }
        public int SelectedCount { get { return GreaterTables.Count(x => x.IsSelected); } }


        public ProductTableBox(ComSchema schema, ComTable table, ComTable greaterTable)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            Schema = schema;
            SourceTable = table;

            GreaterTables = new List<GreaterTableEntry>();

            // For each table, create an entry object
            foreach (ComTable gTab in schema.Root.SubTables)
            {
                GreaterTableEntry entry = new GreaterTableEntry(gTab);
                if (gTab == greaterTable)
                {
                    entry.IsSelected = true;
                }
                GreaterTables.Add(entry);
            }

            InitializeComponent();

            newTableName.Text = table.Name;

            if (greaterTable != null)
            {
                greaterTables.SelectedItem = greaterTable;
            }

            RefreshAll();
        }

        public void RefreshAll()
        {
            this.GetBindingExpression(ProductTableBox.DataContextProperty).UpdateTarget(); // Does not work

            greaterTables.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();

            //newTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }

        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            if (string.IsNullOrWhiteSpace(newTableName.Text)) return false;

            if (SelectedCount == 0) return false;

            return true;
        }
        private void OkCommand_Executed(object state)
        {
            // Save name
            string productTableName = newTableName.Text;
            if (string.IsNullOrWhiteSpace(productTableName) || string.IsNullOrWhiteSpace(productTableName)) return;
            if (SelectedCount == 0) return;

            SourceTable.Name = productTableName;

            SourceTable.Definition.DefinitionType = TableDefinitionType.PRODUCT;

            // Save table
            Schema.AddTable(SourceTable, null, null);

            // Initialize a list of selected dimensions (from the whole list of all greater dimensions
            List<ComTable> greaterSets = new List<ComTable>();
            foreach (var entry in GreaterTables)
            {
                if (!entry.IsSelected) continue;
                greaterSets.Add(entry.Table);
            }

            // Create identity dimensions for the product set
            foreach (ComTable gSet in greaterSets)
            {
                ComColumn gDim = Schema.CreateColumn(gSet.Name, SourceTable, gSet, true);
                gDim.Add();
            }

            this.DialogResult = true;
        }
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }

    /// <summary>
    /// It corresponds to one greater table shown in the list.
    /// </summary>
    public class GreaterTableEntry
    {
        public ComTable Table { get; set; }

        public string Name { get { return Table.Name; } }

        protected bool _isSelected;
        public bool IsSelected 
        {
            get { return _isSelected; }
            set 
            {
                if (_isSelected == value) return;
                _isSelected = value;
            } 
        }

        public GreaterTableEntry(ComTable table)
        {
            Table = table;

            IsSelected = false;
        }

    }

}
