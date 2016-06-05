using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using Com.Schema.Csv;

namespace Samm.Dialogs
{
    /// <summary>
    /// Interaction logic for TableCsvBox.xaml
    /// </summary>
    public partial class TableCsvBox : Window, INotifyPropertyChanged
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
            this.GetBindingExpression(TableCsvBox.DataContextProperty).UpdateTarget();

            tableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            filePath.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            delimiter.GetBindingExpression(ComboBox.SelectedValueProperty).UpdateTarget();
            decimalSeparator.GetBindingExpression(ComboBox.SelectedValueProperty).UpdateTarget();

            hasHeaderRecord.GetBindingExpression(CheckBox.IsCheckedProperty).UpdateTarget();

            tableColumns.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
        }

        //
        // Options defining the regime of the dialog
        //
        bool IsNew
        {
            get { return Table == null; }
        }

        //
        // Context/parameters
        //
        private MainWindow mainVM; // Access to the main view model including Space

        private DcSchema _schema;
        public DcSchema Schema
        {
            get { return Table == null ? _schema : Table.Schema; }
            set
            {
                _schema = value;

                // Explicitly setting schema table means that the dialog is intended to add a new table
                Table = null;
            }
        }

        private DcTable _table;
        public DcTable Table
        {
            get { return _table; }
            set
            {
                _table = value;
                if (_table != null) _schema = _table.Schema;

                initViewModel();
            }
        }

        //
        // View model. Properties of the object shown in UI controls.
        //
        public string TableName { get; set; }
        public string TableFormula { get; set; }

        // Csv specific properties
        public string FilePath { get; set; }

        public ObservableCollection<string> TableColumns { get; set; }
        private void UpdateColumnList() // Read table structure from file and show (table itself is not changed)
        {
            // Display new schema and maybe sample data
            TableColumns.Clear();

            if (!File.Exists(FilePath))
            {
                return;
            }

            // Load column names from the file into the list
            ConnectionCsv connection = new ConnectionCsv();
            connection.OpenReader(FilePath, HasHeaderRecord, Delimiter, Decimal, Encoding.UTF8);
            List<string> columnNames = connection.ReadColumns();
            connection.CloseReader();

            TableColumns.Clear();
            if (columnNames == null)
            {
                return;
            }
            foreach (string column in columnNames)
            {
                TableColumns.Add(column);
            }
        }

        public bool HasHeaderRecord { get; set; }
        private void Header_Changed(object sender, RoutedEventArgs e)
        {
            UpdateColumnList();
        }

        public string Delimiter { get; set; }
        private void Delimiter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateColumnList();
        }

        public string Decimal { get; set; }
        private void Decimal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateColumnList();
        }

        // It is called when preparing this dialog for editing/adding a table or when context changes (not during the process)
        // Essentially, we do it when data context is set.
        private void initViewModel()
        {
            DcSpace space = mainVM.Space;

            if (IsNew)
            {
                TableName = "New Table";
                TableFormula = "";

                FilePath = "";
                HasHeaderRecord = true;
                Delimiter = ",";
                Decimal = ".";

                TableColumns.Clear();
            }
            else
            {
                TableName = Table.Name;
                TableFormula = Table.GetData().WhereFormula;

                FilePath = ((TableCsv)Table).FilePath;
                HasHeaderRecord = ((TableCsv)Table).HasHeaderRecord;
                Delimiter = ((TableCsv)Table).Delimiter;
                Decimal = ((TableCsv)Table).CultureInfo.NumberFormat.NumberDecimalSeparator;

                UpdateColumnList();
            }

            FirePropertyNotifyChanged("");
        }

        public TableCsvBox(MainWindow mainVM)
        {
            this.chooseSourceCommand = new DelegateCommand(this.ChooseSourceCommand_Executed, this.ChooseSourceCommand_CanExecute);
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            this.mainVM = mainVM;
            TableColumns = new ObservableCollection<string>();

            InitializeComponent(); // Setting selected values needs comboboxes to be filled 
        }

        private readonly ICommand chooseSourceCommand;
        public ICommand ChooseSourceCommand
        {
            get { return this.chooseSourceCommand; }
        }
        private bool ChooseSourceCommand_CanExecute(object state)
        {
            return true;
        }
        private void ChooseSourceCommand_Executed(object state)
        {
            var ofg = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
            //ofg.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
            ofg.Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*";
            ofg.RestoreDirectory = true;
            ofg.CheckFileExists = false;
            ofg.CheckPathExists = true;
            ofg.Multiselect = false;

            Nullable<bool> result = ofg.ShowDialog();
            if (result != true) return;

            string filePath = ofg.FileName;
            string safeFilePath = ofg.SafeFileName;
            string fileDir = System.IO.Path.GetDirectoryName(filePath);
            string tableName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            //
            // Create a source table and load its structure into the schema
            //
            if (Table == null)
            {
                //Table = Schema.Space.CreateTable(tableName, Schema.Root);
            }
            //TableName = tableName;
            FilePath = filePath;

            UpdateColumnList(); // Read table structure from file and show

            FirePropertyNotifyChanged("");
        }

        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            if (string.IsNullOrWhiteSpace(TableName)) return false;

            return true;
        }
        private void OkCommand_Executed(object state)
        {
            DcSpace space = mainVM.Space;

            if (IsNew)
            {
                // Create a new table using parameters in the dialog
                TableCsv table = (TableCsv)space.CreateTable(DcSchemaKind.Csv, TableName, Schema.Root);

                table.GetData().WhereFormula = TableFormula;

                table.FilePath = FilePath;
                table.HasHeaderRecord = HasHeaderRecord;
                table.Delimiter = Delimiter;
                table.CultureInfo.NumberFormat.NumberDecimalSeparator = Decimal;

                // Load (read-only) column descriptions from CSV to schema
                //var columns = table.LoadSchema();
                //var columns = ((SchemaCsv)Schema).LoadSchema(table);

                Table = table;
            }
            else
            {
                TableCsv table = (TableCsv)Table;

                table.Name = TableName;
                table.GetData().WhereFormula = TableFormula;

                table.FilePath = FilePath;
                table.HasHeaderRecord = HasHeaderRecord;
                table.Delimiter = Delimiter;
                table.CultureInfo.NumberFormat.NumberDecimalSeparator = Decimal;

                foreach (DcColumn col in table.Columns.ToArray()) if (!col.IsSuper) space.DeleteColumn(col);

                // Load (read-only) column descriptions from CSV to schema
                //var columns = table.LoadSchema();
                //var columns = ((SchemaCsv)Schema).LoadSchema(table);
            }

            ((Com.Schema.Table)Table).NotifyPropertyChanged("");

            this.DialogResult = true;
        }

    }
}
