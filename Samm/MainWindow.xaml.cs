using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Com.Utils;
using Com.Schema;
using Com.Schema.Csv;
using Com.Schema.Rel;
using Samm.Dialogs;

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

        public DcWorkspace Workspace { get; set; }

        public DcSchema MashupTop { 
            get { return Workspace.Mashup; } 
            set { if (value == Workspace.Mashup) return; Workspace.Schemas.Remove(Workspace.Mashup); Workspace.Schemas.Add(value); } 
        }
        public DcTable MashupRoot { get { return MashupTop != null ? MashupTop.Root : null; } }


        public bool IsInMashups(DcTable set) // Determine if the specified set belongs to some mashup
        {
            if (set == null || MashupTop == null) return false;
            if (set.Schema == MashupTop) return true;
            return false;
        }
        public bool IsInMashups(DcColumn dim) // Determine if the specified dimension belongs to some mashup
        {
            if (dim == null || MashupTop == null) return false;
            if (IsInMashups(dim.Input) && IsInMashups(dim.Output)) return true;
            return false;
        }

        //
        // Selection state
        //
        protected DcSchema _selectedSchema;
        public DcSchema SelectedSchema_Bound
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
        public DcSchema SelectedSchema
        {
            get { return SelectedSchema_Bound; }
            set
            {
                // Change selected item directly in the control -> it will triger notifications
                if (SchemaListView != null)
                {
                    SchemaListView.SelectedItem = value;

                    ((Set)value).NotifyPropertyChanged("");
                }
            }
        }

        public DcTable SelectedTable 
        {
            get { return TableListView != null ? TableListView.SelectedItem : null; }
            set 
            {
                if (TableListView == null || TableListView.TablesList == null) return;
                TableListView.TablesList.SelectedItem = value;

                ((Set)value).NotifyPropertyChanged("");
            } 
        }

        public DcColumn SelectedColumn
        {
            get { return ColumnListView != null ? ColumnListView.SelectedItem : null; }
            set
            {
                if (ColumnListView == null || ColumnListView.ColumnList == null) return;
                ColumnListView.ColumnList.SelectedItem = value;

                ((Dim)value).NotifyPropertyChanged("");
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

        public DcSchema CreateSampleSchema()
        {
            DcSchema ds = new Schema("Sample Mashup");
            DcColumn d1, d2, d3, d4;

            DcTable departments = ds.CreateTable("Departments");
            ds.AddTable(departments, null, null);

            d1 = ds.CreateColumn("name", departments, ds.GetPrimitive("String"), true);
            d1.Add();
            d2 = ds.CreateColumn("location", departments, ds.GetPrimitive("String"), false);
            d2.Add();

            departments.Data.Append(new DcColumn[] { d1, d2 }, new object[] { "SALES", "Dresden" });
            departments.Data.Append(new DcColumn[] { d1, d2 }, new object[] { "HR", "Walldorf" });

            DcTable employees = ds.CreateTable("Employees");
            ds.AddTable(employees, null, null);

            d1 = ds.CreateColumn("name", employees, ds.GetPrimitive("String"), true);
            d1.Add();
            d2 = ds.CreateColumn("age", employees, ds.GetPrimitive("Double"), false);
            d2.Add();
            d3 = ds.CreateColumn("salary", employees, ds.GetPrimitive("Double"), false);
            d3.Add();
            d4 = ds.CreateColumn("dept", employees, departments, false);
            d4.Add();

            DcTable managers = ds.CreateTable("Managers");
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
            DcSchema mashupTop = new Schema("New Mashup");
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
            dlg.Filter = "Data Commandr (*.mashup)|*.mashup|All files (*.*)|*.*";
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
            DcWorkspace workspace = (Workspace)Utils.CreateObjectFromJson(json);

            ((Workspace)workspace).FromJson(json, workspace);

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
            dlg.Filter = "Data Commandr (*.mashup)|*.mashup|All files (*.*)|*.*";

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

        # region Schema operations

        private void AddMashupSchemaCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }
        private void AddMashupSchemaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void AddCsvSchemaCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void AddCsvSchemaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SchemaCsv csvSchema = new SchemaCsv("My Files");
            csvSchema.Workspace = Workspace;
            Workspace.Schemas.Add(csvSchema);
            ConnectionCsv conn = new ConnectionCsv();

            e.Handled = true;
        }

        private void EditSchemaCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedSchema == null) e.CanExecute = false;
            else if (SelectedSchema is SchemaCsv)
            {
                e.CanExecute = false;
            }
            else // Mashup
            {
                e.CanExecute = false;
            }
        }
        private void EditSchemaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
        }

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

        private void DeleteSchemaCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedSchema == null) e.CanExecute = false;
            else if (SelectedSchema is SchemaCsv)
            {
                e.CanExecute = true;
            }
            else // Mashup
            {
                e.CanExecute = false;
            }
        }
        private void DeleteSchemaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //
            // Ask for confirmation
            //
            string msg = Application.Current.FindResource("DeleteSchemaMsg").ToString();
            var result = MessageBox.Show(this, msg, "Delete data source...", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            if (SelectedSchema == null) return;
            else if (SelectedSchema is SchemaCsv)
            {
                Workspace.Schemas.Remove(SelectedSchema);
            }
            else // Mashup
            {
            }

            e.Handled = true;
        }

        private void UpdateSchemaCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedSchema != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void UpdateSchemaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
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

                SetCsv table = (SetCsv)SelectedSchema.CreateTable(dlg.TableName);

                table.FilePath = dlg.FilePath;

                table.HasHeaderRecord = dlg.HasHeaderRecord;
                table.Delimiter = dlg.Delimiter;
                table.CultureInfo.NumberFormat.NumberDecimalSeparator = dlg.Decimal;

                SelectedSchema.AddTable(table, null, null);

                var columns = ((SchemaCsv)SelectedSchema).LoadSchema(table);
                columns.ForEach(x => x.Add());

                SelectedTable = table;
            }
            else // Mashup
            {
                TableBox dlg = new TableBox(SelectedSchema, null);
                dlg.Owner = this;
                dlg.ShowDialog();

                if (dlg.DialogResult == false) return; // Cancel

                DcTable table = SelectedSchema.CreateTable(dlg.TableName);
                table.Definition.WhereFormula = dlg.TableFormula;
                SelectedSchema.AddTable(table, null, null);

                SelectedTable = table;
            }

            e.Handled = true;
        }

        private void EditTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null)
            {
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

                SetCsv table = (SetCsv)SelectedTable;

                table.FilePath = dlg.FilePath;

                table.HasHeaderRecord = dlg.HasHeaderRecord;
                table.Delimiter = dlg.Delimiter;
                table.CultureInfo.NumberFormat.NumberDecimalSeparator = dlg.Decimal;


                foreach (DcColumn col in table.Columns.ToArray()) if (!col.IsSuper) col.Remove();
                var columns = ((SchemaCsv)SelectedSchema).LoadSchema(table);
                columns.ForEach(x => x.Add());
            }
            else // Mashup
            {
                TableBox dlg = new TableBox(SelectedSchema, SelectedTable);
                dlg.Owner = this;
                dlg.ShowDialog();

                if (dlg.DialogResult == false) return; // Cancel

                SelectedTable.Name = dlg.TableName;
                SelectedTable.Definition.WhereFormula = dlg.TableFormula;
            }

            SelectedTable = SelectedTable;

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
            DcTable set = null;
            if (SelectedTable != null)
                set = SelectedTable;
            else if (SelectedColumn != null && SelectedColumn.Input != null)
                set = SelectedColumn.Input;
            else return;

            DcSchema schema = set.Schema;

            //
            // Ask for confirmation
            //
            string msg = Application.Current.FindResource("DeleteTableMsg").ToString();
            var result = MessageBox.Show(this, msg, "Delete table...", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            // 
            // Delete tables *generated* from this table (alternatively, leave them but with empty definition)
            //
            var paths = new PathEnumerator(new List<DcTable>(new DcTable[] { set }), new List<DcTable>(), false, DimensionType.GENERATING);
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

        private void AddColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedTable != null) e.CanExecute = true;
            else e.CanExecute = false;
        }
        private void AddColumnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTable == null) return;

            try
            {
                Wizard_AddColumn(SelectedTable);
            }
            catch (System.Exception ex)
            {
                GenericError(ex);
            }

            e.Handled = true;
        }
        public void Wizard_AddColumn(DcTable table)
        {
            DcSchema schema = MashupTop;

            //
            // Show parameters for set extraction
            //
            ColumnBox dlg = new ColumnBox(Workspace.Schemas, table, null);
            dlg.Owner = this;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            //
            // Create a new column using parameters in the dialog
            //
            DcColumn column = schema.CreateColumn(dlg.ColumnName, table, dlg.SelectedTargetTable, dlg.IsKey);
            column.Definition.Formula = dlg.ColumnFormula;
            column.Add();

            SelectedColumn = column;
        }

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
        public void Wizard_FreeColumns(DcTable table)
        {
            DcSchema schema = MashupTop;

            // Show parameters for creating a product set
            FreeColumnBox dlg = new FreeColumnBox(schema, table);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            // Populate the set and its dimensions
            table.Definition.Populate();
        }

        private void EditColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (SelectedColumn != null)
            {
                e.CanExecute = true;
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
        public void Wizard_EditColumn(DcColumn column)
        {
            if (column == null) return;

            DcSchema schema = MashupTop;

            ColumnBox dlg = new ColumnBox(Workspace.Schemas, column.Input, column);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            //
            // Update the column using parameters in the dialog
            //
            column.Name = dlg.ColumnName;
            column.IsKey = dlg.IsKey;
            column.Definition.Formula = dlg.ColumnFormula;
            column.Output = dlg.SelectedTargetTable;

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
        protected void Operation_DeleteColumn(DcColumn column)
        {
            DcSchema schema = column.Input.Schema;

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
                DcTable gTab = column.Output;
                var paths = new PathEnumerator(new List<DcTable>(new DcTable[] { gTab }), new List<DcTable>(), false, DimensionType.GENERATING);
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
            else if (column.Input.Definition.DefinitionType == DcTableDefinitionType.PROJECTION) // It is a extracted table and this column is produced by the mapping (depends onfunction output tuple)
            {
                //DcColumn projDim = column.Input.InputColumns.Where(d => d.Definition.IsAppendData).ToList()[0];
                //Mapping mapping = projDim.Definition.Mapping;
                //PathMatch match = mapping.GetMatchForTarget(new DimPath(column));
                //mapping.RemoveMatch(match.SourcePath, match.TargetPath);

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
        public void Operation_OpenTable(DcTable table)
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
                    //((MainWindow)App.Current.MainWindow).Wizard_AddAggregation((Set)dropTarget, (Dim)dropSource);
                }
            }

            //
            // TODO: Conditions for link column: a set is dropped on another set
            //
            if (dropSource is Set && !(((Set)dropSource).Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups((Set)dropSource))
            {
                if (dropTarget is Set && ((MainWindow)App.Current.MainWindow).IsInMashups((Set)dropTarget))
                {
                    //((MainWindow)App.Current.MainWindow).Wizard_AddPathLink((DcTable)dropTarget, (DcTable)dropSource);
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
                DcTable set = ((SubsetTree)data).Input;
                if(!(set.Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups(set)) 
                {
                    // Call a direct operation method for opening a table with the necessary parameters (rather than a command)
                    ((MainWindow)App.Current.MainWindow).Operation_OpenTable(set);
                }
            }
        }
    }

}
