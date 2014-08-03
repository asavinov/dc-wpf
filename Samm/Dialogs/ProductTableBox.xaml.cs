using System;
using System.Collections.Generic;
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
    /// Interaction logic for ProductTableBox.xaml
    /// </summary>
    public partial class ProductTableBox : Window
    {
        public List<CsTable> GreaterTables { get; set; }

        public ProductTableBox()
        {
            GreaterTables = new List<CsTable>();

            InitializeComponent();
        }

        public void RefreshAll()
        {
            this.GetBindingExpression(ProductTableBox.DataContextProperty).UpdateTarget(); // Does not work

            greaterTables.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();

            //newTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
