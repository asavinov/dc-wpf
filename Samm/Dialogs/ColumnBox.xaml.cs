using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Interaction logic for ColumnBox.xaml
    /// Differences depending on the usage context:
    /// - Import: by default no keys (maybe hide key column, but key editing can be allowed), Extract: all included all chosen targets are always keys (hide key column or make it disabled with always checked)
    /// - New: Possible/existing target columns are empty: Import: initially all are automatically included. Extract: either no or explicitly specified are initially included
    /// - Edit: initial inclusion is taken from the existing mapping (what about posible targets?)
    /// </summary>
    public partial class ColumnBox : Window, INotifyPropertyChanged
    {
        //
        // Options defining the regime of the dialog
        //
        bool IsNew { get; set; } // Set in constructor
        bool IsImport // If source is not mashup (changes for each schema selection)
        {
            get
            {
                if (Table.Schema.GetType() == typeof(Schema)) return false;
                return true;
            }
        }
        bool IsExport // If target is not mashup (changes for each schema selection)
        {
            get
            {
                if (Table.Schema.GetType() != typeof(Schema)) return false;
                if (SelectedTargetSchema == null) return false; // We do not know
                return true;
            }
        }

        //
        // Link column connecting an existing fixed source table with a target table
        //
        public DcTable Table { get; set; }

        public string ColumnName { get; set; }
        public bool IsKey { get; set; }
        public string ColumnFormula { get; set; }

        //
        // Target table including its schema
        //
        public ObservableCollection<DcSchema> TargetSchemas { get; set; }
        public DcSchema SelectedTargetSchema { get; set; }
        public ObservableCollection<DcTable> TargetTables { get; set; }
        public DcTable SelectedTargetTable { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshAll()
        {
            this.GetBindingExpression(ColumnBox.DataContextProperty).UpdateTarget();

            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            columnName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            isKey.GetBindingExpression(CheckBox.IsCheckedProperty).UpdateTarget();
            columnFormula.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            targetSchemaList.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            targetTableList.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();

            targetSchemaList.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
            targetTableList.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
        }

        public ColumnBox(ObservableCollection<DcSchema> targetSchemas, DcTable table, DcColumn column)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            InitializeComponent();

            //
            // Init target schema list
            //

            // Rule/constraints for possible schemas: 
            // - If input schema is remote, then output schema is only Mashup

            TargetSchemas = targetSchemas;
            TargetTables = new ObservableCollection<DcTable>();

            Table = table;

            if (column == null)
            {
                IsNew = true;

                ColumnName = "New Column";
                IsKey = false;
                ColumnFormula = "";

                if (targetSchemas != null && targetSchemas.Count > 0)
                {
                    SelectedTargetSchema = targetSchemas[0];
                }

                //targetSchemaList.IsEnabled = true;
                //targetTableList.IsEnabled = true;
            }
            else
            {
                IsNew = false;

                ColumnName = column.Name;
                IsKey = column.IsKey;
                ColumnFormula = column.Definition.Formula;

                SelectedTargetSchema = column.Output.Schema;
                SelectedTargetTable = column.Output;

                //targetSchemaList.IsEnabled = false;
                //targetTableList.IsEnabled = false;
            }

            RefreshAll();
        }

        private void TargetSchemaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TargetTables.Clear();
            if (SelectedTargetSchema != null)
            {
                // Rules/constraints:
                // - Generating columns cannot target a primitive type
                //   - Import/export is always generating column so cannot target a primitive type
                // - Can we have generating column cycles?
                //   - Can we have import/export cycles?

                // Add primitive tables
                var primitiveTables = SelectedTargetSchema.SubTables.Where(x => x != SelectedTargetSchema.Root).ToList();
                primitiveTables.ForEach(x => TargetTables.Add(x));

                // Add non-primitive tables (only possible/meainingful)
                List<DcTable> targetTables;
                if (SelectedTargetSchema == Table.Schema) // Intra-schema link
                {
                    targetTables = MappingModel.GetPossibleGreaterSets(Table);
                }
                else // Import-export link - all tables
                {
                    targetTables = SelectedTargetSchema.Root.SubTables;
                }
                targetTables.ForEach(x => TargetTables.Add(x));
            }

            RefreshAll();
        }

        private void TargetTableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ;
        }

        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            if (SelectedTargetSchema == null) return false;
            if (SelectedTargetTable == null) return false;

            if (string.IsNullOrWhiteSpace(ColumnName)) return false;

            return true;
        }
        private void OkCommand_Executed(object state)
        {
            this.DialogResult = true;
        }

    }

}
