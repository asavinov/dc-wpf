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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Samm
{
    /// <summary>
    /// Interaction logic for DimensionPathListControl.xaml
    /// 
    /// Interesting article on how to pass data into a user control using both DataContext and parameter binding: 
    /// http://www.codeproject.com/Articles/137288/WPF-Passing-Data-to-Sub-Views-via-DataContext-Caus
    /// </summary>
    public partial class DimensionPathListControl : UserControl
    {
        public DimensionPathListControl()
        {
            InitializeComponent();
        }
    }
}
