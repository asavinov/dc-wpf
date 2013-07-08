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
        public Set Set { get; set; } // It is a model which contains data to be visualized
        public DataGrid Grid { get; set; } // It is a view where the data is visualized

/*
            There are many open sets each showing a DataGrid. We need a global list of all open sets. 
            When a set is open we need to dynamically create a DataGrid with dynamic list of columns corresponding to its dimensions.
            Here we need to dynamically show the currently active grid.

            http://stackoverflow.com/questions/16120010/how-to-dynamically-create-a-datagrid-in-wpf
            Dynamic grid: http://paulstovell.com/blog/dynamic-datagrid
*/
        public SetGridView(Set _set) 
        {
            Set = _set;

            Grid = new DataGrid();
            Grid.Style = Application.Current.MainWindow.FindResource("ReadOnlyGridStyle") as Style;
            Grid.Name = Set.Name;
            Grid.AutoGenerateColumns = false;

            Grid.ItemsSource = new ObservableCollection<object>(); 
            // "{Binding Path=Records}" 
            // A Set method which returns an array or enumerable or otherwise provides access to records
            // Maybe Set class has to return from Set.ItemSource an enumerator object (storing Offset). Then we avoid the problem of record class. 
            // The returned enumerable object has to implement method Value each attribute/cell bound to. 

            // Create columns for each dimension
            foreach(Dim dim in Set.GreaterDims) 
            {
                Binding binding = new Binding(dim.Name);
                DataGridColumn col;
                col = new CustomBoundColumn() // DataGridTextColumn
                {
                    Header = dim.Name,
                    Binding = binding,
                    TemplateName = "CellTemplate"
                    // Width = new DataGridLength(200),
                    // FontSize = 12
                };

                Grid.Columns.Add(col);
            }

        }
    }

    public class CustomBoundColumn : DataGridBoundColumn
    {
        public string TemplateName { get; set; }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var binding = new Binding(((Binding)Binding).Path.Path);
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

}
