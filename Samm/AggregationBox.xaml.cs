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
        public Segments GroupingPath { get; set; }
        public Segments MeasurePath { get; set; }

        public AggregationBox()
        {
            var gExpr = new Com.Model.Expression("Group Root");
            gExpr.OutputSetName = "Output Set Name";
            gExpr.Operation = Com.Model.Operation.FUNCTION;
            GroupingPath = new Segments(gExpr);

            var mExpr = new Com.Model.Expression("Measure Root");
            mExpr.OutputSetName = "Measure Set Name";
            mExpr.Operation = Com.Model.Operation.FUNCTION;
            var mExpr2 = new Com.Model.Expression("Second Segment");
            mExpr2.Operation = Com.Model.Operation.FUNCTION;
            mExpr2.OutputSetName = "Measure 2 Set Name";
            mExpr.Input = mExpr2; mExpr2.ParentExpression = mExpr;
            MeasurePath = new Segments(mExpr);

            InitializeComponent();
        }
    }

}
