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
    /// Interaction logic for ExtractTableBox.xaml
    /// </summary>
    public partial class ExtractTableBox : Window
    {
        public string NewColumnName { get; set; }

        public CsTable SourceTable { get; set; }

        public List<CsColumn> ProjectionDims { get; set; }

        public string NewTableName { get; set; }

        public ExtractTableBox()
        {
            ProjectionDims = new List<CsColumn>();

            InitializeComponent();
        }

        public void RefreshAll()
        {
            this.GetBindingExpression(ExtractTableBox.DataContextProperty).UpdateTarget(); // Does not work

            newColumnName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            projectionDims.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();

            newTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
