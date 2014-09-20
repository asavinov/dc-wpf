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
        bool IsNew { get; set; }
        bool IsImport { get; set; }

        public ComSchema Schema { get; set; }

        public ComColumn Column { get; set; } // Generating projection column with the mapping to be crated/edited

        public string NewTableName { get; set; }

        public string NewColumnName { get; set; }

        public List<ColumnMappingEntry> Entries { get; set; }

        // Target existing columns

        // Target table name (either new or existing, can be edited)

        public void RefreshAll()
        {
            this.GetBindingExpression(ColumnMappingBox.DataContextProperty).UpdateTarget();

            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            //sourceColumn.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

        }

        public ColumnMappingBox(ComSchema targetSchema, ComColumn column, List<ComColumn> initialColumns)
        {
            Schema = targetSchema;
            if (Schema == null) Schema = column.GreaterSet.Schema;
            if (Schema == null) Schema = column.LesserSet.Schema;

            Column = column;

            NewTableName = column.GreaterSet.Name;

            NewColumnName = column.Name;

            if (column.GreaterSet.SuperColumn != null) IsNew = false;
            else IsNew = true;

            if (column.LesserSet.Schema.GetType() == typeof(SetTop)) IsImport = false;
            else IsImport = true;

            List<ComTable> targetTypes = new List<ComTable>();
            targetTypes.Add(Schema.GetPrimitive("Integer"));
            targetTypes.Add(Schema.GetPrimitive("Double"));
            targetTypes.Add(Schema.GetPrimitive("String"));

            // Initialize a list of entries
            Entries = new List<ColumnMappingEntry>();
            if (IsNew)
            {
                if (IsImport)
                {
                    Mapper mapper = new Mapper(); // Create mapping for an import dimension
                    Mapping mapping = mapper.CreatePrimitive(Column.LesserSet, Column.GreaterSet, Schema); // Complete mapping (all to all)

                    CreateEntries(mapping);
                }
                else
                {
                    foreach (ComColumn sourceColumn in Column.LesserSet.Columns)
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

                        entry.TargetTypes = targetTypes;
                        if (sourceColumn.GreaterSet.Name == "Integer")
                            entry.TargetType = Schema.GetPrimitive("Integer");
                        else if (sourceColumn.GreaterSet.Name == "Double")
                            entry.TargetType = Schema.GetPrimitive("Double");
                        else if (sourceColumn.GreaterSet.Name == "String")
                            entry.TargetType = Schema.GetPrimitive("String");

                        Entries.Add(entry);
                    }
                }
            }
            else // Edit
            {
                Mapping existingMapping = Column.Definition.Mapping;
                if (existingMapping == null)
                {
                    existingMapping = new Mapping(Column.LesserSet, Column.GreaterSet);
                }

                CreateEntries(existingMapping);
            }

            InitializeComponent();
        }

        private void CreateEntries(Mapping mapping)
        {
            ComTable sourceTable = mapping.SourceSet;
            ComTable targetTable = mapping.TargetSet;

            List<ComTable> targetTypes = new List<ComTable>();
            targetTypes.Add(Schema.GetPrimitive("Integer"));
            targetTypes.Add(Schema.GetPrimitive("Double"));
            targetTypes.Add(Schema.GetPrimitive("String"));

            foreach (ComColumn sourceColumn in sourceTable.Columns)
            {
                if (sourceColumn.IsSuper) continue;
                if (!sourceColumn.IsPrimitive) continue;
                if (sourceColumn == Column) continue; // Do not include the generating/projection column

                ColumnMappingEntry entry = new ColumnMappingEntry(sourceColumn);

                PathMatch match = mapping.GetMatchForSource(new DimPath(sourceColumn));

                entry.TargetTypes = targetTypes;

                if (match != null)
                {
                    entry.Target = match.TargetPath.FirstSegment;
                    entry.IsMatched = true;
                    entry.IsKey = entry.Target.IsIdentity;

                    entry.TargetType = entry.Target.GreaterSet;
                }
                else
                {
                    entry.Target = null;
                    entry.IsMatched = false;

                    if (IsImport) entry.IsKey = false;
                    else entry.IsKey = true;

                    // Recommend a type
                    if (sourceColumn.GreaterSet.Name == "Integer")
                        entry.TargetType = Schema.GetPrimitive("Integer");
                    else if (sourceColumn.GreaterSet.Name == "Double")
                        entry.TargetType = Schema.GetPrimitive("Double");
                    else if (sourceColumn.GreaterSet.Name == "String")
                        entry.TargetType = Schema.GetPrimitive("String");

                }

                Entries.Add(entry);
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Mapping mapping;
            if (IsNew)
            {
                mapping = new Mapping(Column.LesserSet, Column.GreaterSet);
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
                    targetColumn = Schema.CreateColumn(targetColumnName, Column.GreaterSet, targetType, entry.IsKey);
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
                    if (targetColumn.GreaterSet != entry.TargetType) // Type has been changed
                    {
                        targetColumn.Remove();
                        targetColumn.GreaterSet = entry.TargetType;
                        targetColumn.Add();
                    }
                }
                else // Remains excluded
                {
                }
            }

            Column.Name = NewColumnName;
            Column.GreaterSet.Name = NewTableName;

            if (IsNew)
            {
                Column.Definition.DefinitionType = ColumnDefinitionType.LINK;
                Column.Definition.Mapping = mapping;
                Column.Definition.IsGenerating = true;
                Column.Add();

                Column.GreaterSet.Definition.DefinitionType = TableDefinitionType.PROJECTION;
                Schema.AddTable(Column.GreaterSet, null, null);
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

        public List<ComTable> TargetTypes { get; set; }
        public ComTable TargetType { get; set; }

        public ColumnMappingEntry(ComColumn sourceColumn)
        {
            Source = sourceColumn;

            IsMatched = false;
            IsKey = sourceColumn.IsIdentity;
        }

    }

}
