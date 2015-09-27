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
    /// Interaction logic for RenameBox.xaml
    /// </summary>
    public partial class RenameBox : Window, INotifyPropertyChanged
    {
        public object Element { get; set; }
        public string OldName { get; set; }
        public string NewName { get; set; }

        public bool IsSchema = false;
        public bool IsTable = false;
        public bool IsColumn = false;

        protected DcSchema Schema { get { if (Element is DcSchema) return (DcSchema)Element; else if (Element is DcTable) return ((DcTable)Element).Schema; else if (Element is DcColumn) return ((DcColumn)Element).Input.Schema; else return null; } }
        protected DcTable Table { get { if (Element is DcSchema) return (DcTable)Element; else if (Element is DcTable) return ((DcTable)Element); else if (Element is DcColumn) return ((DcColumn)Element).Input; else return null; } }
        protected DcColumn Column { get { if (Element is DcSchema) return null; else if (Element is DcTable) return null; else if (Element is DcColumn) return (DcColumn)Element; else return null; } }

        public event PropertyChangedEventHandler PropertyChanged;

        public RenameBox(object element, string name)
        {
            Element = element;

            if (element is DcSchema)
            {
                IsSchema = true;
                OldName = Schema.Name;
            }
            else if(element is DcTable) 
            {
                IsTable = true;
                OldName = Table.Name;
            }
            else if(element is DcColumn) 
            {
                IsColumn = true;
                OldName = Column.Name;
            }

            if (string.IsNullOrEmpty(name)) // Initialize by original (old) name
            {
                if (IsSchema || IsTable) NewName = Table.Name;
                else if (IsColumn) NewName = Column.Name;
            }
            else
            {
                NewName = name;
            }

            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            // Really rename the element and propagate this change through all objects where this element name is used
            if (Element is DcSchema)
            {
                Schema.RenameTable(Schema, NewName);
                Schema.Name = NewName;
                ((Set)Schema).NotifyPropertyChanged("");
            }
            else if (Element is DcTable)
            {
                Schema.RenameTable(Table, NewName);
                Table.Name = NewName;
                ((Set)Table).NotifyPropertyChanged("");
            }
            else if (Element is DcColumn)
            {
                Schema.RenameColumn(Column, NewName);
                Column.Name = NewName;
                ((Dim)Column).NotifyPropertyChanged("");
            }

            this.DialogResult = true;
        }

    }
}
