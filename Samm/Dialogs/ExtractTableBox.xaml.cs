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
    /// </summary>
    public partial class ExtractTableBox : Window
    {
        bool IsNew { get; set; }

        public CsTable SourceTable { get; set; }

        public string NewTableName { get; set; }

        public string NewColumnName { get; set; }

        public List<CsColumn> ProjectionDims { get; set; }

        public ExtractTableBox(CsColumn projectionColumn, CsColumn srcColumn)
        {
            SourceTable = projectionColumn.LesserSet;

            if (SourceTable.SuperDim != null) IsNew = false;
            else IsNew = true;

            NewTableName = projectionColumn.GreaterSet.Name;
            NewColumnName = projectionColumn.Name;

            if (srcColumn != null)
            {
                NewTableName = srcColumn.Name + " Group"; // The new table will have the same name as the only extracted dimension
                NewColumnName = srcColumn.Name + " Group";
            }

            ProjectionDims = new List<CsColumn>();
            ProjectionDims.AddRange(SourceTable.GreaterDims);
            ProjectionDims.Remove(SourceTable.SuperDim);

            InitializeComponent();

            projectionDims.SelectedItem = srcColumn;
        }

        public void RefreshAll()
        {
            this.GetBindingExpression(ExtractTableBox.DataContextProperty).UpdateTarget(); // Does not work

            newColumnName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            projectionDims.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();

            newTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewTableName) || string.IsNullOrWhiteSpace(NewColumnName) || projectionDims.SelectedItems.Count == 0) return;

            // In the case of editing, we need to update: 
            // - existing mapping
            // - existing key greater dims of the set corresponding to the target paths in the mapping
            // Mapping and greater dims must be consistent in the case of projection


            // Initialize a list of selected dimensions (from the whole list of all greater dimensions
            List<CsColumn> projDims = new List<CsColumn>();
            foreach (var item in projectionDims.SelectedItems)
            {
                projDims.Add((CsColumn)item);
            }

/*
            // 1. Create a mapping object from the selected projection dimensions
            // 2. Create identity dimensions for the extracted set and their mapping to the projection dimensions
            Mapping mapping = new Mapping(set, extractedSet);
            foreach (CsColumn projDim in projDims)
            {
                CsTable idSet = projDim.GreaterSet;
                CsColumn idDim = schema.CreateColumn(projDim.Name, extractedSet, idSet, true);
                idDim.Add();

                mapping.AddMatch(new PathMatch(new DimPath(projDim), new DimPath(idDim)));
            }

            extractedDim.Definition.Mapping = mapping;
            extractedDim.Definition.IsGenerating = true;


            // If new then add objects to the schema (or do it outside?)
            schema.AddTable(extractedSet, set.SuperSet, null);
            extractedDim.Add();

*/
            
            this.DialogResult = true;
        }
    }
}
