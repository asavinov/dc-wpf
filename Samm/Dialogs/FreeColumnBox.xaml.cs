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
    /// Interaction logic for ProductTableBox.xaml
    /// </summary>
    public partial class FreeColumnBox : Window, INotifyPropertyChanged
    {
        bool IsNew { get; set; }

        public DcSchema Schema { get; set; }

        public DcTable Table { get; set; }

        public string TableName { get; set; }

        public ObservableCollection<GreaterTableEntry> Entries { get; set; }
        public int SelectedCount { get { return Entries.Count(x => x.IsSelected); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshAll()
        {
            this.GetBindingExpression(FreeColumnBox.DataContextProperty).UpdateTarget(); // Does not work

            greaterTables.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();

            //newTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }

        public FreeColumnBox(DcSchema schema, DcTable table)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            //
            // Options and regime of the dialog
            //
            if (table.SuperColumn != null) IsNew = false;
            else IsNew = true;

            Schema = schema;
            Table = table;
            TableName = Table.Name;
            
            Entries = new ObservableCollection<GreaterTableEntry>();
            
            CreateEntries();

            InitializeComponent();

            RefreshAll();
        }

        public void CreateEntries()
        {
            // For each table, create an entry object
            // An entry is a possible greater table with the corresponding greater column
            // Some of them are selected if they are already selected in the existing table

            // PROBLEM: a table can be included more than once using different dimension names
            // SOLUTION: we need a different mechanism for adding greater dimensions (Add...) and then selecting one table
            // Essentially, this means adding free columns one-by-one
            // SOLUTION: Interpret this dialog not as a list of all existing column with selection of additional columns.
            // Rather, it is only Add operation for adding one or more columns (but not deletion).
            // In other words, it is a more general version of adding only one column. 
            // If we want to remove a (free) column the just Delete it as an object.

            foreach (DcTable gTab in Schema.Root.SubTables)
            {
                if (gTab == Table) continue; // We cannot reference this same table
                // TODO: Also exclude tables which produce cycles

                GreaterTableEntry entry = new GreaterTableEntry(gTab);
                Entries.Add(entry);
            }
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
            // Save name
            if (string.IsNullOrWhiteSpace(TableName)) return;

            Table.Name = TableName;

            Table.Definition.DefinitionType = DcTableDefinitionType.PRODUCT;

            // Save table
            if (IsNew)
            {
                Schema.AddTable(Table, null, null);
            }

            // Create free columns for the selected entries
            foreach (var entry in Entries)
            {
                if (!entry.IsSelected) continue;

                DcTable gSet = entry.Table;
                string columnName = entry.ColumnName;
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    columnName = gSet.Name;
                }

                // TODO: Check if such a column already exists (name)

                DcColumn gDim = Schema.CreateColumn(columnName, Table, gSet, true);
                gDim.Add();
            }

            this.DialogResult = true;
        }
    }

    /// <summary>
    /// It corresponds to one greater table shown in the list.
    /// </summary>
    public class GreaterTableEntry
    {
        public DcTable Table { get; set; }

        public string ColumnName { get; set; }

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

        public GreaterTableEntry(DcTable table)
        {
            Table = table;
            ColumnName = table.Name;

            IsSelected = false;
        }

    }

}
