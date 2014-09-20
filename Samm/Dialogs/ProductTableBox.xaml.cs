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

        public List<ComTable> GreaterTables { get; set; }

        public ProductTableBox(ComSchema schema, ComTable table, ComTable greaterTable)
        {
            Schema = schema;
            SourceTable = table;

            GreaterTables = new List<ComTable>();
            GreaterTables.AddRange(Schema.Root.AllSubTables); // Fill the list with potential greater tables

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

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            // Save name
            string productTableName = newTableName.Text;
            if (string.IsNullOrWhiteSpace(productTableName) || string.IsNullOrWhiteSpace(productTableName) || greaterTables.SelectedItems.Count == 0) return;

            SourceTable.Name = productTableName;

            SourceTable.Definition.DefinitionType = TableDefinitionType.PRODUCT;

            // Save table
            Schema.AddTable(SourceTable, null, null);

            // Initialize a list of selected dimensions (from the whole list of all greater dimensions
            List<ComTable> greaterSets = new List<ComTable>();
            foreach (var item in greaterTables.SelectedItems)
            {
                greaterSets.Add((ComTable)item);
            }

            // Create identity dimensions for the product set
            foreach (ComTable gSet in greaterSets)
            {
                ComColumn gDim = Schema.CreateColumn(gSet.Name, SourceTable, gSet, true);
                gDim.Add();
            }

            this.DialogResult = true;
        }
    }
}
