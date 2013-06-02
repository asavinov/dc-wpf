﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

using Com.Model;

namespace Samm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Set> DsModel { get; set; }
        public ObservableCollection<string> StringList { get; set; }

        public MainWindow()
        {
            DsModel = new ObservableCollection<Set>();

            SetRoot root = new  SetRoot("My Data Source");

            DsModel.Add(root);

            Set departments = new Set("Departments");
            departments.SuperDim = new DimSuper("super", departments, root);
            departments.AddGreaterDim(root.GetPrimitiveSubset("String").CreateDefaultLesserDimension("name", departments));
            departments.GetGreaterDim("name").IsIdentity = true;
            departments.AddGreaterDim(root.GetPrimitiveSubset("String").CreateDefaultLesserDimension("location", departments));

            Set employees = new Set("Employees");
            employees.SuperDim = new DimSuper("super", employees, root);
            employees.AddGreaterDim(root.GetPrimitiveSubset("String").CreateDefaultLesserDimension("name", employees));
            employees.GetGreaterDim("name").IsIdentity = true;
            employees.AddGreaterDim(root.GetPrimitiveSubset("Double").CreateDefaultLesserDimension("age", employees));
            employees.AddGreaterDim(root.GetPrimitiveSubset("Double").CreateDefaultLesserDimension("salary", employees));
            employees.AddGreaterDim(departments.CreateDefaultLesserDimension("dept", employees));

            Set managers = new Set("Managers");
            managers.SuperDim = new DimSuper("super", managers, employees);
            managers.AddGreaterDim(root.GetPrimitiveSubset("String").CreateDefaultLesserDimension("title", managers));
            managers.AddGreaterDim(root.GetPrimitiveSubset("Boolean").CreateDefaultLesserDimension("is project manager", managers));

            // Criteria to the tree view: 
            // - we might need either code for visualization or dedicated class (referenced in XAML)
            // - Conditional visualization (item rendering):
            //   - Primitive concepts either not visualized or visualized in a separate folder (also other kinds of folders either with special class or with special properties)
            //   - Dimension structure visualized (so we need to anayze the identity tree) --> Use multibinding
            //   - Different types of dimensions (identity/entiy/greater/lesser etc.) either hide or in separate folders 
            //   - Item visualization structure can depend on either properties or class. How to implement it?
            //   - Using flags/properties for choosing what to visualize: visualize also lesser (incoming) dimensions, visualize also dimension expansion (expand dimension range set), show only identity dims or only entity dims etc.
            //   - Alligning various elements of items across the whole tree like in a table, say, names could be alligned (although it might not be possible for deep children)
            //   - TreeView header (it is probably needed only if we have allignment).
            //   - Custom sorting. One general way is to implement binding properties specially for visualization purposes which will return what is needed for the tree view (and other controls) taking into account flags, filters, properties, dedicated folders for special elements etc.
            // - The root (SetRoot) is either shown or hidden so that its direct child sets are listed in tree view at the very first level
            // - Touchable actions: context menu, drag-n-drop, DnD icon changing its appearance depending on the drop area, scrolling etc.
            // - Visualizations: animations during actions (touching, DnD), pitching, external events like process updates or property changes etc. 
            // - Selection and highlighting: multi-selection (including via touch), conditional selection when not all combinations are possible (with warning animation or other visualization). 
            // - Getting unique representation for a (selected or arbitrary) item visualized by a tree item (dim id, set id etc. including root)

//            this.DataContext = this;
            InitializeComponent();
        }

        private void accessDataSourceMenu_Click(object sender, RoutedEventArgs e)
        {
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
            //
            // http://archive.msdn.microsoft.com/Connection
            // http://www.mztools.com/articles/2007/mz2007011.aspx
            // authorized for redistribution since Feb 2010: http://connect.microsoft.com/VisualStudio/feedback/details/423104/redistributable-microsoft-data-connectionui-dll-and-microsoft-data-connectionui-dialog-dll
            //
            // Assemblies: Microsoft.Data.ConnectionUI.dll, Microsoft.Data.ConnectionUI.Dialog.dll
            DataConnectionDialog dcd = new DataConnectionDialog();
            DataConnectionConfiguration dcs = new DataConnectionConfiguration(null);
            dcs.LoadConfiguration(dcd);

            if (DataConnectionDialog.Show(dcd) != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string connectionString = dcd.ConnectionString; // Example: "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\Users\\savinov\\git\\samm\\Northwind.accdb"
            using (/*SqlConnection*/ System.Data.OleDb.OleDbConnection connection = new System.Data.OleDb.OleDbConnection(connectionString))
            {
                // For oledb: http://www.c-sharpcorner.com/UploadFile/Suprotim/OledbSchema09032005054630AM/OledbSchema.aspx
                    

                connection.Open();

                // Read a table with schema information
                DataTable tables = connection.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

                // Read table info
                foreach (DataRow row in tables.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString();
                    Console.WriteLine(tableName);

                    //
                    // TODO: Create a COM concept for this table
                    //

                    // Read column info
                    DataTable columns = connection.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Columns, new object[] { null, null, tableName, null });
                    foreach (DataRow col in columns.Rows)
                    {
                        string columnName = col["COLUMN_NAME"].ToString();
                        Console.WriteLine("   "+columnName);

                        // Find column type and define the dimension type
                        System.Data.OleDb.OleDbType columnType = (System.Data.OleDb.OleDbType) col["DATA_TYPE"];
                        switch (columnType)
                        {
                            case System.Data.OleDb.OleDbType.Double:
                                break;
                            case System.Data.OleDb.OleDbType.Integer: 
                                break;
                            case System.Data.OleDb.OleDbType.Char:
                            case System.Data.OleDb.OleDbType.VarChar:
                            case System.Data.OleDb.OleDbType.VarWChar:
                            case System.Data.OleDb.OleDbType.WChar:
                                break;
                            default:
                                // All the rest of types or error in the case we have enumerated all of them
                                break;
                        }

                        //
                        // TODO: Create a new COM dimension of this type
                        //
                    }

                    // Read and grouping PK attributes: http://msdn.microsoft.com/en-us/library/system.data.oledb.oledbschemaguid.primary_keys.aspx
                    DataTable pks = connection.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Primary_Keys, new object[] { null, null, tableName });
                    Dictionary<string, List<string>> pkNames = new Dictionary<string, List<string>>();
                    foreach (DataRow pk in pks.Rows)
                    {
                        string Name = (string)pk["PK_NAME"];
                        string PrimaryField = (string)pk["COLUMN_NAME"];

                        // One PK contains several columns. And we also assume that there can be many PKs.
                        if (pkNames.ContainsKey(Name))
                        {
                            pkNames[Name].Add(PrimaryField);
                        }
                        else
                        {
                            pkNames.Add(Name, new List<string>() { PrimaryField } );
                        }
                    }
                    foreach (var entry in pkNames)
                    {
                        Console.WriteLine("   " + tableName + " PK {0}: {1}", entry.Key, entry.Value);
                    }

                    // Read and grouping FK attributes: http://msdn.microsoft.com/en-us/library/system.data.oledb.oledbschemaguid.foreign_keys.aspx
                    DataTable fks = connection.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Foreign_Keys, new object[] { null, null, null, null, null, tableName });
                    Dictionary<string, List<Tuple<string, string, string>>> fkNames = new Dictionary<string, List<Tuple<string, string, string>>>();
                    foreach (DataRow fk in fks.Rows)
                    {
                        // Columns belonging to one FK should by identifed by one FK or PK name
                        // We need to create a list of complex FKs rather than a flat list of FK columns
                        // The structure of a complex FK is defined either explicitly by the columns or (later, derived) by referencing the corresponding PK
                        // Our task is to collect all schema information for creationg a new COM schema
                        string Name = (string)fk["FK_NAME"];
                        string PrimaryTable = (string)fk["PK_TABLE_NAME"];
                        string PrimaryField = (string)fk["PK_COLUMN_NAME"];
                        string PrimaryIndex = (string)fk["PK_NAME"];
                        string ForeignTable = (string)fk["FK_TABLE_NAME"];
                        string ForeignField = (string)fk["FK_COLUMN_NAME"];
                        string OnUpdate = (string)fk["UPDATE_RULE"];
                        string OnDelete = (string)fk["DELETE_RULE"];

                        if (fkNames.ContainsKey(Name))
                        {
                            fkNames[Name].Add(Tuple.Create(ForeignField, PrimaryTable, PrimaryField));
                        }
                        else
                        {
                            fkNames.Add(Name, new List<Tuple<string, string, string>>() { Tuple.Create(ForeignField, PrimaryTable, PrimaryField) });
                        }
                    }
                    foreach (var entry in fkNames)
                    {
                        Console.WriteLine("   " + tableName + " FK {0}: {1}", entry.Key, entry.Value);
                    }

                }
            }

            dcs.SaveConfiguration(dcd);
        }

        private void sqlServerDataSourceMenu_Click(object sender, RoutedEventArgs e)
        {

/*
                    // Read schema: http://www.simple-talk.com/dotnet/.net-framework/schema-and-metadata-retrieval-using-ado.net/

//                    DataTable schema = connection.GetSchema();
//                    DataTable schema = connection.GetSchema("Databases", new string[] { "Northwind" });
//                    DataTable schema = connection.GetSchema("Databases");
//                    DataTable schema = connection.GetSchema(System.Data.SqlClient.SqlClientMetaDataCollectionNames.Databases);

                    using (DataTableReader tableReader = schema.CreateDataReader())
                    {
                        while (tableReader.Read())
                        {
                            Console.WriteLine(tableReader.ToString());
                        }
                    }

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
}
