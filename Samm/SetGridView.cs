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
    class SetGridView
    {
        public CsTable Set { get; set; } // It is a model which contains data to be visualized
        public DataGrid Grid { get; set; } // It is a view where the data is visualized

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
        public SetGridView(CsTable _set) 
        {
            Set = _set;

            Grid = new DataGrid();
            Grid.Style = Application.Current.MainWindow.FindResource("ReadOnlyGridStyle") as Style;
            Grid.AutoGenerateColumns = false;
            Grid.ItemsSource = new Elements(_set);

            // Create columns for each dimension
            for(int i=0; i < Set.GreaterDims.Count; i++) 
            {
                CsColumn dim = Set.GreaterDims[i];
                Binding binding = new Binding(string.Format("[{0}]", i)); // Bind to an indexer

                DataGridColumn col1 = new DataGridTextColumn() { Header = dim.Name, Binding = binding }; // No custom cell template
                DataGridColumn col2 = new CustomBoundColumn() { Header = dim.Name, Binding = binding, TemplateName = "CellTemplate" }; // Custom cell template will be used
                // Additional column parameters: Width = new DataGridLength(200), FontSize = 12

                Grid.Columns.Add(col1);
            }

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
        public CsTable Set { get; set; }
        public int Offset { get; set; }

        public Elements(CsTable set)
        {
            Set = set;
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
                return new Element(Set, Offset); 
            }
        }

        public void Dispose()
        {
            Set = null;
            Offset = -1;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            if (Offset < Set.TableData.Length) Offset++; // Increement if possible

            if (Offset >= Set.TableData.Length) return false; // Cannot move

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
        public CsTable Set { get; set; }
        public int Offset { get; set; }

        public Element(CsTable set)
        {
            Set = set;
            Offset = -1;
        }

        public Element(CsTable set, int offset)
        {
            Set = set;
            Offset = offset;
        }

        public string this[int index]
        {
            get
            {
                int dimCount = Set.GreaterDims.Count;
                if (index < 0 || index >= dimCount) return null;
                CsColumn dim = Set.GreaterDims[index];

                object cell = dim.ColumnData.GetValue(Offset);

                return Convert.ToString(cell);
            }
        }

        public string Value(string dimName)
        {
            CsColumn dim = Set.GetGreaterDim(dimName);
            if (dim == null) return null;
            return (string)dim.ColumnData.GetValue(Offset);
        }
    }

}
