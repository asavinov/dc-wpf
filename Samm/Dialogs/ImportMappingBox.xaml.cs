using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Com.Model;

namespace Samm.Dialogs
{
    /// <summary>
    /// Interaction logic for ImportMappingBox.xaml
    /// </summary>
    public partial class ImportMappingBox : Window
    {
        bool IsNew { get; set; }

        public ComSchema SourceSchema { get; set; }
        public ComColumn Column { get; set; } // Generating projection column with the mapping to be crated/edited
        public ComSchema TargetSchema { get; set; }


        public string NewColumnName { get; set; }

        public string NewTableName { get; set; }


        public ObservableCollection<ImportMappingEntry> SourceColumnEntries { get; set; }

        // Target existing columns

        // Target table name (either new or existing, can be edited)

        public void RefreshAll()
        {
            this.GetBindingExpression(ImportMappingBox.DataContextProperty).UpdateTarget();

            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            //sourceColumn.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

        }

        public ImportMappingBox(ComSchema sourceSchema, ComSchema targetSchema, ComColumn column, List<ComColumn> initialColumns)
        {
            SourceColumnEntries = new ObservableCollection<ImportMappingEntry>();

            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);
            this.chooseSourceCommand = new DelegateCommand(this.ChooseSourceCommand_Executed, this.ChooseSourceCommand_CanExecute);

            if (column.Output.SuperColumn != null) IsNew = false;
            else IsNew = true;

            SourceSchema = sourceSchema;
            Column = column;
            TargetSchema = targetSchema;

            if (!IsNew)
            {
                hasHeaderRecord.IsChecked = ((SetCsv)Column.Input).HasHeaderRecord;
                decimalChar.SelectedItem = ((SetCsv)Column.Input).Delimiter;
            }

            Initialize();

            InitializeComponent();
        }

        private void Initialize() // Initialize dialog after context change (source table or column)
        {
            NewTableName = Column.Output.Name;

            NewColumnName = Column.Name;

            SourceColumnEntries.Clear();

            if (Column.Input == null)
            {
                return; // Empty list if no source table
            }

            // Find a mapping
            Mapping mapping;
            if (IsNew)
            {
                if (Column.Definition.Mapping != null)
                {
                    mapping = Column.Definition.Mapping;
                }
                else
                {
                    Mapper mapper = new Mapper();
                    mapping = mapper.CreatePrimitive(Column.Input, Column.Output, TargetSchema); // Complete mapping (all to all)
                }
            }
            else // Edit
            {
                mapping = Column.Definition.Mapping;
                if (mapping == null)
                {
                    mapping = new Mapping(Column.Input, Column.Output);
                }

            }

            // Initialize a list of entries

            List<ComTable> targetTypes = new List<ComTable>();
            targetTypes.Add(TargetSchema.GetPrimitive("Integer"));
            targetTypes.Add(TargetSchema.GetPrimitive("Double"));
            targetTypes.Add(TargetSchema.GetPrimitive("String"));

            foreach (ComColumn sourceColumn in mapping.SourceSet.Columns)
            {
                if (sourceColumn.IsSuper) continue;
                if (!sourceColumn.IsPrimitive) continue;
                if (sourceColumn == Column) continue; // Do not include the generating/projection column

                ImportMappingEntry entry = new ImportMappingEntry(sourceColumn);

                PathMatch match = mapping.GetMatchForSource(new DimPath(sourceColumn));

                entry.TargetTypes = targetTypes;

                if (match != null)
                {
                    entry.Target = match.TargetPath.FirstSegment;
                    entry.IsMatched = true;
                    entry.IsKey = entry.Target.IsKey;

                    entry.TargetType = entry.Target.Output;
                }
                else
                {
                    entry.Target = null;
                    entry.IsMatched = false;

                    entry.IsKey = false;

                    // Recommend a type
                    if (sourceColumn.Output.Name == "Integer")
                        entry.TargetType = TargetSchema.GetPrimitive("Integer");
                    else if (sourceColumn.Output.Name == "Double")
                        entry.TargetType = TargetSchema.GetPrimitive("Double");
                    else if (sourceColumn.Output.Name == "String")
                        entry.TargetType = TargetSchema.GetPrimitive("String");

                }

                SourceColumnEntries.Add(entry);
            }

        }

