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

using Com.Model;

namespace Samm.Dialogs
{
    /// <summary>
    /// Interaction logic for TableCsvBox.xaml
    /// </summary>
    public partial class TableCsvBox : Window, INotifyPropertyChanged
    {
        bool IsNew { get; set; }

        public DcSchema Schema { get; set; }

        public DcTable Table { get; set; }
        public string TableName { get; set; }

        public bool HasHeaderRecord { get; set; }
        public string Delimiter { get; set; }
        public string Decimal { get; set; }

        public ObservableCollection<DcColumn> TableColumns { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshAll()
        {
            this.GetBindingExpression(ImportMappingBox.DataContextProperty).UpdateTarget();

            tableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }

        public TableCsvBox(DcSchema schema, DcTable table)
        {
            this.chooseSourceCommand = new DelegateCommand(this.ChooseSourceCommand_Executed, this.ChooseSourceCommand_CanExecute);
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            if (table == null)
            {
                IsNew = true;
            }
            else
            {
                IsNew = false;
            }

            Schema = schema;
            Table = table;

            TableColumns = new ObservableCollection<DcColumn>();

            if (IsNew)
            {
                TableName = "";
                Table = Schema.CreateTable(TableName);
            }

            //
            // Initialize
            //

            TableName = Table.Name;

            HasHeaderRecord = ((SetCsv)Table).HasHeaderRecord;
            Delimiter = ((SetCsv)Table).Delimiter;
            Decimal = ((SetCsv)Table).CultureInfo.NumberFormat.NumberDecimalSeparator;

            foreach (DcColumn column in Table.Columns)
            {
                TableColumns.Add(column);
            }

            InitializeComponent();
        }

        private void UpdateColumnList() // Read table structure from file and show (table itself is not changed)
        {
            // Display new schema and maybe sample data
            TableColumns.Clear();

            if (!File.Exists(((SetCsv)Table).FilePath))
            {
                return;
            }

            List<DcColumn> columns = ((SchemaCsv)Schema).LoadSchema((SetCsv)Table);

            foreach (DcColumn column in columns)
            {
                TableColumns.Add(column);
            }
        }

        private void Delimiter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Table == null)
            {
                return;
            }

            ((SetCsv)Table).HasHeaderRecord = (bool)HasHeaderRecord;
            ((SetCsv)Table).Delimiter = Delimiter;
            ((SetCsv)Table).CultureInfo.NumberFormat.NumberDecimalSeparator = (string)Decimal;

            UpdateColumnList();

            RefreshAll();
        }

        private void Decimal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Table == null)
            {
                return;
            }

            ((SetCsv)Table).HasHeaderRecord = (bool)HasHeaderRecord;
            ((SetCsv)Table).Delimiter = Delimiter;
            ((SetCsv)Table).CultureInfo.NumberFormat.NumberDecimalSeparator = (string)Decimal;

            //UpdateColumnList();

            RefreshAll();
        }

        private void Header_Changed(object sender, RoutedEventArgs e)
        {
            if (Table == null)
            {
                return;
            }

            ((SetCsv)Table).HasHeaderRecord = (bool)HasHeaderRecord;
            ((SetCsv)Table).Delimiter = Delimiter;
            ((SetCsv)Table).CultureInfo.NumberFormat.NumberDecimalSeparator = (string)Decimal;

            UpdateColumnList();

            RefreshAll();
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
                Table = Schema.CreateTable(tableName);
            }
            TableName = tableName;
            ((SetCsv)Table).FilePath = filePath;

            ((SetCsv)Table).HasHeaderRecord = (bool)HasHeaderRecord;
            ((SetCsv)Table).Delimiter = Delimiter;
            ((SetCsv)Table).CultureInfo.NumberFormat.NumberDecimalSeparator = (string)Decimal;

            UpdateColumnList(); // Read table structure from file and show
            
            RefreshAll();
        }
        
        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            return true;
        }
        private void OkCommand_Executed(object state)
        {
            if (IsNew)
            {
                if (Table == null)
                {
                    Table = Schema.CreateTable(TableName);
                }

                Table.Name = TableName;

                var columns = ((SchemaCsv)Schema).LoadSchema((SetCsv)Table);
                columns.ForEach(x => x.Add());
                
                Schema.AddTable(Table, null, null);
            }
            else
            {
                Table.Name = TableName;

                // In fact, we have to update columns (leave used, remove unused, and add new)
                foreach (DcColumn column in Table.Columns.ToArray())
                {
                    if (column.IsSuper) continue;
                    column.Remove();
                }

                var columns = ((SchemaCsv)Schema).LoadSchema((SetCsv)Table);
                columns.ForEach(x => x.Add());
            }

            this.DialogResult = true;
        }

    }
}
