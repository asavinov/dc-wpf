using System;
using System.Collections.Generic;
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
    /// Interaction logic for TableBox.xaml
    /// </summary>
    public partial class TableBox : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyNotifyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
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

        // It is called when preparing this dialog for editing/adding a table or when context changes (not during the process)
        // Essentially, we do it when data context is set.
        private void initViewModel()
        {
            DcSpace space = mainVM.Space;

            if (IsNew)
            {
                TableName = "New Table";
                TableFormula = "";
            }
            else
            {
                TableName = Table.Name;
                TableFormula = Table.GetData().WhereFormula;
            }

            FirePropertyNotifyChanged("");
        }

        public TableBox(MainWindow mainVM)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            this.mainVM = mainVM;

            InitializeComponent();
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
                DcTable table = space.CreateTable(TableName, Schema.Root);
                table.GetData().WhereFormula = TableFormula;

                Table = table;
            }
            else
            {
                Table.Name = TableName;
                Table.GetData().WhereFormula = TableFormula;
            }

            ((Com.Schema.Table)Table).NotifyPropertyChanged("");

            this.DialogResult = true;
        }

    }
}
