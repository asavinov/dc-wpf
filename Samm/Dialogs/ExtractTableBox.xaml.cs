using System;
using System.Collections.Generic;
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
    /// Interaction logic for ExtractTableBox.xaml
    /// 
    /// This operation is a version of defining a link column. 
    /// Instead of defining a mapping between existing source and target paths, we choose source paths to be used. 
    /// After that, these chosen source paths are used to create the structrure of the target table which in turn are used as target paths in the mapping.
    /// </summary>
    public partial class ExtractTableBox : Window
    {
        bool IsNew { get; set; }

        public CsColumn Column { get; set; }

        public CsTable SourceTable { get; set; } // Lesser set of the generating dimension
        public CsTable TargetTable { get; set; } // New table

        public string NewTableName { get; set; }

        public string NewColumnName { get; set; }

        public List<CsColumn> ProjectionDims { get; set; }

        public void RefreshAll()
        {
            this.GetBindingExpression(ExtractTableBox.DataContextProperty).UpdateTarget(); // Does not work

            newColumnName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            projectionDims.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();

            newTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }

        public ExtractTableBox(CsColumn column, CsColumn srcColumn)
        {
            Column = column;

            NewTableName = column.GreaterSet.Name;

            NewColumnName = column.Name;

            SourceTable = column.LesserSet;
            TargetTable = column.GreaterSet;

            if (TargetTable.SuperDim != null) IsNew = false;
            else IsNew = true;

            // Initialize a list of columns to be used for extraction
            ProjectionDims = new List<CsColumn>();
            ProjectionDims.AddRange(SourceTable.GreaterDims);
            ProjectionDims.Remove(SourceTable.SuperDim);
            if (!IsNew)
            {
                ProjectionDims.Remove(column);
            }

            InitializeComponent();

            if (srcColumn != null)
            {
                projectionDims.SelectedItem = srcColumn;
            }
            if (!IsNew && column.Definition.Mapping != null)
            {
                // Select columns that are used in the current definition (sources of the mapping)
                foreach (var match in column.Definition.Mapping.Matches)
                {
                    CsColumn col = match.TargetPath.FirstSegment;
                    projectionDims.SelectedItems.Add(col); // TODO: Does not work. Maybe use a kind of item.IsSelected=true
                }
            }

            RefreshAll();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewTableName) || string.IsNullOrWhiteSpace(NewColumnName) || projectionDims.SelectedItems.Count == 0) return;

            CsSchema schema = SourceTable.Top;

            // Initialize a list of selected dimensions (from the whole list of all greater dimensions
            List<CsColumn> projDims = new List<CsColumn>();
            foreach (var item in projectionDims.SelectedItems)
            {
                projDims.Add((CsColumn)item);
            }

            // Remove all existing greater dims (clean the target table before defining new structure)
            foreach (CsColumn dim in TargetTable.GreaterDims.ToList())
            {
                if (dim.IsSuper) continue;
                dim.Remove();
            }

            // 1. Create a mapping object from the selected projection dimensions
            // 2. Create identity dimensions for the extracted set and their mapping to the projection dimensions
            Mapping mapping = new Mapping(SourceTable, TargetTable);
            foreach (CsColumn projDim in projDims)
            {
                CsTable idSet = projDim.GreaterSet;
                CsColumn idDim = schema.CreateColumn(projDim.Name, TargetTable, idSet, true);
                idDim.Add();

                mapping.AddMatch(new PathMatch(new DimPath(projDim), new DimPath(idDim)));
            }
            Column.Definition.DefinitionType = ColumnDefinitionType.LINK;
            Column.Definition.Mapping = mapping;
            Column.Definition.IsGenerating = true;
            Column.Name = NewColumnName;

            SourceTable.Definition.DefinitionType = TableDefinitionType.PROJECTION;

            TargetTable.Name = NewTableName;

            // If new then add objects to the schema (or do it outside?)
            if (IsNew)
            {
                schema.AddTable(TargetTable, SourceTable.SuperSet, null);
                Column.Add();
            }
            
            this.DialogResult = true;
        }
    }
}
