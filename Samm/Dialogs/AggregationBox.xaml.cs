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

        DcColumn Column { get; set; }

        public DcTable SourceTable { get; set; }

        public List<DcTable> FactTables { get; set; }
        public DcTable FactTable { get; set; }

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
                PropertyChanged(this, new PropertyChangedEventArgs("FactTable"));

                PropertyChanged(this, new PropertyChangedEventArgs("GroupingPaths"));
                PropertyChanged(this, new PropertyChangedEventArgs("GroupingPath"));
                PropertyChanged(this, new PropertyChangedEventArgs("MeasurePaths"));
                PropertyChanged(this, new PropertyChangedEventArgs("MeasurePath"));
                PropertyChanged(this, new PropertyChangedEventArgs("AggregationFunctions"));
                PropertyChanged(this, new PropertyChangedEventArgs("AggregationFunction"));
            }

            factTables.GetBindingExpression(ListView.ItemsSourceProperty).UpdateTarget();
            //factTables.Items.Refresh();

            groupingPaths.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            groupingPaths.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
            //groupingPaths.Items.Refresh();

            measurePaths.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            measurePaths.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
            //measurePaths.Items.Refresh();

            aggregationFunctions.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            aggregationFunctions.GetBindingExpression(ComboBox.SelectedItemProperty).UpdateTarget();
            //aggregationFunctions.Items.Refresh();
        }

        public AggregationBox(DcColumn column, DcColumn measureColumn)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            InitializeComponent();

            if (column.Input.Columns.Contains(column)) IsNew = false;
            else IsNew = true;

            Column = column;
            SourceTable = column.Input;

            newColumnName.Text = Column.Name;

            // Initialize all possible fact tables (lesser tables)
            FactTables = MappingModel.GetPossibleLesserSets(SourceTable);
            if (!IsNew)
            {
                FactTable = Column.Definition.FactTable;
            }
            else
            {
                if (FactTables.Count == 1) FactTable = FactTables[0];
            }
            // By setting a fact table here we trigger item selection event where two controls will be filled: grouping paths and measure paths.

            // Use additional parameter for selecting desired measure
            if (IsNew && measureColumn != null)
            {
                // Find at least one fact table that has a measure path to this measure column
                foreach (DcTable table in FactTables)
                {
                    var pathEnum = new PathEnumerator(
                        table,
                        measureColumn.Output, 
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
                AggregationFunction = Column.Definition.Updater;
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

            //
            // Initialize grouping paths. Paths from the fact set to the source set
            //
            var grPaths = new PathEnumerator((DcTable)factTable, SourceTable, DimensionType.IDENTITY_ENTITY);
            GroupingPaths = grPaths.ToList<DimPath>();

            // Select some grouping path item
            if (!IsNew && factTable == Column.Definition.FactTable) // If the fact table as defined then grouping path also as defined (i.e., show the current definition)
            {
                foreach (var p in GroupingPaths) // Definition can store a different instance of the same path (so either override Equals for DimPath or compare manually)
                {
                    if (!p.SamePath(Column.Definition.GroupPaths[0])) continue;
                    GroupingPath = p;
                    break;
                }
            }
            else // Recommend best grouping path: single choise, shortest path etc.
            {
                if (GroupingPaths.Count == 1) GroupingPath = GroupingPaths[0];
            }

            //
            // Initialize measure paths. Paths from the fact set to numeric sets
            //
            DcSchema schema = ((DcTable)factTable).Schema;
            var mePaths = new PathEnumerator(
                new List<DcTable>(new DcTable[] { (DcTable)factTable }),
                new List<DcTable>(new DcTable[] { schema.GetPrimitive("Integer"), schema.GetPrimitive("Double") }),
                false,
                DimensionType.IDENTITY_ENTITY
               );
            MeasurePaths = mePaths.ToList<DimPath>();

            // Select some measure path item
            if (!IsNew && factTable == Column.Definition.FactTable)
            {
                foreach (var p in MeasurePaths) // Definition can store a different instance of the same path (so either override Equals for DimPath or compare manually)
                {
                    if (!p.SamePath(Column.Definition.MeasurePaths[0])) continue;
                    MeasurePath = p;
                    break;
                }
            }
            else
            {
                if (MeasurePaths.Count == 1) MeasurePath = MeasurePaths[0];
            }

            RefreshAll();
        }

        private void GroupingPaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;
        }

        private void MeasurePaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;
        }

        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            if (string.IsNullOrWhiteSpace(newColumnName.Text)) return false;

            if (factTables.SelectedItems.Count == 0) return false;

            if (groupingPaths.SelectedItem == null) return false;

            if (measurePaths.SelectedItem == null && aggregationFunctions.SelectedItem != "COUNT") return false;

            if (aggregationFunctions.SelectedItem == null) return false;

            return true;
        }
        private void OkCommand_Executed(object state)
        {
            DcSchema schema = Column.Input.Schema;

            // Column name
            Column.Name = newColumnName.Text;

            // Column type
            DcTable targetTable = null;
            if (AggregationFunction == "COUNT")
            {
                targetTable = schema.GetPrimitive("Integer"); ;
            }
            else
            {
                targetTable = MeasurePath.Output; // The same as the measure path
            }
            Column.Output = targetTable;

            // Column definition
            Column.Definition.DefinitionType = DcColumnDefinitionType.AGGREGATION;
            Column.Definition.FactTable = FactTable;
            Column.Definition.GroupPaths.Clear();
            Column.Definition.GroupPaths.Add(GroupingPath);
            Column.Definition.MeasurePaths.Clear();
            Column.Definition.MeasurePaths.Add(MeasurePath);
            Column.Definition.Updater = AggregationFunction;

            this.DialogResult = true;
        }
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
        }

    }

}