        private readonly ICommand chooseSourceCommand;
        public ICommand ChooseSourceCommand
        {
            get { return this.chooseSourceCommand; }
        }
        private bool ChooseSourceCommand_CanExecute(object state)
        {
            return true;
        }
        private void ChooseSourceCommand_Executed(object state)
        {
            if (SourceSchema.GetType() == typeof(SchemaCsv))
            {
                var ofg = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
                //ofg.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
                ofg.Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*";
                ofg.RestoreDirectory = true;
                ofg.CheckFileExists = true;
                ofg.Multiselect = false;

                Nullable<bool> result = ofg.ShowDialog();
                if (result != true) return;

                string filePath = ofg.FileName;
                string safeFilePath = ofg.SafeFileName;
                string fileDir = System.IO.Path.GetDirectoryName(filePath);
                string tableName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                //
                // Create a source table and load its structure into the schema
                //
                // TODO: Check that this table has not been loaded yet (compare paths). Or do it in dialog. 
                // If already exists then either allow for imports into different local tables (with different parameters along different projection dimensions) or reuse the existing imported table.
                SetCsv sourceTable = (SetCsv)SourceSchema.CreateTable(tableName);
                sourceTable.FilePath = filePath;

                //sourceTable.HasHeaderRecord = (bool)hasHeaderRecord.IsChecked;

                ((SchemaCsv)SourceSchema).LoadSchema(sourceTable);

                Column.Input = sourceTable;

                // TODO: Check if the chosen source file/table is different from the current
                // TODO: Check if the chosen source file/table already exists in the source schema as an object, e.g., if it has been imported already (we can import one file more than one time)
                // TODO: Create table and initialize its column structure

                Initialize(); // Initialize the dialog
                RefreshAll();
            }
        }

        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            if (string.IsNullOrWhiteSpace(newTableName.Text)) return false;

            // At least one column has to be selected
            bool selected = false;
            foreach (var entry in SourceColumnEntries)
            {
                if (entry.IsMatched)
                {
                    selected = true;
                    break;
                }
            }

            return selected;
        }
        private void OkCommand_Executed(object state)
        {
            Mapping mapping;
            if (IsNew)
            {
                mapping = new Mapping(Column.Input, Column.Output);
            }
            else
            {
                mapping = Column.Definition.Mapping;
            }

            // For each entry decide what to do with the corresponding 1. match in the mapping 2. target column, depending on the comparision with the existing 1. match, target column
            foreach (var entry in SourceColumnEntries)
            {
                ComColumn sourceColumn = entry.Source;

                PathMatch match = mapping.GetMatchForSource(new DimPath(sourceColumn));

                ComColumn targetColumn;

                if (entry.IsMatched && match == null) // Newly added. Creation
                {
                    ComTable targetType = entry.TargetType;
                    string targetColumnName = sourceColumn.Name;
                    targetColumn = TargetSchema.CreateColumn(targetColumnName, Column.Output, targetType, entry.IsKey);
                    targetColumn.Add();

                    mapping.AddMatch(new PathMatch(new DimPath(sourceColumn), new DimPath(targetColumn)));
                }
                else if (!entry.IsMatched && match != null) // Newly removed. Deletion.
                {
                    targetColumn = match.TargetPath.FirstSegment;
                    targetColumn.Remove();

                    mapping.RemoveMatch(match.SourcePath, match.TargetPath);
                }
                else if (entry.IsMatched) // Remains included. Update properties (name, key, type etc.)
                {
                    targetColumn = match.TargetPath.FirstSegment;
                    if (targetColumn.Output != entry.TargetType) // Type has been changed
                    {
                        targetColumn.Remove();
                        targetColumn.Output = entry.TargetType;
                        targetColumn.Add();
                    }
                }
                else // Remains excluded
                {
                }
            }

            Column.Name = NewColumnName;
            Column.Output.Name = NewTableName;

            if (IsNew)
            {
                Column.Definition.DefinitionType = ColumnDefinitionType.LINK;
                Column.Definition.Mapping = mapping;
                Column.Definition.IsAppendData = true;
                Column.Add();

                Column.Output.Definition.DefinitionType = TableDefinitionType.PROJECTION;
                TargetSchema.AddTable(Column.Output, null, null);
            }

            this.DialogResult = true;
        }

    }

    /// <summary>
    /// It corresponds to one match between a source and a target.
    /// </summary>
    public class ImportMappingEntry
    {
        public ComColumn Source { get; set; }

        public ComColumn Target { get; set; }

        public bool IsMatched { get; set; }
        public bool IsKey { get; set; }

        public List<ComTable> TargetTypes { get; set; }
        public ComTable TargetType { get; set; }

        public ImportMappingEntry(ComColumn sourceColumn)
        {
            Source = sourceColumn;

            IsMatched = false;
            IsKey = sourceColumn.IsKey;
        }

    }

}
