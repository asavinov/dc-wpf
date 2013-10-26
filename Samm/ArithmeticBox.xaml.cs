using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for ArithmeticBox.xaml
    /// 
    /// It should return a valid expression for a calculated column. 
    /// Arithmetics means single-valued expression without set (multi-valued) types. 
    /// We can use dimensions, dot operation, system functions (like string or number conversion), arithmetic and maybe other special operations etc.
    /// 
    /// 
    /// Generally, for any kind of expression, we do only one thing: adding a child node of certain type as input or operand. 
    /// For each new child node, we choose its type first, and then specify type-specific parameters, for example:
    /// - dimension (function). Here we choose one greater dimension within some set. 
    ///   - This of a function is specified as input (so type of input is where dimensions are chosen from). We can write 'this' variable to input or we can write the previous function. 
    ///   - do we need to enter DOT separately? What if we want to have de-projection or projection?
    /// - constant. Here we enter some number or string as an element of some set. Wwhich set? 
    /// - projection or deprojection. These are node types but how do we specify dimension names for them?
    /// - plus, minus etc. do not have additional parameters
    /// 
    /// There can be higher leve methods for defining typical fragments and for them there is a specific UI. 
    /// - Dimension path chooser. This control allows for composing a sequence of dimensions - DimPath. 
    ///   - DimPathListChooserControl - edit a list of dimension segments by choosing each individual segment from a separate (nested) dialog - SegmentChooser. 
    ///   - DimPathTreeChooserControl - edit a dimension path by choosing it from a tree which starts from lesser sets and then proceeds to primitive sets. We can choose last sets/segments. Optionally, we could also choose first segment (although it is not clear if it useful).
    ///   - DimPathGraphChooserControl - pointing to first and last segments in a graphical schema.
    /// 
    /// The same choosers should exist for sets:
    /// - SetChooserControl - choose one set. 
    /// 
    /// We need a dialog for choosing system functions. Thhis control could be also embedded into more complex dialogs, say, for composing an expression.
    /// - FunctionChooserControl. Constraints: input/operand/return types/sets and multiplicity (single or multi-valued), Group name (all functions have predefined groups like string conversion, arithmetics, financial etc.)
    /// - AggregtaionFunctionChooserControl. Here we can specify parameters of grouping and aggregation. We choose an aggregation function (SUM, AVG or custom) which are suitable for certain types (numeric, string etc.) or group
    /// 
    /// UI: should contain a filter bar to restric entries in the list by name
    /// UI: should display context like imposed constraints or custom title/message: Choose dimensions from table <My Table> etc.
    /// UI: should have options for closing, say, must select before closing (see FileChooser or FolderChooser). 
    /// UI: should show textual explanation or formula for the current selection/choice like a formula bar, say, for a path it should be a dot function representation like 'this.f1().f2()' or for projection 'this -> f1 -> (Set1) -> f2 (Set2)
    /// Set constraints for all choosers: super set(s), subset(s), greater sets (easy selection by primitive type), lesser sets, name pattern (starts from, ends with, contains, regular expr pattern, approximate etc.)
    /// Dimensin constraints for all choosers: first segment(s), last segment(s), name pattern (starts from, ends with, contains, regular expr pattern, approximate etc.)
    /// 
    /// 
    /// We want to embed controls in many boxes including a simple dedicated box without own logic. 
    /// A control works independently with its own data context and bound paramters. But they should be visible from the box. 
    /// So a control should be bound to the outer box data. So each time a control is embedded, it has to be bound to external data. 
    /// Solution: 
    /// 1. control is developed independent of its data context and all bindings are relative to its data context. Say, Source table name can be bound to Path=SourceSet.Name but the SourceName property must exist in the data context. 
    /// 2. An outer box must set the data context for the control and this data context must have all properties this control is bound to like SourceSet. 
    /// 3. Alternatively, we can bind individual properties of the control to properties of the box. 
    /// 
    /// 
    /// A typical example:
    /// 1. We want to have a separate path chooser dialog which returns the result (path) in its model
    /// 2. We want to embed a path chooser into a dialog by binding to our model or at least having access to the inner control data model. 
    /// 
    /// How to make a model of a control, a field within an outer dialog which can be accessed from this dialog?
    /// 
    /// </summary>
    public partial class ArithmeticBox : Window
    {
        public Set SourceTable { get; set; }
        public ObservableCollection<Com.Model.Expression> ExpressionModel { get; set; }

        public void RefreshAll()
        {
            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            Operands.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            Operands.Items.Refresh();

            expressionModel.ExpressionTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();
            expressionModel.ExpressionTree.Items.Refresh();
        }

        public ArithmeticBox()
        {
            ExpressionModel = new ObservableCollection<Com.Model.Expression>();

            InitializeComponent();

            Operations.ItemsSource = Enum.GetValues(typeof(Operation));
        }

        private void AddOperation_Click(object sender, RoutedEventArgs e)
        {
            //
            // Determine parent expression
            //
            Com.Model.Expression parentExpr;
            parentExpr = (Com.Model.Expression)expressionModel.ExpressionTree.SelectedItem;
            if (parentExpr == null && ExpressionModel.Count != 0) return; // Nothing is selected

            //
            // Determine operation and create child expression
            //
            Operation op = (Operation)Operations.SelectedItem;
            if (op == null) return; // WARNING: never happens

            var expr = new Com.Model.Expression(op.ToString());
            expr.Operation = op;
            expr.OutputSet = SourceTable.Root.GetPrimitiveSubset("Double");
            expr.OutputSetName = expr.OutputSet.Name;
            expr.OutputIsSetValued = false;

            //
            // Insert new child expression
            //
            if (parentExpr == null) // First exprssion node
            {
                ExpressionModel.Add(expr);
            }
            else
            {
                if (parentExpr.Input == null)
                {
                    parentExpr.Input = expr;
                }
                else
                {
                    parentExpr.AddOperand(expr);
                }
            }

            RefreshAll();
        }

        private void AddOperand_Click(object sender, RoutedEventArgs e)
        {
            //
            // Determine parent expression
            //
            Com.Model.Expression parentExpr;
            parentExpr = (Com.Model.Expression)expressionModel.ExpressionTree.SelectedItem;
            if (parentExpr == null && ExpressionModel.Count != 0) return; // Nothing is selected

            //
            // Determine dimension (function) and create child expression
            //
            Dim op = (Dim)Operands.SelectedItem;
            if (op == null) return;

            var expr = Com.Model.Expression.CreateProjectExpression(new List<Dim> { op }, Operation.DOT);

            //
            // Insert new child expression
            //
            if (parentExpr == null) // First exprssion node
            {
                ExpressionModel.Add(expr);
            }
            else
            {
                if (parentExpr.Input == null)
                {
                    parentExpr.Input = expr;
                }
                else
                {
                    parentExpr.AddOperand(expr);
                }
            }

            RefreshAll();

        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
