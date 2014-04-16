using System;
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

namespace Samm
{
    /// <summary>
    /// Interaction logic for AggregationBox.xaml
    /// TODO:
    /// - Sort fragments by relevance. Non-relevant items at the end and possibly greyed. 
    /// - Display non-relevant fragments as a separate group (at the end of the list): http://stackoverflow.com/questions/4114385/wpf-refine-the-look-of-a-grouped-combobox-vs-a-grouped-datagrid-sample-attach
    /// 
    /// </summary>
    public partial class AggregationBox : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public List<Set> TargetSets { get; set; }

        // It is a number of lists representing fragments the user should choose from
        public RecommendedAggregations Recommendations { get; set; }

        public void NewTargetSet(Set newTargetSet) // Reconfigure the dialog objects for this new target set
        {
            if (newTargetSet == null)
            {
                return;
            }

            Recommendations.TargetSet = newTargetSet;

            // Compute/suggest new recommendations for the new target set
            Recommendations.Clear();
            Recommendations.Recommend();

            // Update the model to be shown in UI
            RefreshAll();
        }

        public void RefreshAll()
        {
            SourceSet.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Recommendations"));
                PropertyChanged(this, new PropertyChangedEventArgs("GroupingPaths"));
                PropertyChanged(this, new PropertyChangedEventArgs("FactSets"));
                PropertyChanged(this, new PropertyChangedEventArgs("MeasurePaths"));
                PropertyChanged(this, new PropertyChangedEventArgs("MeasureTables"));
                PropertyChanged(this, new PropertyChangedEventArgs("MeasureDimensions"));
            }

            GroupingPaths.Items.Refresh();
            FactSets.Items.Refresh();
            MeasurePaths.Items.Refresh();
            MeasureTables.Items.Refresh();
            MeasureDimensions.Items.Refresh();
            AggregationFunctions.Items.Refresh();
        }

        public AggregationBox(Set srcSet, Set dstSet, Dim dstDim)
            : this()
        {
            // Initialize possible target (measure) tables.
            List<Set> all = srcSet.Root.GetAllSubsets();
            foreach(Set lSet in all) 
            {
                if (!lSet.IsLesser(srcSet)) continue; // We want to find all lesser (fact) sets

                // Find all greater sets of this fact set
                foreach (Set gSet in all)
                {
                    if (!gSet.IsGreater(lSet)) continue; // We want to find all greater (measure) sets

                    if (gSet == srcSet) continue;
                    if (gSet.IsPrimitive) continue;

                    if (TargetSets.Contains(gSet)) continue;

                    TargetSets.Add(gSet);
                }
            }

            Recommendations.SourceSet = srcSet;
            Recommendations.TargetSet = dstSet;
            Recommendations.FactSet = null; // Any

            Recommendations.Clear();
            Recommendations.Recommend(); // Generate recommendation

            MeasureTables.SelectedItem = dstSet;

            if (dstDim != null) // Try to set the current measure to the specified dimension
            {
                Recommendations.MeasureDimensions.SelectedObject = dstDim;
            }

            RefreshAll();
        }

        public AggregationBox()
        {
            Recommendations = new RecommendedAggregations();
            TargetSets = new List<Set>();

            InitializeComponent();
        }

        private void GroupingPaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;

            Recommendations.UpdateSelection("GroupingPaths");

            // Refresh
            RefreshAll();
            Recommendations = Recommendations;
        }

        private void FactSets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;

            Recommendations.UpdateSelection("FactSets");

            // Refresh
            RefreshAll();
            Recommendations = Recommendations;
        }

        private void MeasurePaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;

            Recommendations.UpdateSelection("MeasurePaths");

            // Refresh
            RefreshAll();
            Recommendations = Recommendations;
        }

        private void MeasureTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var set = cb.SelectedItem; // or SelectedValue

            if (set == null) return;

            NewTargetSet((Set)set);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

    }

}
