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
    /// Interaction logic for ColumnMappingBox.xaml
    /// Differences depending on the usage context:
    /// - Import: by default no keys (maybe hide key column, but key editing can be allowed), Extract: all included all chosen targets are always keys (hide key column or make it disabled with always checked)
    /// - New: Possible/existing target columns are empty: Import: initially all are automatically included. Extract: either no or explicitly specified are initially included
    /// - Edit: initial inclusion is taken from the existing mapping (what about posible targets?)
    /// </summary>
    public partial class ColumnMappingBox : Window
    {
        //
        // Options defining the regime of the dialog
        //
        bool IsNew { get; set; } // Set in constructor
        bool IsImport // If source is not mashup (changes for each schema selection)
        {
            get 
            { 
                if (Column == null || Column.Input == null) return false; 
                if (Column.Input.Schema.GetType() == typeof(Schema)) return false;
                return true;
            }
        }
        bool IsExport // If target is not mashup (changes for each schema selection)
        {
            get
            {
                if (Column == null || Column.Output == null) return false;
                if (Column.Output.Schema.GetType() == typeof(Schema)) return false;
                return true;
            }
        }

        //
        // Link column connecting an existing fixed source table with a target table
        //
        public ComColumn Column { get; set; } // Generating projection column with the mapping to be crated/edited
        public string ColumnName { get; set; }

        public ObservableCollection<ColumnMappingEntry> Entries { get; set; } // Describe mapping 


        //
        // Target table including its schema
        //
        public ObservableCollection<ComSchema> TargetSchemas { get; set; }
        public ComSchema SelectedTargetSchema { get; set; }
        public string TargetTableName { get; set; } // Table to be created if new and existing table name if edit operation


        public void RefreshAll()
        {
            this.GetBindingExpression(ColumnMappingBox.DataContextProperty).UpdateTarget();

            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            //sourceColumn.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

        }

        public ColumnMappingBox(ObservableCollection<ComSchema> targetSchemas, ComColumn column, List<ComColumn> initialColumns)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            //
            // Options and regime of the dialog
            //
            if (column.Output.SuperColumn != null) IsNew = false;
            else IsNew = true;

            //
            // Column
            //
            if (column != null && column.Output != null)
            {
                SelectedTargetSchema = column.Output.Schema;
            }
            if (SelectedTargetSchema == null)
            {
                SelectedTargetSchema = targetSchemas[0];
            }

            Column = column;
            ColumnName = column.Name;

            //
            // Target table including its schema
            //
            TargetSchemas = targetSchemas;
            TargetTableName = column.Output.Name;

            //
            // Initialize a list of entries representing a mapping
            //
            Entries = new ObservableCollection<ColumnMappingEntry>();

            if (IsNew)
            {
                if (IsImport)
                {
                    Mapper mapper = new Mapper(); // Create mapping for an import dimension
                    Mapping mapping = mapper.CreatePrimitive(Column.Input, Column.Output, SelectedTargetSchema); // Complete mapping (all to all)

                    CreateEntries(mapping);
                }
                else
                {
                    foreach (ComColumn sourceColumn in Column.Input.Columns)
                    {
                        if (sourceColumn.IsSuper) continue;
                        if (!sourceColumn.IsPrimitive) continue;
                        if (sourceColumn == Column) continue; // Do not include the generating/projection column

                        ColumnMappingEntry entry = new ColumnMappingEntry(sourceColumn);

                        // Use parameter to select some columns
                        if (initialColumns != null)
                        {
                            if (initialColumns.Contains(sourceColumn)) entry.IsMatched = true;
                            else entry.IsMatched = false;
                        }
                        else // No selection parameters
                        {
                            if (IsImport) entry.IsMatched = true;
                            else entry.IsMatched = false;
                        }

                        if (IsImport) entry.IsKey = false;
                        else entry.IsKey = true;

                        var targetTypes = GetSchemaTypes();
                        targetTypes.ForEach(x => entry.TargetTypes.Add(x));

                        if (sourceColumn.Output.Name == "Integer")
                            entry.TargetType = SelectedTargetSchema.GetPrimitive("Integer");
                        else if (sourceColumn.Output.Name == "Double")
                            entry.TargetType = SelectedTargetSchema.GetPrimitive("Double");
                        else if (sourceColumn.Output.Name == "String")
                            entry.TargetType = SelectedTargetSchema.GetPrimitive("String");

                        Entries.Add(entry);
                    }
                }
            }
            else // Edit
            {
                Mapping existingMapping = Column.Definition.Mapping;
                if (existingMapping == null)
                {
                    existingMapping = new Mapping(Column.Input, Column.Output);
                }

                CreateEntries(existingMapping);
            }

            InitializeComponent();

            if (IsNew)
            {
                schemaList.IsEnabled = true;
            }
            else
            {
                schemaList.IsEnabled = false;
            }
        }

        private List<ComTable> GetSchemaTypes() // The user chooses the desired column type from this list
        {
            var targetTypes = new List<ComTable>();
            if (SelectedTargetSchema == null)
            {
                ;
            }
            if (SelectedTargetSchema is SchemaCsv)
            {
                targetTypes.Add(SelectedTargetSchema.GetPrimitive("String"));
            }
            else
            {
                targetTypes.Add(SelectedTargetSchema.GetPrimitive("Integer"));
                targetTypes.Add(SelectedTargetSchema.GetPrimitive("Double"));
                targetTypes.Add(SelectedTargetSchema.GetPrimitive("String"));
            }

            return targetTypes;
        }

        private void CreateEntries(Mapping mapping)
        {
            ComTable sourceTable = mapping.SourceSet;
            ComTable targetTable = mapping.TargetSet;

            List<ComTable> targetTypes = GetSchemaTypes();

            foreach (ComColumn sourceColumn in sourceTable.Columns)
            {
                if (sourceColumn.IsSuper) continue;
                if (!sourceColumn.IsPrimitive) continue;
                if (sourceColumn == Column) continue; // Do not include the generating/projection column

                ColumnMappingEntry entry = new ColumnMappingEntry(sourceColumn);

                PathMatch match = mapping.GetMatchForSource(new DimPath(sourceColumn));

                targetTypes.ForEach(x => entry.TargetTypes.Add(x));

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

                    if (IsImport) entry.IsKey = false;
                    else entry.IsKey = true;

                    // Recommend a type
                    if (sourceColumn.Output.Name == "Integer")
                        entry.TargetType = SelectedTargetSchema.GetPrimitive("Integer");
                    else if (sourceColumn.Output.Name == "Double")
                        entry.TargetType = SelectedTargetSchema.GetPrimitive("Double");
                    else if (sourceColumn.Output.Name == "String")
                        entry.TargetType = SelectedTargetSchema.GetPrimitive("String");

                }

                Entries.Add(entry);
            }
        }

        private void SchemaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // We can change target schema for new tables only. 
            // In this case, we need to change target types of all entries

            Entries.Clear();

            Mapper mapper = new Mapper(); // Create mapping for an import dimension
            Mapping mapping = mapper.CreatePrimitive(Column.Input, Column.Output, SelectedTargetSchema); // Complete mapping (all to all)

            CreateEntries(mapping);
        }

        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            if (string.IsNullOrWhiteSpace(targetTableName.Text)) return false;

            if (string.IsNullOrWhiteSpace(columnName.Text)) return false;
            
            bool selected = false;
            foreach (var entry in Entries)
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
            foreach (var entry in Entries)
            {
                ComColumn sourceColumn = entry.Source;

                PathMatch match = mapping.GetMatchForSource(new DimPath(sourceColumn));

                ComColumn targetColumn;

                if (entry.IsMatched && match == null) // Newly added. Creation
                {
                    ComTable targetType = entry.TargetType;
                    string targetColumnName = sourceColumn.Name;
                    targetColumn = SelectedTargetSchema.CreateColumn(targetColumnName, Column.Output, targetType, entry.IsKey);
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

            Column.Name = ColumnName;
            Column.Output.Name = TargetTableName;

            if (IsNew)
            {
                Column.Definition.DefinitionType = ColumnDefinitionType.LINK;
                Column.Definition.Mapping = mapping;
                Column.Definition.IsAppendData = true;
                Column.Add();

                Column.Output.Definition.DefinitionType = TableDefinitionType.PROJECTION;
                SelectedTargetSchema.AddTable(Column.Output, null, null);
            }

            this.DialogResult = true;
        }

    }

    /// <summary>
    /// It corresponds to one match between a source and a target.
    /// </summary>
    public class ColumnMappingEntry
    {
        public ComColumn Source { get; set; }

        public ComColumn Target { get; set; }

        public bool IsMatched { get; set; }
        public bool IsKey { get; set; }

        public ObservableCollection<ComTable> TargetTypes { get; set; }
        public ComTable TargetType { get; set; }

        public ColumnMappingEntry(ComColumn sourceColumn)
        {
            Source = sourceColumn;

            IsMatched = false;
            IsKey = sourceColumn.IsKey;

            TargetTypes = new ObservableCollection<ComTable>();
        }

    }

}
