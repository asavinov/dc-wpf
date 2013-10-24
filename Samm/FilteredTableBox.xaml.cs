﻿using System;
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
    /// Interaction logic for FilteredTableBox.xaml
    /// 
    /// It is implemented on the basis of ArithmeticBox so coordinate any changes. At least both boxes have the same expression editor logic which is duplicated.
    /// TODO: Extract expression editor control and embed it into ArithmeticBox and FilteredTableBox
    /// 
    /// </summary>
    public partial class FilteredTableBox : Window
    {
        public Set SourceTable { get; set; }
        public Set FilteredTable { get; set; }
        public ObservableCollection<Com.Model.Expression> ExpressionModel { get; set; }

        public void RefreshAll()
        {
            sourceTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            filteredTable.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            Operands.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            Operands.Items.Refresh();

            expressionModel.ExpressionTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();
            expressionModel.ExpressionTree.Items.Refresh();
        }

        public FilteredTableBox()
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

            var expr = Com.Model.Expression.CreateProjectExpression(new List<Dim> { FilteredTable.SuperDim, op }, Operation.DOT);

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
    }
}