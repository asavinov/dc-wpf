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

namespace Samm.Dialogs
{
    /// <summary>
    /// Interaction logic for ArithmeticBox.xaml
    /// 
    /// The task of this dialog is to create/edit a valid expression that can be used in a column definition. 
    /// This dialog is intended for authoring arithmetic expressions and hence its output is a valid arithmetic expression. 
    /// Particularly, the user can use arithmetic operations for primitive values. Primitive values or column composition are used as operands. 
    /// 
    /// Columns are added as direct greater dimensions or as dimension paths in the case of composition. 
    /// For choosing path, either a list of all paths is provided or a tree of primitive paths. 
    /// 
    /// Also, we might want to have a possibility to choose a type of expression, i.e., apply some conversion. 
    /// Also, system functions could be used for processing results. 
    /// Possible operands (including columns) are chosen depending on the operation. 
    /// 
    /// UI: should contain a filter bar to restric entries in the list by name
    /// UI: should display context like imposed constraints or custom title/message: Choose dimensions from table <My Table> etc.
    /// UI: should have options for closing, say, must select before closing (see FileChooser or FolderChooser). 
    /// UI: should show textual explanation or formula for the current selection/choice like a formula bar, say, for a path it should be a dot function representation like 'this.f1().f2()' or for projection 'this -> f1 -> (Set1) -> f2 (Set2)
    /// Set constraints for all choosers: super set(s), subset(s), greater sets (easy selection by primitive type), lesser sets, name pattern (starts from, ends with, contains, regular calcExpr pattern, approximate etc.)
    /// Dimensin constraints for all choosers: first segment(s), last segment(s), name pattern (starts from, ends with, contains, regular calcExpr pattern, approximate etc.)
    /// 
    /// </summary>
    public partial class ArithmeticBox : Window
    {
        public CsTable SourceTable { get; set; }

        public List<DimPath> SourcePaths { get; set; }

        public ObservableCollection<ExprNode> ExpressionModel { get; set; }

        public void RefreshAll()
        {
            sourceTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            //operations.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            operations.Items.Refresh();

            operands.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            operands.Items.Refresh();

            expressionModel.ExprTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();
            expressionModel.ExprTree.Items.Refresh();
        }

        public ArithmeticBox(CsTable sourceTable, bool whereExpression)
        {
            CsSchema schema = sourceTable.Top;

            SourceTable = sourceTable;

            ExpressionModel = new ObservableCollection<ExprNode>();

            InitializeComponent();

            // Initialize a list of possible operations
            ActionType[] ops;
            if (whereExpression)
            {
                // Other ways: to collapse a grid row: http://stackoverflow.com/questions/2502178/wpf-hide-grid-row
                Controls.RowDefinitions[0].Height = new GridLength(0);

                ops = new ActionType[] 
                { 
                    ActionType.MUL, ActionType.DIV, ActionType.ADD, ActionType.SUB, 
                    ActionType.LEQ, ActionType.GEQ, ActionType.GRE, ActionType.LES, 
                    ActionType.EQ, ActionType.NEQ, 
                    ActionType.AND, ActionType.OR, 
                };
            }
            else
            {
                Controls.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Auto);

                ops = new ActionType[] { ActionType.MUL, ActionType.DIV, ActionType.ADD, ActionType.SUB };
            }
            operations.ItemsSource = ops;

            // Initialize a list of possible column accesses
            var paths = new PathEnumerator(
                new List<CsTable>(new CsTable[] { SourceTable }),
                new List<CsTable>(new CsTable[] { schema.GetPrimitive("Integer"), schema.GetPrimitive("Double") }), 
                false, 
                DimensionType.IDENTITY_ENTITY
               );
            SourcePaths = paths.ToList();
        }

        private void AddOperation_Click(object sender, RoutedEventArgs e)
        {
            //
            // Determine parent expression node for inserting the selected new operation node
            //
            ExprNode parentExpr;
            parentExpr = (ExprNode)expressionModel.ExprTree.SelectedItem;
            if (parentExpr == null && ExpressionModel.Count != 0) return; // No parent node

            //
            // Determine operation and create child expression node
            //
            ActionType op = (ActionType)operations.SelectedItem;
            if (op == null) return; // WARNING: should never happen

            var expr = new ExprNode();
            expr.Operation = OperationType.CALL;
            expr.Action = op;

            // We need a human-readable string for representing an operation
            if (op == ActionType.MUL) expr.Name = "*";
            else if (op == ActionType.DIV) expr.Name = "/";
            else if (op == ActionType.ADD) expr.Name = "+";
            else if (op == ActionType.SUB) expr.Name = "-";

            else if (op == ActionType.LEQ) expr.Name = "<=";
            else if (op == ActionType.GEQ) expr.Name = ">=";
            else if (op == ActionType.GRE) expr.Name = ">";
            else if (op == ActionType.LES) expr.Name = "<";

            else if (op == ActionType.EQ) expr.Name = "==";
            else if (op == ActionType.NEQ) expr.Name = "!=";

            else if (op == ActionType.AND) expr.Name = "&&";
            else if (op == ActionType.OR) expr.Name = "||";

            else expr.Name = op.ToString();

            //
            // Insert new child expression
            //
            if (parentExpr == null) // First exprssion node
            {
                ExpressionModel.Add(expr);
            }
            else
            {
                parentExpr.AddChild(expr);
            }

            RefreshAll();
        }

        private void AddOperand_Click(object sender, RoutedEventArgs e)
        {
            //
            // Determine parent expression
            //
            ExprNode parentExpr;
            parentExpr = (ExprNode)expressionModel.ExprTree.SelectedItem;
            if (parentExpr == null && ExpressionModel.Count != 0) return; // Nothing is selected

            //
            // Determine dimension (function) and create child expression
            //
            DimPath path = (DimPath)operands.SelectedItem;
            if (path == null) return;

            var expr = ExprNode.CreateReader(path, false);
            expr = (ExprNode)expr.Root;

            //
            // Insert new child expression
            //
            if (parentExpr == null) // First exprssion node
            {
                ExpressionModel.Add(expr);
            }
            else
            {
                parentExpr.AddChild(expr);
            }

            RefreshAll();
        }

        private void AddValue_Click(object sender, RoutedEventArgs e)
        {
            //
            // Determine parent expression
            //
            ExprNode parentExpr;
            parentExpr = (ExprNode)expressionModel.ExprTree.SelectedItem;
            if (parentExpr == null && ExpressionModel.Count != 0) return; // Nothing is selected

            //
            // Determine operation and create child expression node
            //
            string val = valueOperand.Text;
            if (string.IsNullOrEmpty(val)) return;

            var expr = new ExprNode();
            expr.Operation = OperationType.VALUE;
            expr.Action = ActionType.READ;
            expr.Name = val;

            //
            // Insert new child expression
            //
            if (parentExpr == null) // First exprssion node
            {
                ExpressionModel.Add(expr);
            }
            else
            {
                parentExpr.AddChild(expr);
            }

            RefreshAll();
        }

        private void RemoveNode_Click(object sender, RoutedEventArgs e)
        {
            //
            // Determine currently selected expression
            //
            ExprNode selectedNode;
            selectedNode = (ExprNode)expressionModel.ExprTree.SelectedItem;
            if (selectedNode == null) return; // Nothing is selected

            if (selectedNode.Parent == null) // First exprssion node
            {
                ExpressionModel.Remove(selectedNode);
            }
            else
            {
                selectedNode.Parent.RemoveChild(selectedNode);
            }

            RefreshAll();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
