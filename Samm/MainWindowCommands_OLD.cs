using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Samm
{
    class MainWindowCommands_OLD
    {
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
            DcSchema targetSchema = MashupTop;

            string tableName = "New Table";
            DcTable targetTable = targetSchema.CreateTable(tableName);
            targetTable.Definition.DefinitionType = DcTableDefinitionType.PROJECTION;

            string columnName = "New Column";
            DcColumn column = sourceSchema.CreateColumn(columnName, null, targetTable, false);

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
            DcTable sourceTable = top.GetSubTable(tableName);

            //
            // Configure import by creating a mapping for import dimensions
            //

            DcSchema schema = MashupTop;

            DcTable targetTable = schema.CreateTable(tableName);
            targetTable.Definition.DefinitionType = DcTableDefinitionType.PROJECTION;

            // Create generating/import column
            Mapper mapper = new Mapper(); // Create mapping for an import dimension
            Mapping map = mapper.CreatePrimitive(sourceTable, targetTable, schema); // Complete mapping (all to all)
            map.Matches.ForEach(m => m.TargetPath.Segments.ForEach(p => p.Add()));

            DcColumn dim = schema.CreateColumn(map.SourceSet.Name, map.SourceSet, map.TargetSet, false);
            dim.Definition.Mapping = map;
            dim.Definition.DefinitionType = DcColumnDefinitionType.LINK;
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

            DcColumn column = importDims.ToList()[0];

            SchemaCsv sourceSchema = (SchemaCsv)Workspace.Schemas.FirstOrDefault(x => x is SchemaCsv);
            DcSchema targetSchema = MashupTop;

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
        public void Wizard_ExportCsv(DcTable table)
        {
            SchemaCsv top = (SchemaCsv)Workspace.Schemas.FirstOrDefault(x => x is SchemaCsv);
            DcSchema schema = MashupTop;

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
            DcColumn[] columns = table.Columns.Where(x => !x.IsSuper).ToArray();

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
                        if (StringSimilarity.SameColumnName(columns[j].Output.Name, "String"))
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


        # region Specific column type operations

        public void Wizard_EditColumnByType(DcColumn column)
        {
            if (column == null) return;

            DcSchema schema = MashupTop;

            if (column.Definition.DefinitionType == DcColumnDefinitionType.FREE)
            {
                return;
            }
            else if (column.Definition.DefinitionType == DcColumnDefinitionType.ARITHMETIC)
            {
                ArithmeticBox dlg = new ArithmeticBox(column, false);
                dlg.Owner = this;
                dlg.ShowDialog(); // Open the dialog box modally 

                if (dlg.DialogResult == false) return; // Cancel

            }
            else if (column.Definition.DefinitionType == DcColumnDefinitionType.LINK)
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
            else if (column.Definition.DefinitionType == DcColumnDefinitionType.AGGREGATION)
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
        public void Wizard_AddArithmetic(DcTable table)
        {
            if (table == null) return;

            DcSchema schema = MashupTop;

            // Create a new column
            DcColumn column = schema.CreateColumn("New Column", table, null, false); // We do not know its output type

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
        public void Wizard_AddPathLink(DcTable sourceTable, DcTable targetTable)
        {
            if (sourceTable == null) return;

            DcSchema schema = MashupTop;

            // Create a new (mapped) dimension using the mapping
            DcColumn column = schema.CreateColumn("New Column", sourceTable, targetTable, false);

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
        public void Wizard_AddColumnLink(DcTable sourceTable, DcTable targetTable)
        {
            DcSchema schema = MashupTop;

            // Create a new (mapped, generating) dimension to the new set
            DcColumn column = schema.CreateColumn("New Column", sourceTable, targetTable, false);

            //
            // Show parameters for set extraction
            //
            List<DcColumn> initialSelection = new List<DcColumn>();
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
            DcTable table = SelectedTable;
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
        public void Wizard_AddAggregation(DcTable table, DcColumn measureColumn)
        {
            if (table == null) return;

            DcSchema schema = MashupTop;

            // Create new aggregated column
            DcColumn column = schema.CreateColumn("My Column", table, null, false);

            // Show recommendations and let the user choose one of them
            AggregationBox dlg = new AggregationBox(column, measureColumn);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            column.Add();

            column.Definition.Evaluate();

            SelectedColumn = column;
        }

        #endregion

        #region Table specific type definitions 

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
        public void Wizard_FilterTable(DcTable table)
        {
            if (table == null) return;

            DcSchema schema = MashupTop;

            // Create a new column (temporary, just to store a where expression which is used in the dialog)
            DcColumn column = schema.CreateColumn("Where Expression", table, schema.GetPrimitive("Boolean"), false);

            // Show dialog for authoring arithmetic expression
            ArithmeticBox whereDlg = new ArithmeticBox(column, true);
            whereDlg.Owner = this;
            whereDlg.ShowDialog(); // Open the dialog box modally 

            ((Set)table).NotifyPropertyChanged(""); // Notify visual components about changes in this column

            // In fact, we have to determine if the column has been really changed and what kind of changes (name change does not require reevaluation)
            table.Definition.Populate();

            SelectedTable = table;
        }

        #endregion

    }
}
