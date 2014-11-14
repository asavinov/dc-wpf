﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Com.Model;

namespace Samm.Controls
{
    /// <summary>
    /// Interaction logic for TableListControl.xaml
    /// </summary>
    public partial class TableListControl : UserControl
    {
        // Tables from this schema are shown (context)
        protected ComSchema _schema;
        public ComSchema Schema 
        {
            get { return _schema; }
            set
            {
                if (_schema == value) return;
                if (_schema != null)
                {
                    ((Set)_schema.Root).CollectionChanged -= this.CollectionChanged; // Unregister from the old schema
                }
                Items.Clear();
                _schema = value;
                if (_schema == null) return;
                ((Set)_schema.Root).CollectionChanged += this.CollectionChanged; // Unregister from the old schema

                // Fill the list of items
                foreach (ComTable table in _schema.Root.SubTables)
                {
                    Items.Add(table);
                }
            }
        }

        // What is displayed in the list and bound to it as (ItemsSource)
        public ObservableCollection<ComTable> Items { get; set; }

        // It is what we bind to the list view (SelectedItem)
        private ComTable _selectedItem;
        public ComTable SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                
                MainWindow main = (MainWindow)Application.Current.MainWindow;
                main.ColumnListView.Table = _selectedItem;
            }
        }


        public TableListControl()
        {
            Items = new ObservableCollection<ComTable>();

            InitializeComponent();
        }

        // Process events from the schema about adding/removing tables
        protected void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add) // Decide if this node has to add a new child node
            {
                ComColumn column = e.NewItems != null && e.NewItems.Count > 0 ? (ComColumn)e.NewItems[0] : null;
                if (column == null) return;

                if (column.IsSuper && !Items.Contains(column.Input))
                {
                    Items.Add(column.Input);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                ComColumn column = e.OldItems != null && e.OldItems.Count > 0 ? (ComColumn)e.OldItems[0] : null;
                if (column == null) return;

                if (column.IsSuper && Items.Contains(column.Input))
                {
                    Items.Remove(column.Input);
                }
            }
        }

    }
}