﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

using Com.Utils;
using Com.Schema;
using Com.Schema.Csv;

namespace Samm.Dialogs
{
    /// <summary>
    /// Interaction logic for ColumnMappingBox.xaml
    /// Differences depending on the usage context:
    /// - Import: by default no keys (maybe hide key column, but key editing can be allowed), Extract: all included all chosen targets are always keys (hide key column or make it disabled with always checked)
    /// - New: Possible/existing target columns are empty: Import: initially all are automatically included. Extract: either no or explicitly specified are initially included
    /// - Edit: initial inclusion is taken from the existing mapping (what about posible targets?)
    /// </summary>
    public partial class ColumnMappingBox : Window, INotifyPropertyChanged
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
                if (SelectedTargetSchema != null)
                {
                    if (SelectedTargetSchema.GetType() == typeof(Schema)) return false;
                    else return true;
                }
                else
                {
                    if (Column == null || Column.Output == null) return false;
                    if (Column.Output.Schema.GetType() == typeof(Schema)) return false;
                    return true;
                }
            }
        }

        //
        // Link column connecting an existing fixed source table with a target table
        //
        public DcColumn Column { get; set; } // Generating projection column with the mapping to be crated/edited
        public string ColumnName { get; set; }

        public ObservableCollection<ColumnMappingEntry> Entries { get; set; } // Describe mapping 

        //
        // Target table including its schema
        //
        public ObservableCollection<DcSchema> TargetSchemas { get; set; }
        public DcSchema SelectedTargetSchema { get; set; }
        public ObservableCollection<DcTable> TargetTables { get; set; }
        public DcTable SelectedTargetTable { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshAll()
        {
            this.GetBindingExpression(ColumnMappingBox.DataContextProperty).UpdateTarget();

            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            //sourceColumn.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            columnMappings.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();
            columnMappings.Items.Refresh();

            targetSchemaList.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
            targetTableList.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
        }

        public ColumnMappingBox(ObservableCollection<DcSchema> targetSchemas, DcColumn column, List<DcColumn> initialColumns)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            //
            // Options and regime of the dialog
            //
            if (column.Input.Columns.Contains(column)) IsNew = false;
            else IsNew = true;

            Column = column;
            ColumnName = column.Name;

            //
            // Init target schema list
            //
            TargetSchemas = targetSchemas;
            TargetTables = new ObservableCollection<DcTable>();

            Entries = new ObservableCollection<ColumnMappingEntry>();
            InitEntries();

            InitializeComponent();

            
            if (IsNew)
            {
                if (initialColumns != null) 
                {
                    // Selection for new column mapping
                    foreach (var entry in Entries) 
                    {
                        if (initialColumns.Contains(entry.Source)) entry.IsMatched = true;
                        else entry.IsMatched = false;
                    }
                }

                if (Column.Output != null)
                {
                    SelectedTargetSchema = column.Output.Schema;
                }
                if (SelectedTargetSchema == null)
                {
                    SelectedTargetSchema = targetSchemas[0];
                }

                targetSchemaList.IsEnabled = true;
                targetTableList.IsEnabled = true;
            }
            else
            {
                targetSchemaList.IsEnabled = false;
                targetTableList.IsEnabled = false;

                SelectedTargetSchema = column.Output.Schema;
                SelectedTargetTable = column.Output;
            }

            RefreshAll();
        }

        private void InitEntries()
        {
            // It is called one time only from constructor

            if (Column.Input == null || Column.Output == null)
            {
                CreateEntries(null);
                return;
            }

            Mapping mapping;
            if (IsNew)
            {
                if (IsImport || IsExport)
                {
                    Mapper mapper = new Mapper(); // Create mapping for an import dimension
                    mapping = mapper.CreatePrimitive(Column.Input, Column.Output, SelectedTargetSchema); // Complete mapping (all to all)
                }
                else // Intra-mashup
                {
                    mapping = new Mapping(Column.Input, Column.Output); // Empty
                }
            }
            else // Edit
            {
                mapping = Column.Definition.Mapping;
                if (mapping == null)
                {
                    mapping = new Mapping(Column.Input, Column.Output); // Empty
                }
            }

            CreateEntries(mapping);
        }

        private void CreateEntries(Mapping mapping)
        {
            Entries.Clear();

            if (mapping == null) return;

            DcTable sourceTable = mapping.SourceSet;
            DcTable targetTable = mapping.TargetSet;

            foreach (DcColumn sourceColumn in sourceTable.Columns)
            {
                if (sourceColumn.IsSuper) continue;
                if (!sourceColumn.IsPrimitive) continue;
                if (sourceColumn == Column) continue; // Do not include the generating/projection column

                ColumnMappingEntry entry = new ColumnMappingEntry(sourceColumn);

                PathMatch match = mapping.GetMatchForSource(new DimPath(sourceColumn));

                if (match != null)
                {
                    entry.Target = match.TargetPath.FirstSegment;
                    entry.TargetType = entry.Target.Output;

                    entry.IsKey = entry.Target.IsKey;

                    entry.IsMatched = true;
                }
                else
                {
                    entry.Target = null;
                    entry.TargetType = null;

                    if (IsImport) entry.IsKey = false;
                    else entry.IsKey = true;

                    entry.IsMatched = false;
                }

                Entries.Add(entry);
            }
        }

        private void FillEntriesTypes()
        {
            // Change available target type lists for each source column without changing the list itself
            // If some current type is not present in the new schema then deselect this entry
            // If an entry is selected then it must have some type selected (it is error if an entry does not have a selected type)

            List<DcTable> targetTypes = GetSchemaTypes();

            DcTable defaultType = null;
            foreach (DcTable table in targetTypes)
            {
                if (StringSimilarity.SameTableName(table.Name, "String"))
                {
                    defaultType = table;
                    break;
                }
            }

            foreach (ColumnMappingEntry entry in Entries)
            {
                entry.TargetTypes.Clear();
                targetTypes.ForEach(x => entry.TargetTypes.Add(x));

                // Now find a good new match for the current type
                // In fact, it should be done by some automatic matching procedure for inter-schema primitive matches

                // Target type has been selected. Try to find the same type
                if (entry.TargetType != null)
                {
                    DcTable targetType = null;
                    foreach (DcTable table in targetTypes)
                    {
                        if (StringSimilarity.SameTableName(table.Name, entry.TargetType.Name))
                        {
                            targetType = table;
                            break; // Found
                        }
                    }
                    if (targetType != null) // Found
                    {
                        entry.TargetType = targetType;
                        continue;
                    }
                }

                // Either not selected or not found. Try to select the source type
                if (entry.Source.Output != null)
                {
                    DcTable targetType = null;
                    foreach (DcTable table in targetTypes)
                    {
                        if (StringSimilarity.SameTableName(table.Name, entry.Source.Output.Name))
                        {
                            targetType = table;
                            break; // Found
                        }
                    }
                    if (targetType != null) // Found
                    {
                        entry.TargetType = targetType;
                        continue;
                    }
                }

                // Nothing helps
                entry.TargetType = defaultType;
            }

        }

        private List<DcTable> GetSchemaTypes() // The user chooses the desired column type from this list
        {
            var targetTypes = new List<DcTable>();
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

        private void TargetSchemaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TargetTables.Clear();
            if (SelectedTargetSchema != null)
            {
                List<DcTable> targetTables;
                if(SelectedTargetSchema == Column.Input.Schema) // Intra-schema link
                {
                    targetTables = MappingModel.GetPossibleGreaterSets(Column.Input);
                }
                else // Import-export link
                {
                    targetTables = SelectedTargetSchema.Root.SubTables;
                }
                targetTables.ForEach(x => TargetTables.Add(x));
            }

            FillEntriesTypes();

            RefreshAll();
        }

        private void TargetTableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Column.Output = SelectedTargetTable;

            InitEntries();

            FillEntriesTypes();
        }

        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            if (SelectedTargetSchema == null) return false;
            if (SelectedTargetTable == null) return false;

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
                DcColumn sourceColumn = entry.Source;

                PathMatch match = mapping.GetMatchForSource(new DimPath(sourceColumn));

                DcColumn targetColumn;

                if (entry.IsMatched && match == null) // Newly added. Creation
                {
                    DcTable targetType = entry.TargetType;
                    string targetColumnName = sourceColumn.Name;

                    // Check if a column with this name already exists
                    targetColumn = Column.Output.GetColumn(targetColumnName);
                    if (targetColumn != null)
                    {
                        targetColumn.Output = targetType; // Alternatively, we can remove it and create a new column below
                    }

                    if (targetColumn == null) // Could not reuse an existing column
                    {
                        targetColumn = SelectedTargetSchema.CreateColumn(targetColumnName, Column.Output, targetType, entry.IsKey);
                        targetColumn.Add();
                    }

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
            //Column.Output.Name = SelectedTargetTable.Name;

            if (IsNew)
            {
                // Target table could contain original columns from previous uses (loaded from csv file or added manually). Now they are not needed.
                foreach (DcColumn targetColumn in Column.Output.Columns)
                {
                    PathMatch match = mapping.GetMatchForTarget(new DimPath(targetColumn));
                    if (match != null) continue;
                    if (targetColumn.Definition.DefinitionType != DcColumnDefinitionType.FREE) continue;
                    if (targetColumn.Definition.DefinitionType != DcColumnDefinitionType.ANY) continue;

                    targetColumn.Remove();
                }

                // Set parameters of the new column
                Column.Definition.DefinitionType = DcColumnDefinitionType.LINK;
                Column.Definition.Mapping = mapping;
                Column.Definition.IsAppendData = true;

                Column.Output.Definition.DefinitionType = DcTableDefinitionType.PROJECTION;
            }

            this.DialogResult = true;
        }

    }

    /// <summary>
    /// It corresponds to one match between a source and a target.
    /// </summary>
    public class ColumnMappingEntry
    {
        public DcColumn Source { get; set; }

        public DcColumn Target { get; set; }

        public bool IsMatched { get; set; }
        public bool IsKey { get; set; }

        public ObservableCollection<DcTable> TargetTypes { get; set; }
        public DcTable TargetType { get; set; }

        public ColumnMappingEntry(DcColumn sourceColumn)
        {
            Source = sourceColumn;

            IsMatched = false;
            IsKey = sourceColumn.IsKey;

            TargetTypes = new ObservableCollection<DcTable>();
        }

    }

}
