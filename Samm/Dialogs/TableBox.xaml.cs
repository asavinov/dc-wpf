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
        bool IsNew { get; set; }

        public DcSchema Schema { get; set; }

        public DcTable Table { get; set; }
        public string TableName { get; set; }

        public string TableFormula { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public TableBox(DcSchema schema, DcTable table)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            Schema = schema;
            Table = table;

            if (table == null)
            {
                IsNew = true;
                TableName = "New Table";
                TableFormula = "";
            }
            else
            {
                IsNew = false;
                TableName = table.Name;
                TableFormula = table.Definition.WhereFormula;
            }

            Table = table;

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
            this.DialogResult = true;
        }

    }
}
