using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Samm
{
    /// <summary>
    /// Interaction logic for AddTableBox.xaml
    /// </summary>
    public partial class ImportTableBox : Window
    {
        public ObservableCollection<Com.Model.Expression> ExpressionModel { get; set; }

        public ImportTableBox()
        {
            ExpressionModel = new ObservableCollection<Com.Model.Expression>();

            InitializeComponent();
        }
    }
}
