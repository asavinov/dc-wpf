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

        public CsSchema Schema { get; set; }

        public CsTable SourceTable { get; set; }

        public List<CsTable> GreaterTables { get; set; }

        public ProductTableBox(CsSchema schema, CsTable table, CsTable greaterTable)
        {
            Schema = schema;
            SourceTable = table;

            GreaterTables = new List<CsTable>();
            GreaterTables.AddRange(Schema.Root.GetAllSubsets()); // Fill the list with potential greater tables

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

            // Save table
            Schema.AddTable(SourceTable, null, null);

            // Initialize a list of selected dimensions (from the whole list of all greater dimensions
            List<CsTable> greaterSets = new List<CsTable>();
            foreach (var item in greaterTables.SelectedItems)
            {
                greaterSets.Add((CsTable)item);
            }

            // Create identity dimensions for the product set
            foreach (CsTable gSet in greaterSets)
            {
                CsColumn gDim = Schema.CreateColumn(gSet.Name, SourceTable, gSet, true);
                gDim.Add();
            }

            this.DialogResult = true;
        }
    }
}
