using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Data.ConnectionUI;

namespace Samm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void sqlServerDataSourceMenu_Click(object sender, RoutedEventArgs e)
        {
            //
            // http://archive.msdn.microsoft.com/Connection
            // http://www.mztools.com/articles/2007/mz2007011.aspx
            // authorized for redistribution since Feb 2010: http://connect.microsoft.com/VisualStudio/feedback/details/423104/redistributable-microsoft-data-connectionui-dll-and-microsoft-data-connectionui-dialog-dll
            //
            // Assemblies: Microsoft.Data.ConnectionUI.dll, Microsoft.Data.ConnectionUI.Dialog.dll
            DataConnectionDialog dcd = new DataConnectionDialog();
            DataConnectionConfiguration dcs = new DataConnectionConfiguration(null);
            dcs.LoadConfiguration(dcd);

            if (DataConnectionDialog.Show(dcd) == System.Windows.Forms.DialogResult.OK)
            {
                // load tables
                using (/*SqlConnection*/ System.Data.OleDb.OleDbConnection connection = new System.Data.OleDb.OleDbConnection(dcd.ConnectionString))
                {
                    connection.Open();

                    // Read schema: http://www.simple-talk.com/dotnet/.net-framework/schema-and-metadata-retrieval-using-ado.net/
                    // For oledb: http://www.c-sharpcorner.com/UploadFile/Suprotim/OledbSchema09032005054630AM/OledbSchema.aspx
                    DataTable schema = connection.GetSchema();
//                    DataTable schema = connection.GetSchema("Databases", new string[] { "Northwind" });
                    using (DataTableReader tableReader = schema.CreateDataReader())
                    {
                        while (tableReader.Read())
                        {
                            Console.WriteLine(tableReader.ToString());
                        }
                    }
/*
                    SqlCommand cmd = new SqlCommand("SELECT * FROM sys.Tables", connection);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine(reader.HasRows);
                        }
                    }
*/
                }
            }

            dcs.SaveConfiguration(dcd);

/*
            //
            // OLEDB Connection string dialog: http://support.microsoft.com/default.aspx?scid=kb;EN-US;310083
            //
            // References (COM):
            // MSDASC: Microsoft OLEDB Service Component 1.0 Type Library
            MSDASC.DataLinks mydlg = new MSDASC.DataLinks();
            // ADODB: Microsoft ActiveX Data Objects 2.7
            ADODB._Connection ADOcon;
            //Cast the generic object that PromptNew returns to an ADODB._Connection.
            ADOcon = (ADODB._Connection) mydlg.PromptNew();
            ADOcon.Open("", "", "", 0);
            if (ADOcon.State == 1)
            {
                MessageBox.Show("Connection Opened");
                ADOcon.Close();
            }
            else
            {
                MessageBox.Show("Connection Failed");
            }
*/ 
/*
            //
            // Custom dialog
            //
            // Instantiate the dialog box
            Connections.SqlServerDialog dlg = new Connections.SqlServerDialog();
            // Configure the dialog box
            dlg.Owner = this;
            // Open the dialog box modally 
            dlg.ShowDialog();
*/
        }
    }
}
