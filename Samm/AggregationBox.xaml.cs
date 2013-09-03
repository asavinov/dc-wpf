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
    /// + SourceTable is fixed in context - we do not want to change it (too flexible)
    /// + MeasureTable and column are also fixed in context - they are chosen before the dialog started - they are paramters of the pattern
    /// - What we can change: 
    ///   - fact table (among alternative, possibly empty), 
    ///   + aggregation function (among possible), 
    ///   - alternative grouping paths (to the current facts), 
    ///   - alternative measure paths (from chosen facts to measure)
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
                }
            }
        }

        // Grouping paths
        public RecommendedFragment GroupingPath { get; set; } 
        // Actually, not needed because current selection is flagged in the selected fragment itself
        // TODO: In RecommendedAggregations define for each dimension property Current* where get returns the flagged element and set sets the flag. Then we can bind to this property. 
        // TODO: In RecommendedFragment (a list item) define property DisplayName so that it shows something meaningful for the fragment. For example, it can compute the name on demand if empty and it can be set by the recommender (better).


        // We need a field for the new column name



/*
        public Set SourceTable { get; set; }

        public List<Set> FactTables { get; set; }
        public Set FactTable { get; set; }

        public Set MeasureTable { get; set; }

        public Dim MeasureColumn { get; set; }

        public List<string> AggregationFunctions { get; private set; }

        // public Segments GroupingPath { get; set; }
        public Segments MeasurePath { get; set; }
*/

        public AggregationBox()
        {

/*
            SetRoot db = ((MainWindow)App.Current.MainWindow).DsModel[0];

            List<Set> allSets = db.SubSets.Where(o=>!o.IsPrimitive).ToList();

            FactTables = allSets;

            SourceTable = allSets[0];
            FactTable = null;
            MeasureTable = allSets[0];
            MeasureColumn = MeasureTable.GreaterDims[0];
            
            // Initialize aggregation expression that we are going to edit (either new or load it from an existing derived dimension)

            AggregationFunctions = new List<string>(new string[] { "SUM", "AVG" });

            // Prepare test aggregation expressions
            var gExpr = new Com.Model.Expression("Root (last)"); // Root of expression is that last computed element along a path (so it has to be the last in the list)
            gExpr.OutputSetName = "Last set (root expr)";
            gExpr.Operation = Com.Model.Operation.DEPROJECTION;

            var gExpr2 = new Com.Model.Expression("Before last");
            gExpr2.OutputSetName = "Before last";
            gExpr2.Operation = Com.Model.Operation.DEPROJECTION;
            gExpr.SetInput(gExpr2);

            var gExpr3 = new Com.Model.Expression("Group OFFSET");
            gExpr3.OutputSetName = "this";
            gExpr3.Operation = Com.Model.Operation.VARIABLE;
            gExpr2.SetInput(gExpr3);

            // GroupingPath = new Segments(gExpr);

            var mExpr = new Com.Model.Expression("Measure Root");
            mExpr.OutputSetName = "Measure Set Name";
            mExpr.Operation = Com.Model.Operation.DOT;

            var mExpr2 = new Com.Model.Expression("Second Segment");
            mExpr2.Operation = Com.Model.Operation.DOT;
            mExpr2.OutputSetName = "Measure 2 Set Name";
            mExpr.SetInput(mExpr2);

            var mExpr3 = new Com.Model.Expression("Measure OFFSET");
            mExpr3.Operation = Com.Model.Operation.VARIABLE;
            mExpr3.OutputSetName = "group";
            mExpr2.SetInput(mExpr3);

            MeasurePath = new Segments(mExpr);
*/

            InitializeComponent();
        }

        private void GroupingPaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;

            // Update other fragment selections, sort order and disabled grouping (avoid recursive updates back to this method)
        }

        private void MeasurePaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;

            // Update other fragment selections, sort order and disabled grouping (avoid recursive updates back to this method)
        }

        private void MeasureDimensions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var fragment = cb.SelectedItem; // or SelectedValue

            if (fragment == null) return;

            // Update other fragment selections, sort order and disabled grouping (avoid recursive updates back to this method)
        }
    }

}
