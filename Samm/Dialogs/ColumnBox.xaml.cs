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

using Com.Schema;

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
        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyNotifyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [Obsolete("Property change notifications are used instead.")]
        public void RefreshAll()
        {
            this.GetBindingExpression(ColumnBox.DataContextProperty).UpdateTarget();

            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            columnName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            isKey.GetBindingExpression(CheckBox.IsCheckedProperty).UpdateTarget();
            columnFormula.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            outputSchemaList.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            outputTableList.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();

            outputSchemaList.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
            outputTableList.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
        }

        //
        // Options defining the regime of the dialog
        //
        bool IsNew
        {
            get { return Column == null; }
        }

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
                if (SelectedOutputSchema == null) return false; // We do not know
                return true;
            }
        }

        //
        // Context/parameters
        //
        private MainWindow mainVM; // Access to the main view model including Space

        private DcTable _table;
        public DcTable Table
        {
            get { return Column == null ? _table : Column.Input; }
            set
            {
                _table = value;

                // Explicitly setting (input) table means that the dialog is intended to add a new column
                Column = null;
            }
        }

        private DcColumn _column;
        public DcColumn Column
        {
            get { return _column; }
            set
            {
                _column = value;
                if(_column != null) _table = _column.Input;

                initViewModel();
            }
        }

        //
        // View model. Properties of the object shown in UI controls.
        //

        public string ColumnName { get; set; }
        public bool IsKey { get; set; }
        public string ColumnFormula { get; set; }

        public ObservableCollection<DcSchema> OutputSchemas { get; set; }
        public DcSchema SelectedOutputSchema { get; set; }
        private void OutputSchemaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Populate type-list with possible tables. Type-list filling is provided by a Space function (not all types are possible because of cycles and maybe other constraints)
            OutputTables.Clear();
            if (SelectedOutputSchema != null)
            {
                // Rules/constraints:
                // - Generating columns (returning tuples) cannot target a primitive type
                //   - Import/export is always generating column so cannot target a primitive type
                // - Can we have generating column cycles?
                //   - Can we have import/export cycles?

                // Add primitive tables
                var primitiveTables = SelectedOutputSchema.SubTables.Where(x => x != SelectedOutputSchema.Root).ToList();
                primitiveTables.ForEach(x => OutputTables.Add(x));

                // Add non-primitive tables (only possible/meainingful)
                List<DcTable> outputTables;
                if (SelectedOutputSchema == Table.Schema) // Intra-schema link
                {
                    outputTables = MappingModel.GetPossibleGreaterSets(Table);
                }
                else // Import-export link - all tables
                {
                    outputTables = SelectedOutputSchema.Root.SubTables;
                }
                outputTables.ForEach(x => OutputTables.Add(x));
            }

        }

        // This type-list will be populated automatically from within schema selection event.
        public ObservableCollection<DcTable> OutputTables { get; set; }
        public DcTable SelectedOutputTable { get; set; }
        private void OutputTableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ;
        }

        // It is called when preparing this dialog for editing/adding a column or when context changes (not during the process)
        // Essentially, we do it when data context is set.
        private void initViewModel()
        {
            // Populate possible schemas using all schemas from the space with some limitations. 
            // Rule/constraints for possible schemas: 
            // - If input schema (always non-null) is remote, then output schema is only Mashup
            DcSpace space = mainVM.Space;
            DcSchema schema = Table.Schema;
            OutputSchemas.Clear();
            List<DcSchema> allSchemas = space.GetSchemas();
            allSchemas.ForEach(x => OutputSchemas.Add(x));

            if (IsNew)
            {
                // Set default parameters: name, schema (e.g., if single), type, key
                ColumnName = "New Column";
                IsKey = false;
                ColumnFormula = "";

                // Set selections
                SelectedOutputSchema = mainVM.MashupTop;

                // Set enabled/disabled
                //targetSchemaList.IsEnabled = true;
                //targetTableList.IsEnabled = true;

            }
            else
            {
                // Set existing column parameters: name, schema, type, key
                ColumnName = Column.Name;
                IsKey = Column.IsKey;
                ColumnFormula = Column.GetData().Formula;

                // Set selections
                SelectedOutputSchema = Column.Output.Schema;
                SelectedOutputTable = Column.Output;

                // Set enabled/disabled
                //targetSchemaList.IsEnabled = false;
                //targetTableList.IsEnabled = false;
            }

            FirePropertyNotifyChanged("");
        }

        public ColumnBox(MainWindow mainVM)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            this.mainVM = mainVM;

            OutputSchemas = new ObservableCollection<DcSchema>();
            OutputTables = new ObservableCollection<DcTable>();

            InitializeComponent();
        }

        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            if (SelectedOutputSchema == null) return false;
            if (SelectedOutputTable == null) return false;

            if (string.IsNullOrWhiteSpace(ColumnName)) return false;

            // New (user-defined) remote columns can only point to mash up (no intra-remote columns)
            if(!mainVM.IsInMashups(Table) && !mainVM.IsInMashups(SelectedOutputTable))
            {
                if(IsNew) // No intra-remote new columns
                {
                    return false;
                }
                else if (mainVM.IsInMashups(Column.Output)) // No convertion from import to intra-remote
                {
                    return false;
                }
            }

            return true;
        }
        private void OkCommand_Executed(object state)
        {
            if (IsNew)
            {
                // Create a new column using parameters in the dialog
                DcSpace space = mainVM.Space;
                DcColumn column = space.CreateColumn(ColumnName, Table, SelectedOutputTable, IsKey);
                column.GetData().Formula = ColumnFormula;
                column.GetData().IsAppendData = true;

                Column = column;
            }
            else
            {
                // Update the column using parameters in the dialog
                Column.Name = ColumnName;
                Column.GetData().Formula = ColumnFormula;
                Column.Output = SelectedOutputTable;

                // TODO: Here we need a smarter way to determine the dirty flag. If only name changes then the dirty flag is not invalidated. 
                Column.IsUpToDate = false;
            }

            ((Column)Column).NotifyPropertyChanged("");

            this.DialogResult = true;
        }

    }

}
