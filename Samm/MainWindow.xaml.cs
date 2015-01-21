using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Com.Model;
using Samm.Dialogs;
using System.Globalization;

namespace Samm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //
        // Configuration and options
        //
        public bool Config_CompressFile = true;
        NumberFormatInfo defaultNumberFormat = new CultureInfo("en-US").NumberFormat;

        CultureInfo defaultCultureInfo = new System.Globalization.CultureInfo("en-US");

        //
        // Workspace
        //
        public string WorkspaceFile { get; set; } // File where the workspace is stored

        public Workspace Workspace { get; set; }

        public ComSchema MashupTop { 
            get { return Workspace.Mashup; } 
            set { if (value == Workspace.Mashup) return; Workspace.Schemas.Remove(Workspace.Mashup); Workspace.Schemas.Add(value); } 
        }
        public ComTable MashupRoot { get { return MashupTop != null ? MashupTop.Root : null; } }


        public bool IsInMashups(ComTable set) // Determine if the specified set belongs to some mashup
        {
            if (set == null || MashupTop == null) return false;
            if (set.Schema == MashupTop) return true;
            return false;
        }
        public bool IsInMashups(ComColumn dim) // Determine if the specified dimension belongs to some mashup
        {
            if (dim == null || MashupTop == null) return false;
            if (IsInMashups(dim.Input) && IsInMashups(dim.Output)) return true;
            return false;
        }

        //
        // Selection state
        //
        protected ComSchema _selectedSchema;
        public ComSchema SelectedSchema_Bound
        {
            get { return _selectedSchema; }
            set
            {
                _selectedSchema = value;
                
                // Notify TableList
                if (TableListView != null)
                {
                    TableListView.Schema = value;
                }
            }
        }
        public ComSchema SelectedSchema
        {
            get { return SelectedSchema_Bound; }
            set
            {
                // Change selected item directly in the control -> it will triger notifications
                if (SchemaListView != null)
                {
                    SchemaListView.SelectedItem = value;
                }
            }
        }

        public ComTable SelectedTable 
        {
            get { return TableListView != null ? TableListView.SelectedItem : null; }
            set 
            {
                if (TableListView == null || TableListView.TablesList == null) return;
                TableListView.TablesList.SelectedItem = value;
            } 
        }

        public ComColumn SelectedColumn
        {
            get { return ColumnListView != null ? ColumnListView.SelectedItem : null; }
            set
            {
                if (ColumnListView == null || ColumnListView.ColumnList == null) return;
                ColumnListView.ColumnList.SelectedItem = value;
            }
        }

        //
        // Operations and behavior
        //
        public DragDropHelper DragDropHelper { get; protected set; }

        public MainWindow()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = defaultCultureInfo;
            System.Threading.Thread.CurrentThread.CurrentUICulture = defaultCultureInfo;

            Utils.cultureInfo = defaultCultureInfo;

            Workspace = new Workspace();

            DragDropHelper = new DragDropHelper();

            //this.DataContext = this;
            InitializeComponent();

            // Create new empty mashup
            Operation_NewWorkspace();
        }

        public ComSchema CreateSampleSchema()
        {
            ComSchema ds = new Schema("Sample Mashup");
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

        protected void GenericError(System.Exception e)
        {
            string msg = Application.Current.FindResource("GenericErrorMsg").ToString();
            var result = MessageBox.Show(this, msg + "\n\nError message: \n" + e.Message, "Error...", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }

        # region File operations

        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                NewFileWizard();
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        private bool NewFileWizard()
        {
            // Ask if changes have to be saved before loading a new workspace
            var saveChanges = MessageBox.Show(this, "Do you want to save changes?", "New...", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
            if (saveChanges == MessageBoxResult.Yes)
            {
                bool isSaved = SaveFileWizard();
                if (!isSaved) return false; // Cancelled in save as dialog
            }
            else if (saveChanges == MessageBoxResult.No)
            {
                // Abondon changes.
            }
            else
            {
                return false;
            }

            Operation_NewWorkspace();

            return true;
        }
        protected void Operation_NewWorkspace()
        {
            if (Workspace == null) Workspace = new Workspace();
            else Workspace.Schemas.Clear();

            //
            // Initialize mashup schema
            //
            ComSchema mashupTop = new Schema("New Mashup");
            mashupTop = CreateSampleSchema();
            mashupTop.Workspace = Workspace;
            Workspace.Schemas.Add(mashupTop);
            Workspace.Mashup = mashupTop;

            //
            // Initialize predefined schemas
            //
            SchemaCsv csvSchema = new SchemaCsv("My Files");
            csvSchema.Workspace = Workspace;
            Workspace.Schemas.Add(csvSchema);
            ConnectionCsv conn = new ConnectionCsv();

            //
            // Update the model that is shown in the visual component
            //
            SelectedSchema = MashupTop;
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                OpenFileWizard();
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        private bool OpenFileWizard()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
            //dlg.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
            dlg.Filter = "DataCommander (*.mashup)|*.mashup|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;
            dlg.CheckFileExists = true;
            dlg.Multiselect = false;

            Nullable<bool> result = dlg.ShowDialog();

            if (result != true) return false;

            string filePath = dlg.FileName;
            string safeFilePath = dlg.SafeFileName;
            string fileDir = System.IO.Path.GetDirectoryName(filePath);
            string tableName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // Ask if changes have to be saved before loading a new workspace
            var saveChanges = MessageBox.Show(this, "Do you want to save changes?", "New...", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
            if (saveChanges == MessageBoxResult.Yes)
            {
                bool isSaved = SaveFileWizard();
                if (!isSaved) return false; // Cancelled in save as dialog
            }
            else if (saveChanges == MessageBoxResult.No)
            {
                // Abondon changes.
            }
            else
            {
                return false;
            }

            // Read from the file and de-serialize workspace
            Operation_ReadWorkspace(filePath);
            WorkspaceFile = filePath;

            return true;
        }
        protected void Operation_ReadWorkspace(string filePath)
        {
            byte[] jsonBytes = System.IO.File.ReadAllBytes(filePath);

            string jsonString = System.IO.File.ReadAllText(filePath);
            if (Config_CompressFile)
            {
                jsonString = Utils.Unzip(jsonBytes);
            }
            else
            {
                jsonString = Encoding.UTF8.GetString(jsonBytes);
            }
            
            // De-serialize
            JObject json = (JObject)JsonConvert.DeserializeObject(jsonString, new JsonSerializerSettings { });
            Workspace workspace = (Workspace)Utils.CreateObjectFromJson(json);

            workspace.FromJson(json, workspace);

            // Switch to new workspace
            Workspace = workspace;

            //
            // Update the model that is shown in the visual component
            //
            SelectedSchema = MashupTop;
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SaveFileWizard();
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        private bool SaveFileWizard()
        {
            if (string.IsNullOrEmpty(WorkspaceFile))
            {
                bool isSaved = SaveAsFileWizard(); // Choose a file to save to. It will call this method again.
                return isSaved;
            }
            else
            {
                Operation_WriteWorkspace(WorkspaceFile); // Simply write to file
                return true;
            }
        }

        private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SaveAsFileWizard();
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        private bool SaveAsFileWizard()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog(); //Alterantive: dialog = new System.Windows.Forms.SaveFileDialog();
            dlg.FileName = "Mashup";
            dlg.DefaultExt = ".mashup";
            dlg.Filter = "DataCommander (*.mashup)|*.mashup|All files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();

            if (result != true) return false; // Cancelled

            string filePath = dlg.FileName;
            string safeFilePath = dlg.SafeFileName;
            string fileDir = System.IO.Path.GetDirectoryName(filePath);
            string tableName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            WorkspaceFile = filePath;
            Operation_WriteWorkspace(WorkspaceFile); // Really save

            return true; // Saved
        }
        protected void Operation_WriteWorkspace(string filePath)
        {
            // Serialize
            JObject json = Utils.CreateJsonFromObject(Workspace);
            Workspace.ToJson(json);
            string jsonString = JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings { });

            byte[] jsonBytes;
            if (Config_CompressFile)
            {
                jsonBytes = Utils.Zip(jsonString);
            }
            else
            {
                jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            }

            // Write to file
            System.IO.File.WriteAllBytes(filePath, jsonBytes);
        }

        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AboutBox dlg = new AboutBox(); // Instantiate the dialog box
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var response = MessageBox.Show(this, "Do you want to save your changes?", "Exiting...", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
            if (response == MessageBoxResult.Yes)
            {
                bool isSaved = SaveFileWizard();
                if (isSaved) // Was really saved
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else if (response == MessageBoxResult.No) 
            {
                Application.Current.Shutdown();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void HelpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://conceptoriented.com");
        }

        #endregion

        # region Import/export operations

        private void ImportTextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Wizard_ImportCsv();
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Wizard_ImportCsv()
        {
            SchemaCsv sourceSchema = (SchemaCsv)Workspace.Schemas.FirstOrDefault(x => x is SchemaCsv);
            ComSchema targetSchema = MashupTop;

            string tableName = "New Table";
            ComTable targetTable = targetSchema.CreateTable(tableName);
            targetTable.Definition.DefinitionType = TableDefinitionType.PROJECTION;

            string columnName = "New Column";
            ComColumn column = sourceSchema.CreateColumn(columnName, null, targetTable, false);

            //
            // Show import dialog with mapping and other parameters
            //
            ImportMappingBox dlg = new ImportMappingBox(sourceSchema, targetSchema, column, null);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            targetTable.Definition.Populate(); // Populate this new table

            SelectedTable = targetTable;
        }
        public void Wizard_ImportText() // Reading text via Oledb
        {
            var ofd = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
            //ofd.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
            ofd.Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*";
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

            SchemaOledb top = new SchemaOledb("");
            top.connection = conn;
            top.LoadSchema(); // Load complete schema
            ComTable sourceTable = top.GetSubTable(tableName);

            //
            // Configure import by creating a mapping for import dimensions
            //

            ComSchema schema = MashupTop;

            ComTable targetTable = schema.CreateTable(tableName);
            targetTable.Definition.DefinitionType = TableDefinitionType.PROJECTION;

            // Create generating/import column
            Mapper mapper = new Mapper(); // Create mapping for an import dimension
            Mapping map = mapper.CreatePrimitive(sourceTable, targetTable, schema); // Complete mapping (all to all)
            map.Matches.ForEach(m => m.TargetPath.Segments.ForEach(p => p.Add()));

            ComColumn dim = schema.CreateColumn(map.SourceSet.Name, map.SourceSet, map.TargetSet, false);
            dim.Definition.Mapping = map;
            dim.Definition.DefinitionType = ColumnDefinitionType.LINK;
            dim.Definition.IsAppendData = true;

            dim.Add();

            // Populate this new table 
            schema.AddTable(targetTable, null, null);
            targetTable.Definition.Populate();

            SelectedTable = targetTable;
        }
        
        private void ImportAccessCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Wizard_ImportAccess();
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Wizard_ImportAccess()
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
            //ofd.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
            ofd.Filter = "Access Files (*.ACCDB)|*.ACCDB|All files (*.*)|*.*";
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() != true) return;

            string filePath = ofd.FileName;
            string safeFilePath = ofd.SafeFileName;

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath;

            // Initialize a new data source schema
            SchemaOledb top = new SchemaOledb("My Data Source");

            //top.ConnectionString = connectionString;

            //top.Open();
            //top.ImportSchema();

            //Sources.Add(top); // Append to the list of data sources

            // And also append to the tree model
            SubsetTree sourceModel = new SubsetTree(top.Root.SuperColumn);
            sourceModel.ExpandTree();
            //SourcesModel.Add(sourceModel);
        }

        private void ImportSqlserverCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Wizard_ImportSqlserver();
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        private static void Wizard_ImportSqlserver()
        {
            /*
            // Read schema: http://www.simple-talk.com/dotnet/.net-framework/schema-and-metadata-retrieval-using-ado.net/

            // DataTable schema = connection.GetSchema();
            // DataTable schema = connection.GetSchema("Databases", new string[] { "Northwind" });
            // DataTable schema = connection.GetSchema("Databases");
            // DataTable schema = connection.GetSchema(System.Data.SqlClient.SqlClientMetaDataCollectionNames.Databases);

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
            /*
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
            */
        }

        private void EditImportCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable == null) e.CanExecute = false;
            else
            {
                var importDims = SelectedTable.InputColumns.Where(x => x.Definition.IsAppendData && x.Input.Schema != MashupTop);
                if (importDims == null || importDims.Count() == 0) e.CanExecute = false;
                else e.CanExecute = true;
            }
        }
        private void EditImportCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var importDims = SelectedTable.InputColumns.Where(x => x.Definition.IsAppendData && x.Input.Schema != MashupTop);
            if (importDims == null || importDims.Count() == 0) return;

            ComColumn column = importDims.ToList()[0];

            SchemaCsv sourceSchema = (SchemaCsv)Workspace.Schemas.FirstOrDefault(x => x is SchemaCsv);
            ComSchema targetSchema = MashupTop;

            //
            // Show import dialog with mapping and other parameters
            //
            ImportMappingBox dlg = new ImportMappingBox(sourceSchema, targetSchema, column, null);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            column.Output.Definition.Populate(); // Populate table

            e.Handled = true;
        }

        private void ExportTextCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void ExportTextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Wizard_ExportCsv(SelectedTable);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Wizard_ExportCsv(ComTable table)
        {
            SchemaCsv top = (SchemaCsv)Workspace.Schemas.FirstOrDefault(x => x is SchemaCsv);
            ComSchema schema = MashupTop;

            var dlg = new Microsoft.Win32.SaveFileDialog(); //Alterantive: dialog = new System.Windows.Forms.SaveFileDialog();
            dlg.FileName = table.Name;
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();

            if (result != true) return; // Cancelled

            string filePath = dlg.FileName;
            string safeFilePath = dlg.SafeFileName;
            string fileDir = System.IO.Path.GetDirectoryName(filePath);
            string tableName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            //
            // Create a target file and write all records to it
            //
            ComColumn[] columns = table.Columns.Where(x => !x.IsSuper).ToArray();

            using (var sw = new System.IO.StreamWriter(filePath))
            {
                var csv = new CsvHelper.CsvWriter(sw);

                // Write header
                for (int j = 0; j < columns.Length; j++)
                {
                    csv.WriteField(columns[j].Name, true);
                }
                csv.NextRecord(); // End of record

                // Write records
                for (int i = 0; i < table.Data.Length; i++)
                {
                    for (int j = 0; j < columns.Length; j++)
                    {
                        object val = table.Data.GetValue(columns[j].Name, i);
                        string field = val.ToString();
                        bool shouldQuote = false;
                        if(StringSimilarity.SameColumnName(columns[j].Output.Name, "String")) 
                        {

                            shouldQuote = true;
                        }

                        csv.WriteField(field, shouldQuote);
                    }

                    csv.NextRecord(); // End of record
                }
            }

        }
        
        #endregion

        # region Schema operations

        private void RenameSchemaCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedSchema != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void RenameSchemaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedSchema == null) return;

            RenameBox dlg = new RenameBox(SelectedSchema, null);
            dlg.Owner = this;
            dlg.ShowDialog();

            if (dlg.DialogResult == false) return; // Cancel
        }

        private void UpdateSchemaCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedSchema != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void UpdateSchemaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException("Update Schema not implemented.");

            e.Handled = true;
        }

        #endregion

        # region Table operations

        private void AddTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedSchema != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void AddTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedSchema == null) return;

            // For each schema/connection type, we use a specific dialog
            if (SelectedSchema is SchemaCsv)
            {
                TableCsvBox dlg = new TableCsvBox(SelectedSchema, null);
                dlg.Owner = this;
                dlg.ShowDialog();

                if (dlg.DialogResult == false) return; // Cancel
            }
            else // Mashup
            {
                TableBox dlg = new TableBox(SelectedSchema, null);
                dlg.Owner = this;
                dlg.ShowDialog();

                if (dlg.DialogResult == false) return; // Cancel
            }

            e.Handled = true;
        }

        private void FilterTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void FilterTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;
            Wizard_FilterTable(SelectedTable);
            e.Handled = true;
        }
        public void Wizard_FilterTable(ComTable table)
        {
            if (table == null) return;

            ComSchema schema = MashupTop;

            // Create a new column (temporary, just to store a where expression which is used in the dialog)
            ComColumn column = schema.CreateColumn("Where Expression", table, schema.GetPrimitive("Boolean"), false);

            // Show dialog for authoring arithmetic expression
            ArithmeticBox whereDlg = new ArithmeticBox(column, true);
            whereDlg.Owner = this;
            whereDlg.ShowDialog(); // Open the dialog box modally 

            ((Set)table).NotifyPropertyChanged(""); // Notify visual components about changes in this column

            // In fact, we have to determine if the column has been really changed and what kind of changes (name change does not require reevaluation)
            table.Definition.Populate();

            SelectedTable = table;
        }

        private void EditTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null)
            {
                if (IsInMashups(SelectedTable))
                    e.CanExecute = false;
                else
                    e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }
        private void EditTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;

            // For each schema/connection type, we use a specific dialog
            if (SelectedSchema is SchemaCsv)
            {
                TableCsvBox dlg = new TableCsvBox(SelectedSchema, SelectedTable);
                dlg.Owner = this;
                dlg.ShowDialog();

                if (dlg.DialogResult == false) return; // Cancel
            }
            else // Mashup
            {
                RenameBox dlg = new RenameBox(SelectedTable, null);
                dlg.Owner = this;
                dlg.ShowDialog();

                if (dlg.DialogResult == false) return; // Cancel
            }

            e.Handled = true;
        }

        private void RenameTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void RenameTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;

            RenameBox dlg = new RenameBox(SelectedTable, null);
            dlg.Owner = this;
            dlg.ShowDialog();

            if (dlg.DialogResult == false) return; // Cancel
        }

        private void DeleteTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void DeleteTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ComTable set = null;
            if (SelectedTable != null)
                set = SelectedTable;
            else if (SelectedColumn != null && SelectedColumn.Input != null)
                set = SelectedColumn.Input;
            else return;

            ComSchema schema = set.Schema;

            // Ask for confirmation
            string msg = Application.Current.FindResource("DeleteTableMsg").ToString();
            var result = MessageBox.Show(this, msg, "Delete table...", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            // 
            // Delete tables *generated* from this table (alternatively, leave them but with empty definition)
            //
            var paths = new PathEnumerator(new List<ComTable>(new ComTable[] { set }), new List<ComTable>(), false, DimensionType.GENERATING);
            foreach (var path in paths)
            {
                for (int i = path.Segments.Count - 1; i >= 0; i--)
                {
                    schema.DeleteTable(path.Segments[i].Output); // Delete (indirectly) generated table
                }
            }

            // Remove all connections of this set with the schema by deleting all its dimensions
            schema.DeleteTable(set);

            e.Handled = true;
        }

        private void UpdateTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void UpdateTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;
            try
            {
                SelectedTable.Definition.Populate();
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }
        }

        #endregion

        # region Column operations

        private void AddFreeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void AddFreeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;

            try
            {
                Wizard_FreeColumns(SelectedTable);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Wizard_FreeColumns(ComTable table)
        {
            ComSchema schema = MashupTop;

            // Show parameters for creating a product set
            FreeColumnBox dlg = new FreeColumnBox(schema, table);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            // Populate the set and its dimensions
            table.Definition.Populate();
        }

        private void AddArithmeticCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void AddArithmeticCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Wizard_AddArithmetic(SelectedTable);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Wizard_AddArithmetic(ComTable table)
        {
            if (table == null) return;

            ComSchema schema = MashupTop;

            // Create a new column
            ComColumn column = schema.CreateColumn("New Column", table, null, false); // We do not know its output type

            // Show dialog for authoring arithmetic expression
            ArithmeticBox dlg = new ArithmeticBox(column, false);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            if (column.Definition.FormulaExpr == null) return; // No formula

            column.Add();

            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        private void AddPathLinkCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void AddPathLinkCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Wizard_AddPathLink(SelectedTable, null);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }
        }
        public void Wizard_AddPathLink(ComTable sourceTable, ComTable targetTable)
        {
            if (sourceTable == null) return;

            ComSchema schema = MashupTop;

            // Create a new (mapped) dimension using the mapping
            ComColumn column = schema.CreateColumn("New Column", sourceTable, targetTable, false);

            // Show link column dialog
            PathMappingBox dlg = new PathMappingBox(column);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            column.Add();

            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        private void AddColumnLinkCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void AddColumnLinkCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;

            try
            {
                Wizard_AddColumnLink(SelectedTable, null);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Wizard_AddColumnLink(ComTable sourceTable, ComTable targetTable)
        {
            ComSchema schema = MashupTop;

            // Create a new (mapped, generating) dimension to the new set
            ComColumn column = schema.CreateColumn("New Column", sourceTable, targetTable, false);

            //
            // Show parameters for set extraction
            //
            List<ComColumn> initialSelection = new List<ComColumn>();
            initialSelection.Add(SelectedColumn);
            ColumnMappingBox dlg = new ColumnMappingBox(Workspace.Schemas, column, initialSelection);
            dlg.Owner = this;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            column.Add();

            targetTable = column.Output;

            // Populate the set and the dimension 
            targetTable.Definition.Populate();

            SelectedColumn = column;
        }

        private void AddAggregationCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void AddAggregationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ComTable table = SelectedTable;
            if (table == null && SelectedColumn != null)
            {
                table = SelectedColumn.Input;
            }
            try
            {
                Wizard_AddAggregation(table, null);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Wizard_AddAggregation(ComTable table, ComColumn measureColumn)
        {
            if (table == null) return;

            ComSchema schema = MashupTop;

            // Create new aggregated column
            ComColumn column = schema.CreateColumn("My Column", table, null, false);

            // Show recommendations and let the user choose one of them
            AggregationBox dlg = new AggregationBox(column, measureColumn);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            column.Add();

            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        private void EditColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedColumn != null)
            {
                if (SelectedColumn.Definition.DefinitionType == ColumnDefinitionType.FREE) e.CanExecute = false;
                else e.CanExecute = true;
            }
            else e.CanExecute = false;
        }
        private void EditColumnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedColumn == null) return;

            try
            {
                Wizard_EditColumn(SelectedColumn);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Wizard_EditColumn(ComColumn column)
        {
            if (column == null) return;

            ComSchema schema = MashupTop;

            if (column.Definition.DefinitionType == ColumnDefinitionType.FREE)
            {
                return;
            }
            else if (column.Definition.DefinitionType == ColumnDefinitionType.ARITHMETIC)
            {
                ArithmeticBox dlg = new ArithmeticBox(column, false);
                dlg.Owner = this;
                dlg.ShowDialog(); // Open the dialog box modally 

                if (dlg.DialogResult == false) return; // Cancel

            }
            else if (column.Definition.DefinitionType == ColumnDefinitionType.LINK)
            {
                if (!column.Definition.IsAppendData)
                {
                    PathMappingBox dlg = new PathMappingBox(column);
                    dlg.Owner = this;
                    dlg.ShowDialog(); // Open the dialog box modally 

                    if (dlg.DialogResult == false) return; // Cancel
                }
                else // Generating (import) column
                {
                    // ComColumn column = table.InputColumns.Where(d => d.Definition.IsAppendData).ToList()[0];

                    ColumnMappingBox dlg = new ColumnMappingBox(Workspace.Schemas, column, null);
                    dlg.Owner = this;
                    dlg.ShowDialog(); // Open the dialog box modally 

                    if (dlg.DialogResult == false) return; // Cancel
                }

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

            ((Dim)column).NotifyPropertyChanged(""); // Notify visual components about changes in this column

            // In fact, we have to determine if the column has been really changed and what kind of changes (name change does not require reevaluation)
            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        private void RenameColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedColumn != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void RenameColumnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedColumn == null) return;

            RenameBox dlg = new RenameBox(SelectedColumn, null);
            dlg.Owner = this;
            dlg.ShowDialog();

            if (dlg.DialogResult == false) return; // Cancel
        }

        private void DeleteColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedColumn != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void DeleteColumnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedColumn == null) return;

            try
            {
                Operation_DeleteColumn(SelectedColumn);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }


            e.Handled = true;
        }
        protected void Operation_DeleteColumn(ComColumn column)
        {
            ComSchema schema = column.Input.Schema;

            // Ask for confirmation
            string msg = Application.Current.FindResource("DeleteColumnMsg").ToString();
            var result = MessageBox.Show(this, msg, "Delete column...", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            // 
            // Delete related columns/tables
            //
            if (column.Definition.IsAppendData) // Delete all tables that are directly or indirectly generated by this column
            {
                ComTable gTab = column.Output;
                var paths = new PathEnumerator(new List<ComTable>(new ComTable[] { gTab }), new List<ComTable>(), false, DimensionType.GENERATING);
                foreach (var path in paths)
                {
                    for (int i = path.Segments.Count - 1; i >= 0; i--)
                    {
                        schema.DeleteTable(path.Segments[i].Output); // Delete (indirectly) generated table
                    }
                }
                schema.DeleteTable(gTab); // Delete (directly) generated table
                // This column will be now deleted as a result of the deletion of the generated table
            }
            else if (column.Input.Definition.DefinitionType == TableDefinitionType.PROJECTION) // It is a extracted table and this column is produced by the mapping (depends onfunction output tuple)
            {
                ComColumn projDim = column.Input.InputColumns.Where(d => d.Definition.IsAppendData).ToList()[0];
                Mapping mapping = projDim.Definition.Mapping;
                PathMatch match = mapping.GetMatchForTarget(new DimPath(column));
                mapping.RemoveMatch(match.SourcePath, match.TargetPath);

                schema.DeleteColumn(column);
            }
            else // Just delete this column
            {
                schema.DeleteColumn(column);
            }
        }

        private void UpdateColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedColumn != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void UpdateColumnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedColumn == null) return;
            try
            {
                SelectedColumn.Definition.Evaluate();
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }
        }

        #endregion

        # region Data operations

        private void OpenTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void OpenTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;
            try
            {
                Operation_OpenTable(SelectedTable);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Operation_OpenTable(ComTable table)
        {
            //lblWorkspace.Content = table.Name;

            var gridView = new SetGridView(table);
            GridPanel.Content = gridView.Grid;
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
                if (((SubsetTree)dropSource).IsSubsetNode) dropSource = ((SubsetTree)dropSource).Input;
                else if (((SubsetTree)dropSource).IsDimensionNode) dropSource = ((SubsetTree)dropSource).Dim;
            }
            if (dropTarget is SubsetTree)
            {
                if (((SubsetTree)dropTarget).IsSubsetNode) dropTarget = ((SubsetTree)dropTarget).Input;
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
                    ((MainWindow)App.Current.MainWindow).Wizard_AddPathLink((ComTable)dropTarget, (ComTable)dropSource);
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
                ComTable set = ((SubsetTree)data).Input;
                if(!(set.Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups(set)) 
                {
                    // Call a direct operation method for opening a table with the necessary parameters (rather than a command)
                    ((MainWindow)App.Current.MainWindow).Operation_OpenTable(set);
                }
            }
        }
    }

}
