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

        protected ComSchema Schema { get { if (Element is ComSchema) return (ComSchema)Element; else if (Element is ComTable) return ((ComTable)Element).Schema; else if (Element is ComColumn) return ((ComColumn)Element).Input.Schema; else return null; } }
        protected ComTable Table { get { if (Element is ComSchema) return (ComTable)Element; else if (Element is ComTable) return ((ComTable)Element); else if (Element is ComColumn) return ((ComColumn)Element).Input; else return null; } }
        protected ComColumn Column { get { if (Element is ComSchema) return null; else if (Element is ComTable) return null; else if (Element is ComColumn) return (ComColumn)Element; else return null; } }

        public event PropertyChangedEventHandler PropertyChanged;

        public RenameBox(object element, string name)
        {
            Element = element;

            if (element is ComSchema)
            {
                IsSchema = true;
                OldName = Schema.Name;
            }
            else if(element is ComTable) 
            {
                IsTable = true;
                OldName = Table.Name;
            }
            else if(element is ComColumn) 
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
            if (Element is ComSchema)
            {
                Schema.RenameTable(Schema, NewName);
                Schema.Name = NewName;
                ((Set)Schema).NotifyPropertyChanged("");
            }
            else if (Element is ComTable)
            {
                Schema.RenameTable(Table, NewName);
                Table.Name = NewName;
                ((Set)Table).NotifyPropertyChanged("");
            }
            else if (Element is ComColumn)
            {
                Schema.RenameColumn(Column, NewName);
                Column.Name = NewName;
                ((Dim)Column).NotifyPropertyChanged("");
            }

            this.DialogResult = true;
        }

    }
}
