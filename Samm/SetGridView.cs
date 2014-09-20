using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Com.Model;

namespace Samm
{
    /// <summary>
    /// This class can be viewed as an extension of Grid which is intended for visualizing a set.
    /// </summary>
    public class SetGridView
    {
        // Inheritance by-reference. We reference a base object.
        public DataGrid Grid { get; set; } // It is a view where the data is visualized


        public ComTable Set { get; set; } // It is a model which contains data to be visualized

        public List<DimPath> Paths { get; set; } // Only these paths will be displayed

        public bool ShowPaths = true;

        /*
                    There are many open sets each showing a DataGrid. We need a global list of all open sets. 
                    When a set is open we need to dynamically create a DataGrid with dynamic list of columns corresponding to its dimensions.
                    Here we need to dynamically show the currently active grid.

                    Bidning sources overview: http://msdn.microsoft.com/en-us/library/ms743643.aspx
                    http://stackoverflow.com/questions/16120010/how-to-dynamically-create-a-datagrid-in-wpf
                    Dynamic grid: http://paulstovell.com/blog/dynamic-datagrid
                    Virtualization: http://www.codeproject.com/Articles/34405/WPF-Data-Virtualization
 
                    Item source uses an IEnumerator of a set (MoveNext, Current etc.) which returns an object representing the current element. ElementEnemerator/Items<Item or string[]> Set.GetElementEnumerator/GetItemSource.
                    It is better to use IList because it has an indexer of elements and can be used more efficiently for virtualization: IList<Element/Item/string[]> Set.GetItemList/GetItemSource.
                    ElementItems : IList<Item/Element/Row/Member> GetElementItems() - produce such a list. Current and indexer return NEW Element
                    StringItems : IList<string[]> - produce such a list. Current and indexer return NEW string[]

                    The returned object (Element/Item/string[]) cannot be shared so each time we have to return a new instance from Current or element indexer.

                    We might return an Item/Element object with offset and set reference. The class implements also a dimension indexer by dynamically accesses the set.
                    + we can implement also other methods (say, access on dim names, converters etc.),
                    Or we could simply return from Current an array of string values to be shown in the grid (which has by definition an indexer).
                    -+ values are copied and transformed into string which is good if they are anyway copied, + formatting and options can be controled by the set parameters
        */
        public SetGridView(ComTable _set) 
        {
            Set = _set;

            Grid = new DataGrid();
            Grid.Style = Application.Current.MainWindow.FindResource("ReadOnlyGridStyle") as Style;
            Grid.AutoGenerateColumns = false;

            // Initialize paths we want to visualize
            var pathEnum = new PathEnumerator(Set, DimensionType.IDENTITY_ENTITY);
            Paths = new List<DimPath>();
            foreach(var path in pathEnum) 
            {
                if (path.Path.Count == 0) continue; // ERROR
                if (path.Path.Exists(x => x.IsSuper)) continue;

                if (path.Path.Count == 1)
                {
                    path.Name = path.FirstSegment.Name;
                }
                else
                {
                    path.Name = path.ColumnNamePath;
                }

                Paths.Add(path);
            }

            if (ShowPaths) // Create and confiture grid columns for all paths
            {
                for (int i = 0; i < Paths.Count; i++)
                {
                    DimPath path = Paths[i];

                    Binding binding = new Binding(string.Format("[{0}]", i)); // Bind to an indexer

                    DataGridColumn col1 = new DataGridTextColumn() { Header = path.Name, Binding = binding }; // No custom cell template
                    DataGridColumn col2 = new CustomBoundColumn() { Header = path.Name, Binding = binding, TemplateName = "CellTemplate" }; // Custom cell template will be used
                    // Additional column parameters: Width = new DataGridLength(200), FontSize = 12

                    Grid.Columns.Add(col1);
                }
            }
            else // Create and configure grid columns for all direct greater dimensions
            {
                for (int i = 0; i < Set.Columns.Count; i++)
                {
                    ComColumn dim = Set.Columns[i];
                    if (dim.IsSuper) continue;

                    Binding binding = new Binding(string.Format("[{0}]", i)); // Bind to an indexer

                    DataGridColumn col1 = new DataGridTextColumn() { Header = dim.Name, Binding = binding }; // No custom cell template
                    DataGridColumn col2 = new CustomBoundColumn() { Header = dim.Name, Binding = binding, TemplateName = "CellTemplate" }; // Custom cell template will be used
                    // Additional column parameters: Width = new DataGridLength(200), FontSize = 12

                    Grid.Columns.Add(col1);
                }
            }

            Grid.ItemsSource = new Elements(this);
        }
    }

    public class CustomBoundColumn : DataGridBoundColumn
    {
        public string TemplateName { get; set; }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var binding = new Binding(((Binding)Binding).Path.Path);
            //var binding = new Binding("[0]");
            binding.Source = dataItem;

            var content = new ContentControl();
            content.ContentTemplate = (DataTemplate)cell.FindResource(TemplateName);
            content.SetBinding(ContentControl.ContentProperty, binding);
            return content;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            return GenerateElement(cell, dataItem);
       }
    }

    public class Elements : IEnumerable<Element>, IEnumerator<Element>
    {
        public SetGridView GridView { get; set; }

        public int Offset { get; set; }

        public Elements(SetGridView gridView)
        {
            GridView = gridView;
            Offset = -1;
        }

        // IEnumerable Members 

        public IEnumerator<Element> GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // IEnumerable interface

        public Element Current
        {
            get 
            {
                return new Element(GridView, Offset); 
            }
        }

        public void Dispose()
        {
            GridView = null;
            Offset = -1;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            if (Offset < GridView.Set.Data.Length) Offset++; // Increement if possible

            if (Offset >= GridView.Set.Data.Length) return false; // Cannot move

            return true;
        }

        public void Reset()
        {
            Offset = -1;
        }
    }

    public class Element 
    {
        // It should be an enumerator returned by a Set
        public SetGridView GridView { get; set; }
        public int Offset { get; set; }

        public Element(SetGridView gridView)
            : this(gridView, -1)
        {
        }

        public Element(SetGridView gridView, int offset)
        {
            GridView = gridView;
            Offset = offset;
        }

        public string this[int index] // It returns values that are shown in grid columns
        {
            get
            {
                object cell = Offset;

                if (GridView.ShowPaths) // Use stored paths
                {
                    DimPath path = GridView.Paths[index];
                    int ofs;
                    for (int i = 0; i < path.Path.Count; i++)
                    {
                        ofs = (int)cell; // Use output as an input for the next iteration

                        if (path.Path[i].Data.IsNull(ofs))
                        {
                            cell = null;
                            break; // Cannot continue with next segments
                        }
                        else
                        {
                            cell = path.Path[i].Data.GetValue(ofs);
                        }
                    }
                }
                else // Use greater dimensions
                {
                    int dimCount = GridView.Set.Columns.Count;
                    if (index < 0 || index >= dimCount) return null;
                    ComColumn dim = GridView.Set.Columns[index];

                    if (dim.Data.IsNull(Offset))
                    {
                        cell = null;
                    }
                    else
                    {
                        cell = dim.Data.GetValue(Offset);
                    }
                }

                if (cell == null)
                {
                    return "";
                }
                else
                {
                    return Convert.ToString(cell);
                }
            }
        }

        public string Value(string dimName)
        {
            ComColumn dim = GridView.Set.GetColumn(dimName);
            if (dim == null) return null;
            return (string)dim.Data.GetValue(Offset);
        }
    }

}
