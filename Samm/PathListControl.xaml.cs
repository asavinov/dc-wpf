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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Samm
{
    /// <summary>
    /// Interaction logic for PathListControl.xaml
    /// 
    /// Interesting article on how to pass data into a user control using both DataContext and parameter binding: 
    /// http://www.codeproject.com/Articles/137288/WPF-Passing-Data-to-Sub-Views-via-DataContext-Caus
    /// 
    /// Visualization options:
    /// - Possible segment directions: up, down, both
    /// - Editable or constant segments. Segments can be added and removed.
    /// - Editable or constant sets. Only existing sets can be used or sets can be added (for future creation).
    /// - The path can be temporarily in inconsistent state. Missing intermediate segments or empty segments with no initialized parameters. For example, if a segment is deleted.
    /// - Validate expression method.
    /// - Recommendation of paths/segments/sets method and its use when proposing segments
    /// 
    /// </summary>
    public partial class PathListControl : UserControl
    {
        public PathListControl()
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

        public Com.Model.Expression FirstSegment
        {
            get
            {
                if (Root == null) return null;

                Com.Model.Expression seg = Root;
                while (seg.Input != null)
                {
                    if (seg.Input.Operation != Com.Model.Operation.FUNCTION && seg.Input.Operation != Com.Model.Operation.INVERSE_FUNCTION)
                        break; // Nest node is non-function node
                    seg = seg.Input;
                }
                return seg; 
            }
        }
        public Com.Model.Expression LastSegment
        {
            get
            {
                if (Root == null) return null;

                if (Root.Operation != Com.Model.Operation.FUNCTION && Root.Operation != Com.Model.Operation.INVERSE_FUNCTION)
                    return null;
                    
                return Root; 
            }
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
                _current = FirstSegment;
            }
            else
            {
                _current = _current.ParentExpression;
            }

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
