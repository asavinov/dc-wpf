using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace coViz.Connections
{
    /// <summary>
    /// Interaction logic for SqlServerDialog.xaml
    /// See: ADO.NET http://support.microsoft.com/kb/310083
    /// See: http://stackoverflow.com/questions/567981/is-there-a-free-add-connection-or-sql-connection-dialog
    /// See: http://archive.msdn.microsoft.com/Connection
    /// See: http://www.codeproject.com/Articles/21186/SQL-Connection-Dialog
    /// </summary>
    public partial class SqlServerDialog : Window
    {
        public SqlServerDialog()
        {
            InitializeComponent();
        }
    }
}
