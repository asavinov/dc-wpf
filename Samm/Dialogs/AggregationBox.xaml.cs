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

using Com.Model;

namespace Samm.Dialogs
{
    /// <summary>
    /// Interaction logic for AggregationBox.xaml
    /// TODO:
    /// - Automatically selecting the best alternative and a single alternative. 
    /// - Computing alternatives depending on aggregation function. Say, for COUNT, measure path is not used at all and should be disabled.
    /// - Sort alternatives by relevance. Non-relevant items at the end. 
    /// - Mark/visualize relevance. Non-relevant items gray or visualized in a separate (last) group in the list: http://stackoverflow.com/questions/4114385/wpf-refine-the-look-of-a-grouped-combobox-vs-a-grouped-datagrid-sample-attach
    /// </summary>
    public partial class AggregationBox : Window, INotifyPropertyChanged
    {
        bool IsNew { get; set; }

        CsColumn Column { get; set; }

        public CsTable SourceTable { get; set; }

        public List<CsTable> FactTables { get; set; }
        public CsTable FactTable { get; set; }

        public List<DimPath> GroupingPaths { get; set; } // Alternative paths from the fact to the source
        public DimPath GroupingPath { get; set; }

        public List<DimPath> MeasurePaths { get; set; } // Alternative paths from the fact to a numeric table
        public DimPath MeasurePath { get; set; }

        public List<string> AggregationFunctions { get; set; } // Updaters
        public string AggregationFunction { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshAll()
        {
            sourceTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("FactTables"));
                PropertyChanged(this, new PropertyChangedEventArgs("GroupingPaths"));
                PropertyChanged(this, new PropertyChangedEventArgs("GroupingPath"));
                PropertyChanged(this, new PropertyChangedEventArgs("MeasurePaths"));
                PropertyChanged(this, new PropertyChangedEventArgs("MeasurePath"));
                PropertyChanged(this, new PropertyChangedEventArgs("AggregationFunctions"));
            }

            factTables.Items.Refresh();
            groupingPaths.Items.Refresh();
            measurePaths.Items.Refresh();
            aggregationFunctions.Items.Refresh();
        }

        public AggregationBox(CsColumn column, CsColumn measureColumn)
        {
            InitializeComponent();

            if (column.LesserSet.GreaterDims.Contains(column)) IsNew = false;
            else IsNew = true;

            Column = column;
            SourceTable = column.LesserSet;

            // Initialize all possible fact tables (lesser tables)
            FactTables = MappingModel.GetPossibleLesserSets(SourceTable);
            if (!IsNew)
            {
                FactTable = Column.ColumnDefinition.FactTable;
            }
            else
            {
                if (FactTables.Count == 1) FactTable = FactTables[0];
            }

            // Initialize all possible grouping paths
            GroupingPaths = new List<DimPath>();
            if (!IsNew)
            {
                GroupingPath = Column.ColumnDefinition.GroupPaths[0];
            }
            else
            {
                if (GroupingPaths.Count == 1) GroupingPath = GroupingPaths[0];
            }

            // Initialize all possible measure paths
            MeasurePaths = new List<DimPath>();
            if (!IsNew)
            {
                MeasurePath = Column.ColumnDefinition.MeasurePaths[0];
            }
            else
            {
                if (MeasurePaths.Count == 1) MeasurePath = MeasurePaths[0];
            }
            
            if (measureColumn != null)
            {
                // Find at least one fact table that has a measure path to this measure column
                foreach (CsTable table in FactTables)
                {
                    var pathEnum = new PathEnumerator(
                        table, 
                        measureColumn.GreaterSet, 
                        DimensionType.IDENTITY_ENTITY
                        );

                    var paths = pathEnum.ToList();
                    if (paths.Count() == 0) continue;

                    FactTable = table; // Here the list of measure paths has to be filled automatically
                    MeasurePath = paths[0];
                }
            }

            // Initialize aggregation functions
            AggregationFunctions = new List<string>(new string[] { "COUNT", "SUM", "MUL" });
            if (!IsNew)
            {
                AggregationFunction = Column.ColumnDefinition.Updater;
            }
            else
            {
                AggregationFunction = "SUM";
            }

            RefreshAll();
        }

        private void FactTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lv = (ListView)e.Source; // or sender
            var factTable = lv.SelectedItem; // or SelectedValue

            if (factTable == null) return;

            // Paths from the fact set to the source set
            var grPaths = new PathEnumerator((CsTable)factTable, SourceTable, DimensionType.IDENTITY_ENTITY);
            GroupingPaths = grPaths.ToList<DimPath>();
            if (GroupingPaths.Count == 1) GroupingPath = GroupingPaths[0];

            // Paths from the fact set to numeric sets
            CsSchema schema = ((CsTable)factTable).Top;
            var mePaths = new PathEnumerator(
                new List<CsTable>(new CsTable[] { (CsTable)factTable }),
                new List<CsTable>(new CsTable[] { schema.GetPrimitive("Integer"), schema.GetPrimitive("Double") }),
                false,
                DimensionType.IDENTITY_ENTITY
               );

            MeasurePaths = mePaths.ToList<DimPath>();
            if (MeasurePaths.Count == 1) MeasurePath = MeasurePaths[0];

            // Refresh
            RefreshAll();
        }

        private void GroupingPaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;

            //
            // TODO: other lists depend on this selection, so update path lists
            //

            // Refresh
            RefreshAll();
        }

        private void MeasurePaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;

            //
            // TODO: other lists depend on this selection, so update path lists
            //

            // Refresh
            RefreshAll();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            CsSchema schema = Column.LesserSet.Top;

            // Column name
            Column.Name = newColumnName.Text;

            // Column type
            CsTable targetTable = null;
            if (AggregationFunction == "COUNT")
            {
                targetTable = schema.GetPrimitive("Integer"); ;
            }
            else
            {
                targetTable = MeasurePath.GreaterSet; // The same as the measure path
            }
            Column.GreaterSet = targetTable;

            // Column definition
            Column.ColumnDefinition.FactTable = FactTable;
            Column.ColumnDefinition.GroupPaths.Add(GroupingPath);
            Column.ColumnDefinition.MeasurePaths.Add(MeasurePath);
            Column.ColumnDefinition.Updater = AggregationFunction;

            this.DialogResult = true;
        }

    }

}
