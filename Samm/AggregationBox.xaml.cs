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

        // It is a number of lists representing fragments the user should choose from
        private RecommendedAggregations _recommendations;
        public RecommendedAggregations Recommendations
        {
            get { return _recommendations; }
            set
            {
                _recommendations = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Recommendations"));
                    PropertyChanged(this, new PropertyChangedEventArgs("GroupingPaths"));
                    PropertyChanged(this, new PropertyChangedEventArgs("FactSets"));
                    PropertyChanged(this, new PropertyChangedEventArgs("MeasurePaths"));
                }
            }
        }

        public void RefreshAll()
        {
            GroupingPaths.Items.Refresh();
            FactSets.Items.Refresh();
            MeasurePaths.Items.Refresh();
            MeasureDimensions.Items.Refresh();
            AggregationFunctions.Items.Refresh();
        }

        public AggregationBox()
        {
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

    }

}
