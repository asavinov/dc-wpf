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

namespace Samm
{
    /// <summary>
    /// Interaction logic for ExtractTableBox.xaml
    /// </summary>
    public partial class ExtractTableBox : Window
    {
        public Set ProjectedSet { get; set; }

        public string ExtractedDimName { get; set; }

        public List<Dim> ProjectionDims { get; set; }

        public string ExtractedSetName { get; set; }

        public ExtractTableBox()
        {
            ProjectionDims = new List<Dim>();

            InitializeComponent();
        }

        public void RefreshAll()
        {
            this.GetBindingExpression(ExtractTableBox.DataContextProperty).UpdateTarget(); // Does not work

            projectedSet.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            extractedDimName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            projectionDims.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();

            extractedSetName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
