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
                if (Column == null || Column.Input == null) return false;
                if (Column.Input.Schema.GetType() == typeof(Schema)) return false;
                return true;
            }
        }
        bool IsExport // If target is not mashup (changes for each schema selection)
        {
            get
            {
                if (SelectedTargetSchema != null)
                {
                    if (SelectedTargetSchema.GetType() == typeof(Schema)) return false;
                    else return true;
                }
                else
                {
                    if (Column == null || Column.Output == null) return false;
                    if (Column.Output.Schema.GetType() == typeof(Schema)) return false;
                    return true;
                }
            }
        }

        //
        // Link column connecting an existing fixed source table with a target table
        //
        public DcColumn Column { get; set; } // Generating projection column with the mapping to be crated/edited
        public string ColumnName { get; set; }

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
            //sourceColumn.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            targetSchemaList.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
            targetTableList.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
        }

        public ColumnBox(ObservableCollection<DcSchema> targetSchemas, DcColumn column)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            //
            // Options and regime of the dialog
            //
            if (column.Input.Columns.Contains(column)) IsNew = false;
            else IsNew = true;

            Column = column;
            ColumnName = column.Name;

            //
            // Init target schema list
            //
            TargetSchemas = targetSchemas;
            TargetTables = new ObservableCollection<DcTable>();

            InitializeComponent();

            if (IsNew)
            {
                if (Column.Output != null)
                {
                    SelectedTargetSchema = column.Output.Schema;
                }
                if (SelectedTargetSchema == null)
                {
                    SelectedTargetSchema = targetSchemas[0];
                }

                targetSchemaList.IsEnabled = true;
                targetTableList.IsEnabled = true;
            }
            else
            {
                targetSchemaList.IsEnabled = false;
                targetTableList.IsEnabled = false;

                SelectedTargetSchema = column.Output.Schema;
                SelectedTargetTable = column.Output;
            }

            RefreshAll();
        }

        private void TargetSchemaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TargetTables.Clear();
            if (SelectedTargetSchema != null)
            {
                List<DcTable> targetTables;
                if (SelectedTargetSchema == Column.Input.Schema) // Intra-schema link
                {
                    targetTables = MappingModel.GetPossibleGreaterSets(Column.Input);
                }
                else // Import-export link
                {
                    targetTables = SelectedTargetSchema.Root.SubTables;
                }
                targetTables.ForEach(x => TargetTables.Add(x));
            }

            RefreshAll();
        }

        private void TargetTableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Column.Output = SelectedTargetTable;
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

            if (string.IsNullOrWhiteSpace(columnName.Text)) return false;

            return true;
        }
        private void OkCommand_Executed(object state)
        {
            Mapping mapping;
            if (IsNew)
            {
                mapping = new Mapping(Column.Input, Column.Output);
            }
            else
            {
                mapping = Column.Definition.Mapping;
            }

            Column.Name = ColumnName;
            //Column.Output.Name = SelectedTargetTable.Name;

            if (IsNew)
            {
                // Target table could contain original columns from previous uses (loaded from csv file or added manually). Now they are not needed.
                foreach (DcColumn targetColumn in Column.Output.Columns)
                {
                    PathMatch match = mapping.GetMatchForTarget(new DimPath(targetColumn));
                    if (match != null) continue;
                    if (targetColumn.Definition.DefinitionType != DcColumnDefinitionType.FREE) continue;
                    if (targetColumn.Definition.DefinitionType != DcColumnDefinitionType.ANY) continue;

                    targetColumn.Remove();
                }

                // Set parameters of the new column
                Column.Definition.DefinitionType = DcColumnDefinitionType.LINK;
                Column.Definition.Mapping = mapping;
                Column.Definition.IsAppendData = true;

                Column.Output.Definition.DefinitionType = DcTableDefinitionType.PROJECTION;
            }

            this.DialogResult = true;
        }

    }

}
