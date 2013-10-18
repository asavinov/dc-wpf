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
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
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
    public partial class MainWindow : RibbonWindow
    {
        public ObservableCollection<SetRoot> DsModel { get; set; }
        public ObservableCollection<SetRoot> MashupModel { get; set; }

        List<object> windows = new List<object>();

        public MainWindow()
        {
            DsModel = new ObservableCollection<SetRoot>();

            SetRoot root = new  SetRoot("My Data Source");

            DsModel.Add(root);

            Set departments = new Set("Departments");
            departments.SuperDim = new DimSuper("super", departments, root);
            departments.AddGreaterDim(root.GetPrimitiveSubset("String").CreateDefaultLesserDimension("name", departments));
            departments.GetGreaterDim("name").IsIdentity = true;
            departments.AddGreaterDim(root.GetPrimitiveSubset("String").CreateDefaultLesserDimension("location", departments));

            departments.Append();
            departments.SetValue("name", 0, "SALES");
            departments.SetValue("location", 0, "Dresden");
            departments.Append();
            departments.SetValue("name", 1, "HR");
            departments.SetValue("location", 1, "Walldorf");

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

            MashupModel = new ObservableCollection<SetRoot>();

            SetRoot muRoot = new SetRoot("My Mashup");

            MashupModel.Add(muRoot);


//            this.DataContext = this;
            InitializeComponent();
        }

        private void readOledbSchema(string connectionString)
        {
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
        }

        private void MashupView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ICommand cmd = this.Resources["OpenTableCommand"] as ICommand;

            if (cmd.CanExecute(null))
            {
                cmd.Execute(null);
            }
        }

        private void OpenTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var tv = (TreeView)e.Source;

            var item = tv.SelectedItem; // tv.SelectedValue

            if (item is Set)
            {
                Set set = (Set)item;
                lblWorkspace.Content = set.Name;

                GridPanel.Children.Clear();

                Label lbl = new Label();
                lbl.Content = "Content";
                // GridPanel.Children.Add(lbl);

                var gridView = new SetGridView(set);
                GridPanel.Children.Add(gridView.Grid);

            }
            else if (item is Dim)
            {
                Dim dim = (Dim)item;
                Set set = dim.LesserSet;
                lblWorkspace.Content = set.Name + " : " + dim.Name;

                GridPanel.Children.Clear();
            }

            e.Handled = true;
        }

        private void AccessDatasourceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
            ofd.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
            ofd.Filter = "Access Files (*.ACCDB)|*.ACCDB|All files (*.*)|*.*";
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() != true) return;

            string filePath = ofd.FileName;
            string safeFilePath = ofd.SafeFileName;

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath;

            // Initialize the data suorce
            if (DsModel == null) DsModel = new ObservableCollection<SetRoot>();
            else DsModel.Clear();

            SetRootOledb root = new SetRootOledb("My Data Source");

            root.ConnectionString = connectionString;

            root.Open();
            root.ImportSchema();

            DsModel.Add(root);
        }

        private void SqlserverDatasourceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
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

            //readOledbSchema(connectionString); //For testing purposes

            dcs.SaveConfiguration(dcd);

        }

        private void ImportTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetRoot mashup = MashupModel[0];

            var item = DsView.SelectedItem;

            if (item is Set && ((Set)item).Root == DsModel[0])
            {
                Set set = (Set)item;

                Mapper mapper = new Mapper();
                mapper.RecommendMappings(set, mashup, 1.0);

                SetMapping bestMapping = mapper.GetBestMapping(set);
                Com.Model.Expression expr = bestMapping.GetTargetExpression(); // Build a tuple tree with paths in leaves

                // Show dialog for editing import
                ImportTableBox dlg = new ImportTableBox(); // Instantiate the dialog box
                dlg.Owner = this;
                dlg.ExpressionModel.Add(expr);
                dlg.ShowDialog();

                Set targetSet = bestMapping.TargetSet;
                DimTree tree = bestMapping.GetTargetTree();
                tree.IncludeInSchema(mashup); // Include new elements in schema

                targetSet.ImportExpression = expr;
                string importDimName = set.Name; // The same as the source (imported) set name
                DimImport importDim = new DimImport(importDimName, targetSet, set);
                importDim.Add();

                targetSet.Populate();

                // HACK: refresh the view
                mashup = MashupModel[0];
                MashupModel.RemoveAt(0);
                MashupModel.Add(mashup);
            }

            e.Handled = true;
        }

        private void FilteredTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = MashupView.SelectedItem;

            if (item == null) return;

            Set srcSet = null;
            if (item is Set)
            {
                srcSet = (Set)item;
            }
            else if (item is Dim)
            {
                srcSet = ((Dim)item).LesserSet;
            }

            //
            // Create new subset
            //
            Set dstSet = new Set("My Table");
            srcSet.AddSubset(dstSet);

            //
            // Show recommendations and let the user choose one of them
            //
            FilteredTableBox dlg = new FilteredTableBox();
            dlg.Owner = this;
            dlg.SourceTable = srcSet;
            dlg.FilteredTable = dstSet;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.ExpressionModel == null && dlg.ExpressionModel.Count == 0)
                return;

            Com.Model.Expression expr = dlg.ExpressionModel[0];

            // Populate new set
            dstSet.WhereExpression = expr;
            dstSet.Populate();

            // HACK: refresh the view
            SetRoot mashup = MashupModel[0];
            MashupModel.RemoveAt(0);
            MashupModel.Add(mashup);
        }

        private void AddAggregationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = MashupView.SelectedItem;

            if(item == null) return;

            Set srcSet = null;
            Set dstSet = null;
            Dim dstDim = null; // mashup.FindSubset("Employees");
            if (item is Set)
            {
                dstSet = (Set)item;
            }
            else if (item is Dim)
            {
                dstDim = (Dim)item;
                dstSet = dstDim.LesserSet;
            }
            srcSet = dstSet.Root.FindSubset("Customers"); // TODO: Now the source set is fixed. We need a mechanism for choosing a source set, for example, by DnD. 

            RecommendedAggregations recoms = new RecommendedAggregations();
            recoms.SourceSet = srcSet;
            recoms.TargetSet = dstSet;
            recoms.FactSet = null; // Any

            recoms.Recommend();

            if (dstDim != null) // Try to set the current measure to the specified dimension
            {
                recoms.MeasureDimensions.SelectedObject = dstDim; 
            }

            //
            // Show recommendations and let the user choose one of them
            //
            AggregationBox dlg = new AggregationBox();
            dlg.Owner = this;
            dlg.Recommendations = recoms;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (recoms.IsValidExpression() != null) return;

            //
            // Create new derived dimension
            // Example: (Customers) <- (Orders) <- (Order Details) -> (Products) -> List Price
            //
            Dim aggregDim = (Dim)recoms.MeasureDimensions.SelectedObject;
            string derivedDimName = dlg.SourceColumn.Text;
            Com.Model.Expression aggreExpr = recoms.GetExpression();

            Dim derivedDim = aggregDim.GreaterSet.CreateDefaultLesserDimension(derivedDimName, srcSet);
            derivedDim.SelectExpression = aggreExpr;
            srcSet.AddGreaterDim(derivedDim);

            // Update new derived dimension
            derivedDim.Populate(); // Call SelectExpression.Evaluate(EvaluationMode.UPDATE);
        }

        private void AddCalculatedCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = MashupView.SelectedItem;

            if (item == null) return;

            Set srcSet = null;
            if (item is Set)
            {
                srcSet = (Set)item;
            }
            else if (item is Dim)
            {
                srcSet = ((Dim)item).LesserSet;
            }

            //
            // Show recommendations and let the user choose one of them
            //
            ArithmeticBox dlg = new ArithmeticBox();
            dlg.Owner = this;
            dlg.SourceTable = srcSet;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.ExpressionModel == null && dlg.ExpressionModel.Count == 0)
                return;

            Com.Model.Expression expr = expr = dlg.ExpressionModel[0];

            //
            // Create new derived dimension
            // Example: (Customers) <- (Orders) <- (Order Details) -> (Products) -> List Price
            //
            string derivedDimName = dlg.sourceColumn.Text;

            Set dstSet = expr.OutputSet;
            Dim derivedDim = dstSet.CreateDefaultLesserDimension(derivedDimName, srcSet);
            derivedDim.SelectExpression = expr;
            srcSet.AddGreaterDim(derivedDim);

            // Update new derived dimension
            derivedDim.Populate(); // Call SelectExpression.Evaluate(EvaluationMode.UPDATE);

            // HACK: refresh the view
            SetRoot mashup = MashupModel[0];
            MashupModel.RemoveAt(0);
            MashupModel.Add(mashup);
        }

        private void ChangeRangeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = MashupView.SelectedItem;

            if (item == null) return;

            Set srcSet = null;
            Dim srcDim = null;
            if (item is Set)
            {
                srcSet = (Set)item;
            }
            else if (item is Dim)
            {
                srcDim = (Dim)item;
                srcSet = srcDim.LesserSet;
            }

            Set dstSet = srcSet.Root.FindSubset("Customers"); // TODO: It is for test purposes. We need a new parameter with the desired target table

            //
            // Parameterize the matching model
            //
            SetMapping mapping = new SetMapping(srcSet, dstSet);

            // Source MatchDimTree
            MatchTree sourceTree = new MatchTree();
            sourceTree.Mapping = mapping;
            MatchTreeNode sourceChild = new MatchTreeNode();
            sourceChild.Dim = srcDim;
            sourceTree.AddChild(sourceChild);
            sourceTree.ExpandTree();

            // Target DimTree
            MatchTree targetTree = new MatchTree();
            targetTree.Mapping = mapping;
            MatchTreeNode targetChild = new MatchTreeNode();
            targetChild.Set = dstSet;
            targetTree.AddChild(targetChild);
            targetTree.ExpandTree();

            //
            // Show recommendations and let the user choose one of them
            //
            ChangeRangeBox dlg = new ChangeRangeBox();
            dlg.Owner = this;
            dlg.MatchTreeModel = sourceTree;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            //
            // Really changing the range
            //

            // Create import (conversion) expression 

            // Import data from old to new dimension with conversion 
            // Importing is performed in the loop on source elements by finding root element in another tree. During propagation primitive values (tree leaves) intermediate sets are appended if necessary
            // We need to formally create an expression from two dimension trees with matches. 
            // This expression tree is then used by the (standard) population procedure which iterates through one set, assignes leaf values, propagates and appends new elements in the target sets and intermediate sets.

            // delete old dimension

            // Update existing (lesser) import expressions so that next time they are used the data is imported into the new dimension rather than old one (which is deleted)

        }

        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AboutBox dlg = new AboutBox(); // Instantiate the dialog box
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 
        }

    }
}
