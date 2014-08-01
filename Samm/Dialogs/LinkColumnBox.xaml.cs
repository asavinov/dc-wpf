﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
    /// Interaction logic for LinkColumnBox.xaml
    /// </summary>
    public partial class LinkColumnBox : Window
    {
        public string NewColumnName { get; set; }

        public MappingModel MappingModel { get; set; }

        public List<CsTable> TargetTables { get; set; }

        public void RefreshAll()
        {
            this.GetBindingExpression(LinkColumnBox.DataContextProperty).UpdateTarget();

            newColumnName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            sourceTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            targetTables.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            //targetTables.SelectedItem = null; // NewColumnName.GreaterSet;

            sourceTree.MatchTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();
            targetTree.MatchTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();

            // !!! Use it for other controls where we update the data context and need to refresh the view
            sourceTree.GetBindingExpression(TreeView.DataContextProperty).UpdateTarget();
            targetTree.GetBindingExpression(TreeView.DataContextProperty).UpdateTarget();
        }

        public LinkColumnBox(CsTable sourceTable, List<CsTable> targetSets, CsTable targetTable)
            : this()
        {
            //
            // TODO: Suggest the best new type for the new dimension if it has not been specified
            //
            if (targetTable == null)
            {
                // Compare the quality of gest mappings from the the source set to possible target sets

                CsTable bestTargetTable = targetSets[0];
                double bestSimilarity = 0.0;
                /*
                foreach (CsTable set in targetTables)
                {
                   CsColumn dim2 = set.CreateDefaultLesserDimension(dim.Name, dim.LesserSet);

                    List<Mapping> mappings = m.MapDim(new DimPath(dim), new DimPath(dim2));
                    if (mappings[0].Similarity > bestSimilarity) { bestTargetTable = set; bestSimilarity = mappings[0].Similarity; }
                    m.Mappings.Clear();
                }
                */
                targetTable = bestTargetTable;
            }

            // Target table is always chosen. 
            NewColumnName = targetTable.Name;

            MappingModel = new MappingModel(sourceTable, targetTable);

            TargetTables = targetSets;
            targetTables.SelectedItem = targetTable;
        }

        public LinkColumnBox()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void recommendButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addMatchButton_Click(object sender, RoutedEventArgs e)
        {
            MappingModel.AddMatch();

            // Redraw both trees. Use property name like "CanMatch" or String.Empty for all properties (but it does not work because selection is also updated)
            MappingModel.SourceTree.NotifyAllOnPropertyChanged("CanMatch");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("CanMatch");

            MappingModel.SourceTree.NotifyAllOnPropertyChanged("IsMatched");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("IsMatched");
        }

        private void removeMatchButton_Click(object sender, RoutedEventArgs e)
        {
            MappingModel.RemoveMatch();

            // Redraw both trees. Use property name like "CanMatch" or String.Empty for all properties (but it does not work because selection is also updated)
            MappingModel.SourceTree.NotifyAllOnPropertyChanged("CanMatch");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("CanMatch");

            MappingModel.SourceTree.NotifyAllOnPropertyChanged("IsMatched");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("IsMatched");
        }

        private void TargetTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var set = cb.SelectedItem; // or SelectedValue

            if (set == null) return;

            MappingModel.TargetSet = (CsTable)set;

            RefreshAll(); // Refresh
        }

    }

    // Usage of validation rules: http://msdn.microsoft.com/en-us/library/aa969773.aspx
    public class ChangeRangeValidationRule : ValidationRule
    {
        double minMargin;
        double maxMargin;

        public double MinMargin
        {
            get { return this.minMargin; }
            set { this.minMargin = value; }
        }

        public double MaxMargin
        {
            get { return this.maxMargin; }
            set { this.maxMargin = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double margin;

            // Is a number? 
            if (!double.TryParse((string)value, out margin))
            {
                return new ValidationResult(false, "Not a number.");
            }

            // Is in range? 
            if ((margin < this.minMargin) || (margin > this.maxMargin))
            {
                string msg = string.Format("Margin must be between {0} and {1}.", this.minMargin, this.maxMargin);
                return new ValidationResult(false, msg);
            }

            // Number is valid 
            return new ValidationResult(true, null);
        }
    }
}