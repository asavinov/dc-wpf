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

        // TODO: we need to represent connections and source tables

        //
        // Mashups (only one is used)
        //
        public ObservableCollection<CsSchema> Mashups { get; set; }
        public CsSchema MashupTop { get { return Mashups.Count != 0 ? Mashups[0] : null; } }
        public CsTable MashupRoot { get { return Mashups.Count != 0 ? Mashups[0].Root : null; } }

        public ObservableCollection<SubsetTree> MashupsModel { get; set; } // What is shown in SubsetTree for mashups
        public SubsetTree MashupModelRoot { get { return MashupsModel.Count != 0 ? (SubsetTree)MashupsModel[0] : null; } }

        public bool IsInMashups(CsTable set) // Determine if the specified set belongs to some mashup
        {
            if (set == null || Mashups == null) return false;
            foreach (CsSchema t in Mashups) { if (set.Top == t) return true; }
            return false;
        }
        public bool IsInMashups(CsColumn dim) // Determine if the specified dimension belongs to some mashup
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
        public CsTable SelectedRoot
        {
            get 
            { 
                SubsetTree item = SelectedItem; 
                if (item == null) return null;
                if (item.IsSubsetNode && item.LesserSet.IsPrimitive && item.LesserSet.Name == "Root") return (CsTable)item.LesserSet; 
                return null;
            }
        }
        public CsTable SelectedTable 
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
        public CsColumn SelectedColumn 
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

            //
            // Initialize mashups (one empty mashup)
            //
            Mashups = new ObservableCollection<CsSchema>();
            MashupsModel = new ObservableCollection<SubsetTree>();

            CsSchema mashupTop = CreateSampleSchema(); // new SetTop("New Mashup");
            Mashups.Add(mashupTop);

            SubsetTree mashupModel = new SubsetTree(mashupTop.Root.SuperDim);
            mashupModel.ExpandTree();
            MashupsModel.Add(mashupModel);

            DragDropHelper = new DragDropHelper();

            //this.DataContext = this;
            InitializeComponent();
        }

        public CsSchema CreateSampleSchema()
        {
            CsSchema ds = new SetTop("Sample Mashup");
            CsColumn d1, d2, d3, d4;

            CsTable departments = ds.CreateTable("Departments");
            ds.AddTable(departments, null, null);

            d1 = ds.CreateColumn("name", departments, ds.GetPrimitive("String"), true);
            d1.Add();
            d2 = ds.CreateColumn("location", departments, ds.GetPrimitive("String"), false);
            d2.Add();

            departments.Data.Append(new CsColumn[] { d1, d2 }, new object[] { "SALES", "Dresden" });
            departments.Data.Append(new CsColumn[] { d1, d2 }, new object[] { "HR", "Walldorf" });

            CsTable employees = ds.CreateTable("Employees");
            ds.AddTable(employees, null, null);

            d1 = ds.CreateColumn("name", employees, ds.GetPrimitive("String"), true);
            d1.Add();
            d2 = ds.CreateColumn("age", employees, ds.GetPrimitive("Double"), false);
            d2.Add();
            d3 = ds.CreateColumn("salary", employees, ds.GetPrimitive("Double"), false);
            d3.Add();
            d4 = ds.CreateColumn("dept", employees, departments, false);
            d4.Add();

            CsTable managers = ds.CreateTable("Managers");
            ds.AddTable(managers, employees, null);

            d1 = ds.CreateColumn("title", managers, ds.GetPrimitive("String"), false);
            d1.Add();
            d2 = ds.CreateColumn("is project manager", managers, ds.GetPrimitive("Boolean"), false);
            d2.Add();

            return ds;
        }

        # region Command_Executed (call backs from Commands)

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

        private void TextDatasourceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_TextDatasource();
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

        private void FilteredTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;
            e.Handled = true;
        }

        private void ProductTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = true;
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
            CsTable table = SelectedTable;
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
            CsTable set = null;
            if (SelectedTable != null)
                set = SelectedTable;
            else if (SelectedColumn != null && SelectedColumn.LesserSet != null)
                set = SelectedColumn.LesserSet;
            else return;

            // Remove all connections of this set with the schema by deleting all its dimensions
            set.SuperDim.Remove();
            set.SubDims.ToArray().ToList().ForEach(x => x.Remove());

            set.GreaterDims.ToArray().ToList().ForEach(x => x.Remove());
            set.LesserDims.ToArray().ToList().ForEach(x => x.Remove());

            e.Handled = true;
        }

        private void UpdateTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = true;
            else if (SelectedColumn != null) e.CanExecute = false;
            else e.CanExecute = false;
        }
        private void UpdateTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;
            SelectedTable.Definition.Populate();
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
            CsColumn selDim = SelectedColumn;
            CsTable srcSet = SelectedTable;
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
            CsColumn selDim = SelectedColumn;
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
            CsColumn selDim = SelectedColumn;
            if (selDim == null) return;

            selDim.Remove();

            e.Handled = true;
        }

        private void UpdateColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedRoot != null) e.CanExecute = false;
            else if (SelectedTable != null) e.CanExecute = false;
            else if (SelectedColumn != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void UpdateColumnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedColumn == null) return;
            SelectedColumn.Definition.Evaluate();
        }

        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AboutBox dlg = new AboutBox(); // Instantiate the dialog box
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 
        }

        #endregion

        #region Wizards (with user interactions)

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
            CsTable sourceTable = top.FindTable(tableName);

            //
            // Configure import by creating a mapping for import dimensions
            //

            CsSchema schema = MashupTop;
            CsTable targetTable = schema.CreateTable(tableName);
            schema.AddTable(targetTable, null, null);

            Mapper mapper = new Mapper(); // Create mapping for an import dimension
            Mapping map = mapper.CreatePrimitive(sourceTable, targetTable); // Complete mapping (all to all)
            map.Matches.ForEach(m => m.TargetPath.Path.ForEach(p => p.Add()));

            CsColumn dim = new Dim(map); // Create generating/import column with this mapping
            dim.Add();

            // Populate this new table (in fact, can be done separately during Update)
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

        public void Wizard_ProductTable(CsTable set)
        {
            CsSchema schema = MashupTop;

            // Create a new (product) set
            string productTableName = "New Table";
            CsTable productSet = schema.CreateTable(productTableName);

            // Show parameters for creating a product set
            ProductTableBox dlg = new ProductTableBox(schema, productSet, SelectedTable);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            //
            // Add filter expression for the new table
            //

            // Create a new column (temporary, just to store a where expression)
            CsColumn column = schema.CreateColumn("Where Expression", productSet, schema.GetPrimitive("Boolean"), false);

            // Show dialog for authoring arithmetic expression
            ArithmeticBox whereDlg = new ArithmeticBox(column, true);
            whereDlg.Owner = this;
            whereDlg.ShowDialog(); // Open the dialog box modally 

            // if cancelled then remove the new set and all its columns
            if (whereDlg.DialogResult == false) 
            {
                schema.RemoveTable(productSet);
                return;
            }

            // Populate the set and its dimensions (alternatively, it can be done explicitly by Update command).
            productSet.Definition.Populate();

            SelectedTable = productSet;
        }

        public void Wizard_ExtractTable(CsTable set)
        {
            CsSchema schema = MashupTop;

            // Create a new (extracted) set
            string newTableName = "New Table";
            CsTable extractedSet = schema.CreateTable(newTableName);
            extractedSet.Definition.DefinitionType = TableDefinitionType.PROJECTION;

            // Create a new (mapped, generating) dimension to the new set
            string newColumnName = "New Column";
            CsColumn extractedDim = schema.CreateColumn(newColumnName, set, extractedSet, false);

            //
            // Show parameters for set extraction
            //
            ExtractTableBox dlg = new ExtractTableBox(extractedDim, SelectedColumn);
            dlg.Owner = this;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            // Populate the set and the dimension. The dimension is populated precisely as any (mapped) dimension
            extractedSet.Definition.Populate();

            SelectedTable = extractedSet;
        }

        public void Wizard_EditTable(CsTable table)
        {
            if (table == null) return;

            CsSchema schema = MashupTop;

            if (table.Definition.DefinitionType == TableDefinitionType.PROJECTION)
            {
                CsColumn column = table.Definition.GeneratingDimensions[0];

                ExtractTableBox dlg = new ExtractTableBox(column, null);
                dlg.Owner = this;
                dlg.ShowDialog(); // Open the dialog box modally 

                if (dlg.DialogResult == false) return; // Cancel
            }
            if (table.Definition.DefinitionType == TableDefinitionType.PRODUCT)
            {
                // Create a new column (temporary, just to store a where expression)
                CsColumn column = schema.CreateColumn("Where Expression", table, schema.GetPrimitive("Boolean"), false);

                // Show dialog for authoring arithmetic expression
                ArithmeticBox whereDlg = new ArithmeticBox(column, true);
                whereDlg.Owner = this;
                whereDlg.ShowDialog(); // Open the dialog box modally 
            }
            else
            {
                throw new NotImplementedException("A table must have a definition of certain type.");
            }

            // Notify visual components about changes in this column
            MashupModelRoot.NotifyAllOnPropertyChanged("");

            // In fact, we have to determine if the column has been really changed and what kind of changes (name change does not require reevaluation)
            table.Definition.Populate();

            SelectedTable = table;
        }

        public void Wizard_AddArithmetic(CsTable srcSet)
        {
            if (srcSet == null) return;

            CsSchema schema = MashupTop;

            // Create a new column
            CsColumn column = schema.CreateColumn("New Column", srcSet, null, false); // We do not know its output type

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

        public void Wizard_AddLink(CsTable sourceTable, CsTable targetTable)
        {
            if (sourceTable == null) return;

            CsSchema schema = MashupTop;

            // Create a new (mapped) dimension using the mapping
            CsColumn column = schema.CreateColumn("New Column", sourceTable, targetTable, false);

            // Show link column dialog
            LinkColumnBox dlg = new LinkColumnBox(column);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            column.Add();

            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        public void Wizard_AddAggregation(CsTable srcSet, CsColumn measureColumn)
        {
            if (srcSet == null) return;

            CsSchema schema = MashupTop;

            // Create new aggregated column
            CsColumn column = schema.CreateColumn("My Column", srcSet, null, false);

            // Show recommendations and let the user choose one of them
            AggregationBox dlg = new AggregationBox(column, measureColumn);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            column.Add();

            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        public void Wizard_EditColumn(CsColumn column)
        {
            if (column == null) return;

            CsSchema schema = MashupTop;

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

        public void Operation_OpenTable(CsTable set)
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
                    ((MainWindow)App.Current.MainWindow).Wizard_AddLink((CsTable)dropTarget, (CsTable)dropSource);
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
                CsTable set = ((SubsetTree)data).LesserSet;
                if(!(set.Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups(set)) 
                {
                    // Call a direct operation method for opening a table with the necessary parameters (rather than a command)
                    ((MainWindow)App.Current.MainWindow).Operation_OpenTable(set);
                }
            }
        }
    }

}
