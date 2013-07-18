﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Samm
{
    /// <summary>
    /// Interaction logic for DimensionPathListControl.xaml
    /// 
    /// Interesting article on how to pass data into a user control using both DataContext and parameter binding: 
    /// http://www.codeproject.com/Articles/137288/WPF-Passing-Data-to-Sub-Views-via-DataContext-Caus
    /// </summary>
    public partial class DimensionPathListControl : UserControl
    {
        public DimensionPathListControl()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// Enumerates segments of a (path) expression. 
    /// It is a model for all views that visualize or edit dimension path including a list view and a graph view. 
    /// </summary>
    public class Segments : IEnumerable<Segment>, IEnumerator<Segment>
    {
        public Com.Model.Expression Root { get; private set; }

        private Com.Model.Expression _current;

        public Segments(Com.Model.Expression root)
        {
            Root = root;
            _current = null;
        }

        // IEnumerable Members 

        public IEnumerator<Segment> GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // IEnumerable interface

        public Segment Current
        {
            get
            {
                return new Segment(_current);
            }
        }

        public void Dispose()
        {
            Root = null;
            _current = null;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            if (_current == null) // Before start
            {
                _current = Root;
            }
            else
            {
                _current = _current.Input;
            }

            // We need only function segments - so check the operation
            if (_current == null || (_current.Operation != Com.Model.Operation.FUNCTION && _current.Operation != Com.Model.Operation.INVERSE_FUNCTION)) 
                return false; // After end - non-function segment was reached

            return true;
        }

        public void Reset()
        {
            _current = null;
        }
    }

    public class Segment
    {
        public Com.Model.Expression Expression { get; private set; }

        public Segment(Com.Model.Expression expr)
        {
            Expression = expr;
        }

        public string SegmentName { get { return Expression.Name; } }

        public string FromName { get { return Expression.Input != null ? Expression.Input.OutputSetName : "NULL"; } }

        public string ToName { get { return Expression.OutputSetName; } }

        public bool IsDirect { get { return Expression.Operation == Com.Model.Operation.FUNCTION; } }

        public bool IsInverse { get { return Expression.Operation == Com.Model.Operation.INVERSE_FUNCTION; } }
    }

}
