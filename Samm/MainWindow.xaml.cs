using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Com.Utils;
using Com.Schema;
using Com.Schema.Csv;
using Com.Schema.Rel;
using Com.Data;
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
        // Space
        //
        public string SpaceFile { get; set; } // File where the space is stored

        private DcSpace _space;
        public DcSpace Space {
            get { return _space; }
            set
            {
                if (_space == value) return;

                if(_space != null) ((Space)_space).CollectionChanged -= this.CollectionChanged;
                _space = value;
                if (_space != null) ((Space)_space).CollectionChanged += this.CollectionChanged;

                // Update list of schemas (or notify)
                UpdateSchemaList();
            }
        }

        public DcSchema MashupTop { 
            get { return Space.GetSchemas().FirstOrDefault(x => x.GetSchemaKind() == DcSchemaKind.Dc); } 
        }
        public DcTable MashupRoot { get { return MashupTop != null ? MashupTop.Root : null; } }


        public bool IsInMashups(DcTable tab) // Determine if the specified set belongs to some mashup
        {
            if (tab == null || MashupTop == null) return false;
            if (tab.Schema == MashupTop) return true;
            return false;
        }
        public bool IsInMashups(DcColumn dim) // Determine if the specified dimension belongs to some mashup
        {
            if (dim == null || MashupTop == null) return false;
            if (IsInMashups(dim.Input) && IsInMashups(dim.Output)) return true;
            return false;
        }

        //
        // Schema List View Model
        //

        // What is displayed in the list and bound to it as (ItemsSource)
        public ObservableCollection<DcTable> SchemaList { get; set; }
        public void UpdateSchemaList()
        {
            SchemaList.Clear();
            if (Space == null) return;

            // Fill the list of items
            foreach (DcTable schema in Space.GetSchemas())
            {
                SchemaList.Add(schema);
            }
        }

        // It is what we bind to the list view (SelectedItem)
        private DcSchema _selectedSchema;
        public DcSchema SelectedSchema
        {
            get { return _selectedSchema; }
            set
            {
                if (_selectedSchema == value) return;
                _selectedSchema = value;

                // Update list of columns (or notify)
                UpdateTableList();

                // Update Formula
                MainWindow main = this;
                main.FormulaBarType.Text = "";
                main.FormulaBarName.Text = "";
                main.FormulaBarFormula.Text = "";
            }
        }

        //
        // Table List View Model
        //

        // What is displayed in the list and bound to it as (ItemsSource)
        public ObservableCollection<DcTable> TableList { get; set; }
        public void UpdateTableList()
        {
            TableList.Clear();
            if (SelectedSchema == null) return;

            // Fill the list of items
            foreach (DcTable table in SelectedSchema.Root.SubTables)
            {
                TableList.Add(table);
            }
        }

        // It is what we bind to the list view (SelectedItem)
        private DcTable _selectedTable;
        public DcTable SelectedTable
        {
            get { return _selectedTable; }
            set
            {
                if (_selectedTable == value) return;
                _selectedTable = value;

                // Update list of columns (or notify)
                UpdateColumnList();

                // Update Formula
                MainWindow main = this;
                main.FormulaBarType.Text = "";
                main.FormulaBarName.Text = "";
                main.FormulaBarFormula.Text = "";
            }
        }

        //
        // Column List View Model
        //

        // What is displayed in the list and bound to it as (ItemsSource)
        public ObservableCollection<DcColumn> ColumnList { get; set; }
        public void UpdateColumnList()
        {
            ColumnList.Clear();
            if (SelectedTable == null) return;

            // Fill the list of items
            foreach (DcColumn column in SelectedTable.Columns)
            {
                if (column.IsSuper) continue;
                ColumnList.Add(column);
            }
        }

        // It is what we bind to the list view (SelectedItem)
        private DcColumn _selectedColumn;
        public DcColumn SelectedColumn
        {
            get { return _selectedColumn; }
            set
            {
                if (_selectedColumn == value) return;
                _selectedColumn = value;

                MainWindow main = this;

                main.FormulaBarType.Text = _selectedColumn == null ? "" : _selectedColumn.Output.Name;
                main.FormulaBarName.Text = _selectedColumn == null ? "" : _selectedColumn.Name;
                main.FormulaBarFormula.Text = _selectedColumn == null || _selectedColumn.GetData() == null ? "" : _selectedColumn.GetData().Formula;
            }
        }


        //
        // Dialog boxes and controls
        //
        public ColumnBox columnBox;
        
        //
        // Listeners from the model object
        //
        public void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MainWindow vm = this;

            if (e.Action == NotifyCollectionChangedAction.Add) 
            {
                if (e.NewItems == null || e.NewItems.Count == 0) return;

                if (e.NewItems[0] is DcSchema)
                {
                    DcSchema sch = (DcSchema)e.NewItems[0];
                    if (sch == null) return;
                    if (vm.SchemaList.Contains(sch)) return;

                    vm.SchemaList.Add(sch);
                }
                else if (e.NewItems[0] is DcTable)
                {
                    DcTable tab = (DcTable)e.NewItems[0];
                    if (tab == null) return;
                    if (vm.TableList.Contains(tab)) return;

                    vm.TableList.Add(tab);
                }
                else if (e.NewItems[0] is DcColumn)
                {
                    DcColumn column = (DcColumn)e.NewItems[0];
                    if (column == null) return;
                    if (column.Input != vm.SelectedTable) return;
                    if (vm.ColumnList.Contains(column)) return;

                    vm.ColumnList.Add(column);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems == null || e.OldItems.Count == 0) return;

                if (e.OldItems[0] is DcSchema)
                {
                    DcSchema sch = (DcSchema)e.OldItems[0];
                    if (sch == null) return;
                    if (!vm.SchemaList.Contains(sch)) return;

                    vm.SchemaList.Remove(sch);
                }
                else if (e.OldItems[0] is DcTable)
                {
                    DcTable tab = (DcTable)e.OldItems[0];
                    if (tab == null) return;
                    if (!vm.TableList.Contains(tab)) return;

                    vm.TableList.Remove(tab);
                }
                else if (e.OldItems[0] is DcColumn)
                {
                    DcColumn column = (DcColumn)e.OldItems[0];
                    if (column == null) return;
                    if (column.Input != vm.SelectedTable) return;
                    if (!vm.ColumnList.Contains(column)) return;

                    vm.ColumnList.Remove(column);
                }
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

            // View models: for controls, dialog boxes and the main window model
            SchemaList = new ObservableCollection<DcTable>();
            TableList = new ObservableCollection<DcTable>();
            ColumnList = new ObservableCollection<DcColumn>();

            // Model: business object being shown/edited
            Space = new Space();

            DragDropHelper = new DragDropHelper();

            InitializeComponent();

            // Views: dialog boxes, controls and other visual elements
            columnBox = new ColumnBox(this);

            // Create new empty mashup
            Operation_NewSpace();
        }

        public void CreateSampleSchema(DcSchema schema)
        {
            DcSpace space = schema.Space;

            DcColumn d1, d2, d3, d4;

            DcTable departments = space.CreateTable("Departments", schema.Root);

            d1 = space.CreateColumn("name", departments, schema.GetPrimitiveType("String"), true);
            d2 = space.CreateColumn("location", departments, schema.GetPrimitiveType("String"), false);

            DcTableWriter writer;

            writer = departments.GetData().GetTableWriter();
            writer.Open();
            writer.Append(new DcColumn[] { d1, d2 }, new object[] { "SALES", "Dresden" });
            writer.Append(new DcColumn[] { d1, d2 }, new object[] { "HR", "Walldorf" });
            writer.Close();

            DcTable employees = space.CreateTable("Employees", schema.Root);

            d1 = space.CreateColumn("name", employees, schema.GetPrimitiveType("String"), true);
            d2 = space.CreateColumn("age", employees, schema.GetPrimitiveType("Double"), false);
            d3 = space.CreateColumn("salary", employees, schema.GetPrimitiveType("Double"), false);
            d4 = space.CreateColumn("dept", employees, departments, false);

            DcTable managers = space.CreateTable("Managers", employees);

            d1 = space.CreateColumn("title", managers, schema.GetPrimitiveType("String"), false);
            d2 = space.CreateColumn("is project manager", managers, schema.GetPrimitiveType("Boolean"), false);
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

            Operation_NewSpace();

            return true;
        }
        protected void Operation_NewSpace()
        {
            DcSpace space = new Space();

            //
            // Initialize mashup schema
            //
            DcSchema mashupTop = space.CreateSchema("New Mashup", DcSchemaKind.Dc);
            CreateSampleSchema(mashupTop);

            //
            // Initialize predefined schemas
            //
            SchemaCsv csvSchema = (SchemaCsv)space.CreateSchema("My Files", DcSchemaKind.Csv);


            //
            // Update the model that is shown in the visual component
            //
            Space = space;
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
            Operation_ReadSpace(filePath);
            SpaceFile = filePath;

            return true;
        }
        protected void Operation_ReadSpace(string filePath)
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
            DcSpace space = (Space)Utils.CreateObjectFromJson(json);

            ((Space)space).FromJson(json, space);

            // Switch to new workspace
            Space = space;

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
            if (string.IsNullOrEmpty(SpaceFile))
            {
                bool isSaved = SaveAsFileWizard(); // Choose a file to save to. It will call this method again.
                return isSaved;
            }
            else
            {
                Operation_WriteSpace(SpaceFile); // Simply write to file
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

            SpaceFile = filePath;
            Operation_WriteSpace(SpaceFile); // Really save

            return true; // Saved
        }
        protected void Operation_WriteSpace(string filePath)
        {
            // Serialize
            JObject json = Utils.CreateJsonFromObject(Space);
            Space.ToJson(json);
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
            SchemaCsv csvSchema = (SchemaCsv)Space.CreateSchema("My Files", DcSchemaKind.Csv);

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
                Space.DeleteSchema(SelectedSchema);
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

                TableCsv table = (TableCsv)Space.CreateTable(dlg.TableName, SelectedSchema.Root);

                table.FilePath = dlg.FilePath;

                table.HasHeaderRecord = dlg.HasHeaderRecord;
                table.Delimiter = dlg.Delimiter;
                table.CultureInfo.NumberFormat.NumberDecimalSeparator = dlg.Decimal;

                var columns = ((SchemaCsv)SelectedSchema).LoadSchema(table);

                SelectedTable = table;
            }
            else // Mashup
            {
                TableBox dlg = new TableBox(this);
                dlg.Owner = this;
                dlg.Schema = SelectedSchema;
                Nullable<bool> dlgResult = dlg.ShowDialog(); // Open the dialog box modally

                //if (dlg.DialogResult == false) return; // Cancel
                if (!(dlg.DialogResult.HasValue && dlg.DialogResult.Value)) return; // Cancel

                SelectedTable = dlg.Table;
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

                TableCsv table = (TableCsv)SelectedTable;

                table.FilePath = dlg.FilePath;

                table.HasHeaderRecord = dlg.HasHeaderRecord;
                table.Delimiter = dlg.Delimiter;
                table.CultureInfo.NumberFormat.NumberDecimalSeparator = dlg.Decimal;


                foreach (DcColumn col in table.Columns.ToArray()) if (!col.IsSuper) Space.DeleteColumn(col);
                var columns = ((SchemaCsv)SelectedSchema).LoadSchema(table);
            }
            else // Mashup
            {
                TableBox dlg = new TableBox(this);
                dlg.Owner = this;
                dlg.Table = SelectedTable;
                Nullable<bool> dlgResult = dlg.ShowDialog(); // Open the dialog box modally

                //if (dlg.DialogResult == false) return; // Cancel
                if (!(dlg.DialogResult.HasValue && dlg.DialogResult.Value)) return; // Cancel
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
            DcTable tab = null;
            if (SelectedTable != null)
                tab = SelectedTable;
            else if (SelectedColumn != null && SelectedColumn.Input != null)
                tab = SelectedColumn.Input;
            else return;

            DcSchema schema = tab.Schema;

            //
            // Ask for confirmation
            //
            string msg = Application.Current.FindResource("DeleteTableMsg").ToString();
            var result = MessageBox.Show(this, msg, "Delete table...", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            // Delete table along with all its input/output columns
            Space.DeleteTable(tab);

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
                SelectedTable.GetData().Populate();
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
            columnBox = new ColumnBox(this);
            columnBox.Owner = this;
            columnBox.Table = table;
            Nullable<bool> dlgResult = columnBox.ShowDialog(); // Open the dialog box modally

            //if (columnBox.DialogResult == false) return; // Cancel
            if (!(columnBox.DialogResult.HasValue && columnBox.DialogResult.Value)) return; // Cancel

            SelectedColumn = columnBox.Column;
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
            table.GetData().Populate();
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

            columnBox = new ColumnBox(this);
            columnBox.Owner = this;
            columnBox.Column = column;
            Nullable<bool> dlgResult = columnBox.ShowDialog(); // Open the dialog box modally

            //if (columnBox.DialogResult == false) return; // Cancel
            if (!(columnBox.DialogResult.HasValue && columnBox.DialogResult.Value)) return; // Cancel

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

            Space.DeleteColumn(column);
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
                SelectedColumn.GetData().Evaluate();
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
            lblSpace.Content = "DATA";

            GridPanel.Content = null;

            e.Handled = true;
        }

        #endregion

        //
        // Window operations
        //
        #region Window operations

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Onwer of a dialog can be set only of the main (this) window has been already shown
            columnBox.Owner = this;
        }

        #endregion

    }



    public class DragDropHelper
    {
        public bool CanDrag(object data)
        {
            if (data is Table) return true;
            else if (data is Column) return true;
            else if (data is SubtableTree) return true;
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

            // Transform to one format (Set or Dim) from several possible classes: Table, Column, SubtableTree
            if (dropSource is SubtableTree)
            {
                if (((SubtableTree)dropSource).IsSubsetNode) dropSource = ((SubtableTree)dropSource).Input;
                else if (((SubtableTree)dropSource).IsColumnNode) dropSource = ((SubtableTree)dropSource).Column;
            }
            if (dropTarget is SubtableTree)
            {
                if (((SubtableTree)dropTarget).IsSubsetNode) dropTarget = ((SubtableTree)dropTarget).Input;
                else if (((SubtableTree)dropTarget).IsColumnNode) dropTarget = ((SubtableTree)dropTarget).Column;
            }

            //
            // Conditions for new aggregated column: dimension is dropped on a set
            //
            if (dropSource is Column && ((MainWindow)App.Current.MainWindow).IsInMashups((Column)dropSource))
            {
                if (dropTarget is Table && !(((Table)dropTarget).Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups((Table)dropTarget))
                {
                    // Note that here source and target in terms of DnD have opposite interpretations to the aggregation method
                    //((MainWindow)App.Current.MainWindow).Wizard_AddAggregation((Table)dropTarget, (Dim)dropSource);
                }
            }

            //
            // TODO: Conditions for link column: a set is dropped on another set
            //
            if (dropSource is Table && !(((Table)dropSource).Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups((Table)dropSource))
            {
                if (dropTarget is Table && ((MainWindow)App.Current.MainWindow).IsInMashups((Table)dropTarget))
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
            if (data is SubtableTree && ((SubtableTree)data).IsSubsetNode) 
            {
                DcTable tab = ((SubtableTree)data).Input;
                if(!(tab.Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups(tab)) 
                {
                    // Call a direct operation method for opening a table with the necessary parameters (rather than a command)
                    ((MainWindow)App.Current.MainWindow).Operation_OpenTable(tab);
                }
            }
        }

    }

}
