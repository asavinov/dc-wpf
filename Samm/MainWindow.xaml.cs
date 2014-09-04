using System;
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
using Samm.Dialogs;

namespace Samm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        //
        // Data sources
        //
        public ObservableCollection<ComSchema> RemoteSources { get; set; }

        //
        // Mashups (only one is used)
        //
        public ObservableCollection<ComSchema> Mashups { get; set; }
        public ComSchema MashupTop { get { return Mashups.Count != 0 ? Mashups[0] : null; } }
        public ComTable MashupRoot { get { return Mashups.Count != 0 ? Mashups[0].Root : null; } }

        public ObservableCollection<SubsetTree> MashupsModel { get; set; } // What is shown in SubsetTree for mashups
        public SubsetTree MashupModelRoot { get { return MashupsModel.Count != 0 ? (SubsetTree)MashupsModel[0] : null; } }

        public bool IsInMashups(ComTable set) // Determine if the specified set belongs to some mashup
        {
            if (set == null || Mashups == null) return false;
            foreach (ComSchema t in Mashups) { if (set.Top == t) return true; }
            return false;
        }
        public bool IsInMashups(ComColumn dim) // Determine if the specified dimension belongs to some mashup
        {
            if (dim == null || Mashups == null) return false;
            if (IsInMashups(dim.LesserSet) && IsInMashups(dim.GreaterSet)) return true;
            return false;
        }

        //
        // Selection state
        //
        public SubsetTree SelectedItem 
        { 
            get { if (MashupsView == null || MashupsView.SubsetTree == null) return null; return (SubsetTree)MashupsView.SubsetTree.SelectedItem; }
        }
        public ComTable SelectedRoot
        {
            get 
            { 
                SubsetTree item = SelectedItem; 
                if (item == null) return null;
                if (item.IsSubsetNode && item.LesserSet.IsPrimitive && item.LesserSet.Name == "Root") return (ComTable)item.LesserSet; 
                return null;
            }
        }
        public ComTable SelectedTable 
        {
            get
            {
                SubsetTree item = SelectedItem;
                if (item == null) return null;
                if (item.IsSubsetNode)
                {
                    if (item.LesserSet.IsPrimitive) return null; 
                    else return item.LesserSet;
                }
                return null;
            }
            set { if (MashupsView == null || MashupsView.SubsetTree == null) return; MashupsView.Select(value); } 
        }
        public ComColumn SelectedColumn 
        { 
            get { SubsetTree item = SelectedItem; if (item == null) return null; if (item.IsDimensionNode) return item.Dim; return null; }
            set { if (MashupsView == null || MashupsView.SubsetTree == null) return; MashupsView.Select(value); }
        }

        //
        // Operations and behavior
        //
        public DragDropHelper DragDropHelper { get; protected set; }

        public MainWindow()
        {
            //
            // Initialize data sources
            //
            RemoteSources = new ObservableCollection<ComSchema>();
            SetTopCsv csvSchema = new SetTopCsv("My Files");
            ConnectionCsv conn = new ConnectionCsv();
            csvSchema.connection = conn;
            
            RemoteSources.Add(csvSchema);

            //
            // Initialize mashups (one empty mashup)
            //
            Mashups = new ObservableCollection<ComSchema>();
            MashupsModel = new ObservableCollection<SubsetTree>();

            ComSchema mashupTop = new SetTop("New Mashup");
            //mashupTop = CreateSampleSchema();
            Mashups.Add(mashupTop);

            SubsetTree mashupModel = new SubsetTree(mashupTop.Root.SuperDim);
            mashupModel.ExpandTree();
            MashupsModel.Add(mashupModel);

            DragDropHelper = new DragDropHelper();

            //this.DataContext = this;
            InitializeComponent();
        }

        public ComSchema CreateSampleSchema()
        {
            ComSchema ds = new SetTop("Sample Mashup");
            ComColumn d1, d2, d3, d4;

            ComTable departments = ds.CreateTable("Departments");
            ds.AddTable(departments, null, null);

            d1 = ds.CreateColumn("name", departments, ds.GetPrimitive("String"), true);
            d1.Add();
            d2 = ds.CreateColumn("location", departments, ds.GetPrimitive("String"), false);
            d2.Add();

            departments.Data.Append(new ComColumn[] { d1, d2 }, new object[] { "SALES", "Dresden" });
            departments.Data.Append(new ComColumn[] { d1, d2 }, new object[] { "HR", "Walldorf" });

            ComTable employees = ds.CreateTable("Employees");
            ds.AddTable(employees, null, null);

            d1 = ds.CreateColumn("name", employees, ds.GetPrimitive("String"), true);
            d1.Add();
            d2 = ds.CreateColumn("age", employees, ds.GetPrimitive("Double"), false);
            d2.Add();
            d3 = ds.CreateColumn("salary", employees, ds.GetPrimitive("Double"), false);
            d3.Add();
            d4 = ds.CreateColumn("dept", employees, departments, false);
            d4.Add();

            ComTable managers = ds.CreateTable("Managers");
            ds.AddTable(managers, employees, null);

            d1 = ds.CreateColumn("title", managers, ds.GetPrimitive("String"), false);
            d1.Add();
            d2 = ds.CreateColumn("is project manager", managers, ds.GetPrimitive("Boolean"), false);
            d2.Add();

            return ds;
        }

        # region Command_Executed (call backs from Commands)

        private void TextDatasourceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_CsvDatasource();
            e.Handled = true;
        }

        private void AccessDatasourceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_AccessDatasource();
            e.Handled = true;
        }

        private void SqlserverDatasourceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_SqlserverDatasource();
            e.Handled = true;
        }

        private void OpenTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = false;
            else e.CanExecute = false;
        }
        private void OpenTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;
            Operation_OpenTable(SelectedTable);
            e.Handled = true;
        }

        private void CloseViewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (GridPanel != null && GridPanel.Content != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void CloseViewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            lblWorkspace.Content = "DATA";

            GridPanel.Content = null;

            e.Handled = true;
        }

        private void UpdateElementCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = true;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void UpdateElementCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedRoot != null) // Update all
            {
                // Use dependency graph to update elements starting from independent and ending with dependent
            }
            else if (SelectedTable != null) // Update table
            {
                SelectedTable.Definition.Populate();
            }
            else if (SelectedColumn != null) // Update column
            {
                SelectedColumn.Definition.Evaluate();
            }
        }

        private void RenameElementCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedItem != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void RenameElementCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            object element = null;
            if (SelectedRoot != null) // Rename schema
            {
                element = SelectedRoot.Top;
            }
            else if (SelectedTable != null) // Rename table
            {
                element = SelectedTable;
            }
            else if (SelectedColumn != null) // Rename column
            {
                element = SelectedColumn;
            }

            RenameBox dlg = new RenameBox(element, null);
            dlg.Owner = this;
            dlg.ShowDialog();

            if (dlg.DialogResult == false) return; // Cancel

            MashupModelRoot.NotifyAllOnPropertyChanged(""); // Notify visual components about changes in this column
        }

        private void FilteredTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;
            e.Handled = true;
        }

        private void ProductTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = false;
            else e.CanExecute = false;
        }
        private void ProductTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_ProductTable(SelectedTable);
            e.Handled = true;
        }

        private void ExtractTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void ExtractTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable != null) 
                Wizard_ExtractTable(SelectedTable);
            else if (SelectedColumn != null && SelectedColumn.LesserSet != null)
                Wizard_ExtractTable(SelectedColumn.LesserSet);
            e.Handled = true;
        }

        private void EditTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = false;
            else e.CanExecute = false;
        }
        private void EditTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ComTable table = SelectedTable;
            if (table != null)
            {
                Wizard_EditTable(table);
            }
            e.Handled = true;
        }

        private void DeleteTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = false;
            else e.CanExecute = false;
        }
        private void DeleteTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ComTable set = null;
            if (SelectedTable != null)
                set = SelectedTable;
            else if (SelectedColumn != null && SelectedColumn.LesserSet != null)
                set = SelectedColumn.LesserSet;
            else return;

            ComSchema schema = set.Top;

            // 
            // Delete tables generated from this table (alternatively, leave them but with empty definition)
            //
            var paths = new PathEnumerator(new List<ComTable>(new ComTable[] { set }), new List<ComTable>(), false, DimensionType.GENERATING);
            foreach (var path in paths)
            {
                for (int i = path.Path.Count - 1; i >= 0; i--)
                {
                    schema.DeleteTable(path.Path[i].GreaterSet); // Delete (indirectly) generated table
                }
            }

            // Remove all connections of this set with the schema by deleting all its dimensions
            schema.DeleteTable(set);

            e.Handled = true;
        }

        private void AddArithmeticCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = false;
            else e.CanExecute = false;
        }
        private void AddArithmeticCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_AddArithmetic(SelectedTable);
            e.Handled = true;
        }

        private void AddLinkCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = false;
            else e.CanExecute = false;
        }
        private void AddLinkCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_AddLink(SelectedTable, null);
        }

        private void AddAggregationCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = false;
            else e.CanExecute = false;
        }
        private void AddAggregationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ComColumn selDim = SelectedColumn;
            ComTable srcSet = SelectedTable;
            if (srcSet == null && selDim != null)
            {
                srcSet = selDim.LesserSet;
            }

            Wizard_AddAggregation(srcSet, null);
            e.Handled = true;
        }

        private void EditColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = false;
            else if (SelectedColumn != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void EditColumnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ComColumn selDim = SelectedColumn;
            if (selDim != null)
            {
                Wizard_EditColumn(selDim);
            }
            e.Handled = true;
        }

        private void DeleteColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = false;
            else if (SelectedColumn != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void DeleteColumnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ComColumn selDim = SelectedColumn;
            if (selDim == null) return;

            ComSchema schema = selDim.LesserSet.Top;

            // 
            // Delete related columns/tables
            //
            if (selDim.Definition.IsGenerating) // Delete all tables that are directly or indirectly generated by this column
            {
                ComTable gTab = selDim.GreaterSet;
                var paths = new PathEnumerator(new List<ComTable>( new ComTable[] { gTab } ), new List<ComTable>(), false, DimensionType.GENERATING);
                foreach (var path in paths)
                {
                    for (int i = path.Path.Count - 1; i >= 0; i--)
                    {
                        schema.DeleteTable(path.Path[i].GreaterSet); // Delete (indirectly) generated table
                    }
                }
                schema.DeleteTable(gTab); // Delete (directly) generated table
                // This column will be now deleted as a result of the deletion of the generated table
            }
            else if(selDim.LesserSet.Definition.DefinitionType == TableDefinitionType.PROJECTION) // It is a extracted table and this column is produced by the mapping (depends onfunction output tuple)
            {
                ComColumn projDim = selDim.LesserSet.Definition.GeneratingDimensions[0];
                Mapping mapping = projDim.Definition.Mapping;
                PathMatch match = mapping.GetMatchForTarget(new DimPath(selDim));
                mapping.RemoveMatch(match.SourcePath, match.TargetPath);

                schema.DeleteColumn(selDim);
            }
            else // Just delete this column
            {
                schema.DeleteColumn(selDim);
            }

            e.Handled = true;
        }

        private void HelpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://conceptoriented.com");
        }

        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AboutBox dlg = new AboutBox(); // Instantiate the dialog box
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 
        }

        #endregion

        #region Wizards (with user interactions)

        public void Wizard_CsvDatasource()
        {
            SetTopCsv top = (SetTopCsv)RemoteSources.FirstOrDefault(x => x is SetTopCsv);
            ComSchema schema = MashupTop;

            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
            ofd.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
            ofd.Filter = "Access Files (*.CSV)|*.CSV|All files (*.*)|*.*";
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() != true) return;

            string filePath = ofd.FileName;
            string safeFilePath = ofd.SafeFileName;
            string fileDir = System.IO.Path.GetDirectoryName(filePath);
            string tableName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            //
            // Create a source table and load its structure into the schema
            //
            // TODO: Check that this table has not been loaded yet (compare paths). Or do it in dialog. 
            // If already exists then either allow for imports into different local tables (with different parameters along different projection dimensions) or reuse the existing imported table.
            SetCsv sourceTable = (SetCsv)top.CreateTable(tableName);
            sourceTable.FilePath = filePath;
            top.LoadSchema(sourceTable);

            //
            // Create a target table and configure import using a mapping stored in projection dimensions
            //

            // Create a new (imported) set
            string newTableName = tableName;
            ComTable targetTable = schema.CreateTable(newTableName);
            targetTable.Definition.DefinitionType = TableDefinitionType.PROJECTION;

            // Create generating/import column
            string newColumnName = sourceTable.Name;
            ComColumn importDim = schema.CreateColumn(newColumnName, sourceTable, targetTable, false);
            importDim.Definition.DefinitionType = ColumnDefinitionType.LINK;
            importDim.Definition.IsGenerating = true;

            //
            // Show parameters for set extraction
            //
            List<ComColumn> initialSelection = new List<ComColumn>();
            initialSelection.Add(SelectedColumn);
            ColumnMappingBox dlg = new ColumnMappingBox(schema, importDim, initialSelection);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            // Populate this new table (in fact, can be done separately during Update)
            targetTable.Definition.Populate();

            SelectedTable = targetTable;
        }

        public void Wizard_TextDatasource()
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
            ofd.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
            ofd.Filter = "Access Files (*.CSV)|*.CSV|All files (*.*)|*.*";
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() != true) return;

            string filePath = ofd.FileName;
            string safeFilePath = ofd.SafeFileName;
            string fileDir = System.IO.Path.GetDirectoryName(filePath);
            string tableName = safeFilePath.Replace('.', '#');

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + fileDir + "; Extended Properties='Text;Excel 12.0;HDR=Yes;FMT=CSVDelimited;'";

            //
            // Initialize a new data source schema
            //
            ConnectionOledb conn = new ConnectionOledb();
            conn.ConnectionString = connectionString;

            SetTopOledb top = new SetTopOledb("");
            top.connection = conn;
            top.LoadSchema(); // Load complete schema
            ComTable sourceTable = top.FindTable(tableName);

            //
            // Configure import by creating a mapping for import dimensions
            //

            ComSchema schema = MashupTop;

            ComTable targetTable = schema.CreateTable(tableName);
            targetTable.Definition.DefinitionType = TableDefinitionType.PROJECTION;

            // Create generating/import column
            Mapper mapper = new Mapper(); // Create mapping for an import dimension
            Mapping map = mapper.CreatePrimitive(sourceTable, targetTable, schema); // Complete mapping (all to all)
            map.Matches.ForEach(m => m.TargetPath.Path.ForEach(p => p.Add()));

            ComColumn dim = schema.CreateColumn(map.SourceSet.Name, map.SourceSet, map.TargetSet, false);
            dim.Definition.Mapping = map;
            dim.Definition.DefinitionType = ColumnDefinitionType.LINK;
            dim.Definition.IsGenerating = true;

            dim.Add();

            // Populate this new table (in fact, can be done separately during Update)
            schema.AddTable(targetTable, null, null);
            targetTable.Definition.Populate();

            SelectedTable = targetTable;
        }

        public void Wizard_AccessDatasource()
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

            // Initialize a new data source schema
            SetTopOledb top = new SetTopOledb("My Data Source");

            //top.ConnectionString = connectionString;

            //top.Open();
            //top.ImportSchema();

            //Sources.Add(top); // Append to the list of data sources

            // And also append to the tree model
            SubsetTree sourceModel = new SubsetTree(top.Root.SuperDim);
            sourceModel.ExpandTree();
            //SourcesModel.Add(sourceModel);
        }

        private static void Wizard_SqlserverDatasource()
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

        public void Wizard_ProductTable(ComTable set)
        {
            ComSchema schema = MashupTop;

            // Create a new (product) set
            string productTableName = "New Table";
            ComTable productSet = schema.CreateTable(productTableName);

            // Show parameters for creating a product set
            ProductTableBox dlg = new ProductTableBox(schema, productSet, SelectedTable);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            //
            // Add filter expression for the new table
            //

            // Create a new column (temporary, just to store a where expression)
            ComColumn column = schema.CreateColumn("Where Expression", productSet, schema.GetPrimitive("Boolean"), false);

            // Show dialog for authoring arithmetic expression
            ArithmeticBox whereDlg = new ArithmeticBox(column, true);
            whereDlg.Owner = this;
            whereDlg.ShowDialog(); // Open the dialog box modally 

            // if cancelled then remove the new set and all its columns
            if (whereDlg.DialogResult == false) 
            {
                schema.DeleteTable(productSet);
                return;
            }

            // Populate the set and its dimensions (alternatively, it can be done explicitly by Update command).
            productSet.Definition.Populate();

            SelectedTable = productSet;
        }

        public void Wizard_ExtractTable(ComTable set)
        {
            ComSchema schema = MashupTop;

            // Create a new (extracted) set
            string newTableName = "New Table";
            ComTable targetTable = schema.CreateTable(newTableName);
            targetTable.Definition.DefinitionType = TableDefinitionType.PROJECTION;

            // Create a new (mapped, generating) dimension to the new set
            string newColumnName = "New Column";
            ComColumn extractedDim = schema.CreateColumn(newColumnName, set, targetTable, false);

            //
            // Show parameters for set extraction
            //
            List<ComColumn> initialSelection = new List<ComColumn>();
            initialSelection.Add(SelectedColumn);
            ColumnMappingBox dlg = new ColumnMappingBox(schema, extractedDim, initialSelection);
            dlg.Owner = this;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            // Populate the set and the dimension. The dimension is populated precisely as any (mapped) dimension
            targetTable.Definition.Populate();

            SelectedTable = targetTable;
        }

        public void Wizard_EditTable(ComTable table)
        {
            if (table == null) return;

            ComSchema schema = MashupTop;

            if (table.Definition.DefinitionType == TableDefinitionType.PROJECTION)
            {
                ComColumn column = table.Definition.GeneratingDimensions[0];

                ColumnMappingBox dlg = new ColumnMappingBox(schema, column, null);
                dlg.Owner = this;
                dlg.ShowDialog(); // Open the dialog box modally 

                if (dlg.DialogResult == false) return; // Cancel
            }
            else if (table.Definition.DefinitionType == TableDefinitionType.PRODUCT)
            {
                // Create a new column (temporary, just to store a where expression)
                ComColumn column = schema.CreateColumn("Where Expression", table, schema.GetPrimitive("Boolean"), false);

                // Show dialog for authoring arithmetic expression
                ArithmeticBox whereDlg = new ArithmeticBox(column, true);
                whereDlg.Owner = this;
                whereDlg.ShowDialog(); // Open the dialog box modally 
            }
            else
            {
                throw new NotImplementedException("A table must have a definition of certain type.");
            }

            MashupModelRoot.NotifyAllOnPropertyChanged(""); // Notify visual components about changes in this column

            // In fact, we have to determine if the column has been really changed and what kind of changes (name change does not require reevaluation)
            table.Definition.Populate();

            SelectedTable = table;
        }

        public void Wizard_AddArithmetic(ComTable srcSet)
        {
            if (srcSet == null) return;

            ComSchema schema = MashupTop;

            // Create a new column
            ComColumn column = schema.CreateColumn("New Column", srcSet, null, false); // We do not know its output type

            // Show dialog for authoring arithmetic expression
            ArithmeticBox dlg = new ArithmeticBox(column, false);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            if (column.Definition.Formula == null) return; // No formula
            
            column.Add();

            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        public void Wizard_AddLink(ComTable sourceTable, ComTable targetTable)
        {
            if (sourceTable == null) return;

            ComSchema schema = MashupTop;

            // Create a new (mapped) dimension using the mapping
            ComColumn column = schema.CreateColumn("New Column", sourceTable, targetTable, false);

            // Show link column dialog
            LinkColumnBox dlg = new LinkColumnBox(column);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            column.Add();

            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        public void Wizard_AddAggregation(ComTable srcSet, ComColumn measureColumn)
        {
            if (srcSet == null) return;

            ComSchema schema = MashupTop;

            // Create new aggregated column
            ComColumn column = schema.CreateColumn("My Column", srcSet, null, false);

            // Show recommendations and let the user choose one of them
            AggregationBox dlg = new AggregationBox(column, measureColumn);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            column.Add();

            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        public void Wizard_EditColumn(ComColumn column)
        {
            if (column == null) return;

            ComSchema schema = MashupTop;

            if (column.Definition.DefinitionType == ColumnDefinitionType.ARITHMETIC)
            {
                ArithmeticBox dlg = new ArithmeticBox(column, false);
                dlg.Owner = this;
                dlg.ShowDialog(); // Open the dialog box modally 

                if (dlg.DialogResult == false) return; // Cancel

            }
            else if (column.Definition.DefinitionType == ColumnDefinitionType.LINK)
            {
                LinkColumnBox dlg = new LinkColumnBox(column);
                dlg.Owner = this;
                dlg.ShowDialog(); // Open the dialog box modally 

                if (dlg.DialogResult == false) return; // Cancel
            }
            else if (column.Definition.DefinitionType == ColumnDefinitionType.AGGREGATION)
            {
                AggregationBox dlg = new AggregationBox(column, null);
                dlg.Owner = this;
                dlg.ShowDialog(); // Open the dialog box modally 

                if (dlg.DialogResult == false) return; // Cancel
            }
            else
            {
                throw new NotImplementedException("A column must have a definition of certain type.");
            }

            // Notify visual components about changes in this column
            MashupModelRoot.NotifyAllOnPropertyChanged("");
            
            // In fact, we have to determine if the column has been really changed and what kind of changes (name change does not require reevaluation)
            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        #endregion

        #region Operations (no user interactions)

        public void Operation_OpenTable(ComTable set)
        {
            lblWorkspace.Content = set.Name;

            var gridView = new SetGridView(set);
            GridPanel.Content = gridView.Grid;
        }

        #endregion

    }

    public class DragDropHelper
    {
        public bool CanDrag(object data)
        {
            if (data is Set) return true;
            else if (data is Dim) return true;
            else if (data is SubsetTree) return true;
            return false;
        }

        public bool CanDrop(object dropSource, object dropTarget)
        {
            if (dropSource == null || dropTarget == null) return false;
            return true;
        }

        public void Drop(object dropSource, object dropTarget)
        {
            if (dropSource == null || dropTarget == null) return;

            // Transform to one format (Set or Dim) from several possible classes: Set, Dim, SubsetTree
            if (dropSource is SubsetTree)
            {
                if (((SubsetTree)dropSource).IsSubsetNode) dropSource = ((SubsetTree)dropSource).LesserSet;
                else if (((SubsetTree)dropSource).IsDimensionNode) dropSource = ((SubsetTree)dropSource).Dim;
            }
            if (dropTarget is SubsetTree)
            {
                if (((SubsetTree)dropTarget).IsSubsetNode) dropTarget = ((SubsetTree)dropTarget).LesserSet;
                else if (((SubsetTree)dropTarget).IsDimensionNode) dropTarget = ((SubsetTree)dropTarget).Dim;
            }

            //
            // Conditions for new aggregated column: dimension is dropped on a set
            //
            if (dropSource is Dim && ((MainWindow)App.Current.MainWindow).IsInMashups((Dim)dropSource))
            {
                if (dropTarget is Set && !(((Set)dropTarget).Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups((Set)dropTarget))
                {
                    // Note that here source and target in terms of DnD have opposite interpretations to the aggregation method
                    ((MainWindow)App.Current.MainWindow).Wizard_AddAggregation((Set)dropTarget, (Dim)dropSource);
                }
            }

            //
            // TODO: Conditions for link column: a set is dropped on another set
            //
            if (dropSource is Set && !(((Set)dropSource).Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups((Set)dropSource))
            {
                if (dropTarget is Set && ((MainWindow)App.Current.MainWindow).IsInMashups((Set)dropTarget))
                {
                    ((MainWindow)App.Current.MainWindow).Wizard_AddLink((ComTable)dropTarget, (ComTable)dropSource);
                }
            }

        }

        public void DoDoubleClick(object data)
        {
            if (data == null) return;

            //
            // Conditions for opening a table view
            //
            if (data is SubsetTree && ((SubsetTree)data).IsSubsetNode) 
            {
                ComTable set = ((SubsetTree)data).LesserSet;
                if(!(set.Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups(set)) 
                {
                    // Call a direct operation method for opening a table with the necessary parameters (rather than a command)
                    ((MainWindow)App.Current.MainWindow).Operation_OpenTable(set);
                }
            }
        }
    }

}
