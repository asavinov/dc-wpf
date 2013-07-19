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

namespace Samm
{
    /// <summary>
    /// Interaction logic for AggregationBox.xaml
    /// </summary>
    public partial class AggregationBox : Window
    {
        private Com.Model.Expression aggregationExpresion;

        public List<string> AggregationFunctions { get; private set; }
        
        public Segments GroupingPath { get; set; }
        public Segments MeasurePath { get; set; }

        public AggregationBox()
        {
            // Initialize aggregation expression that we are going to edit (either new or load it from an existing derived dimension)

            AggregationFunctions = new List<string>(new string[] { "SUM", "AVG" });

            // Prepare test aggregation expressions
            var gExpr = new Com.Model.Expression("Root (last)"); // Root of expression is that last computed element along a path (so it has to be the last in the list)
            gExpr.OutputSetName = "Last set (root expr)";
            gExpr.Operation = Com.Model.Operation.INVERSE_FUNCTION;

            var gExpr2 = new Com.Model.Expression("Before last");
            gExpr2.OutputSetName = "Before last";
            gExpr2.Operation = Com.Model.Operation.INVERSE_FUNCTION;
            gExpr.SetInput(gExpr2);

            var gExpr3 = new Com.Model.Expression("Group OFFSET");
            gExpr3.OutputSetName = "First (offset)";
            gExpr3.Operation = Com.Model.Operation.OFFSET;
            gExpr2.SetInput(gExpr3);

            GroupingPath = new Segments(gExpr);

            var mExpr = new Com.Model.Expression("Measure Root");
            mExpr.OutputSetName = "Measure Set Name";
            mExpr.Operation = Com.Model.Operation.FUNCTION;

            var mExpr2 = new Com.Model.Expression("Second Segment");
            mExpr2.Operation = Com.Model.Operation.FUNCTION;
            mExpr2.OutputSetName = "Measure 2 Set Name";
            mExpr.SetInput(mExpr2);

            var mExpr3 = new Com.Model.Expression("Measure OFFSET");
            mExpr3.Operation = Com.Model.Operation.OFFSET;
            mExpr3.OutputSetName = "Offset Name";
            mExpr2.SetInput(mExpr3);

            MeasurePath = new Segments(mExpr);

            InitializeComponent();
        }
    }

}
