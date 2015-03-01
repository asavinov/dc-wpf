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

using Com.Model;

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

        public event PropertyChangedEventHandler PropertyChanged;

        public TableBox(DcSchema schema, DcTable table)
        {
            Schema = schema;

            if (table == null)
            {
                IsNew = true;
            }
            else
            {
                IsNew = false;
            }

            Table = table;

            if (Table == null)
            {
                TableName = "New Table";
            }
            else
            {
                TableName = Table.Name;
            }

            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsNew)
            {
                if (Table == null)
                {
                    Table = Schema.CreateTable(TableName);
                }

                Table.Name = TableName;
                Schema.AddTable(Table, null, null);
            }
            else
            {
                Table.Name = TableName;
                Schema.AddTable(Table, null, null);
            }

            this.DialogResult = true;
        }

    }
}
