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
    /// Interaction logic for ColumnListControl.xaml
    /// </summary>
    public partial class ColumnListControl : UserControl
    {
        // Columns from this table are shown (context)
        protected ComTable _table;
        public ComTable Table
        {
            get { return _table; }
            set
            {
                if (_table == value) return;
                if (_table != null)
                {
                    ((Set)_table).CollectionChanged -= this.CollectionChanged; // Unregister from the old schema
                }
                Items.Clear();
                _table = value;
                if (_table == null) return;
                ((Set)_table).CollectionChanged += this.CollectionChanged; // Unregister from the old schema

                // Fill the list of items
                foreach (ComColumn column in _table.Columns)
                {
                    if (column.IsSuper) continue;
                    Items.Add(column);
                }
            }
        }

        // What is displayed in the list and bound to it as (ItemsSource)
        public ObservableCollection<ComColumn> Items { get; set; }

        // It is what we bind to the list view (SelectedItem)
        private ComColumn _selectedItem;
        public ComColumn SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
            }
        }
        
        public ColumnListControl()
        {
            Items = new ObservableCollection<ComColumn>();

            InitializeComponent();
        }

        // Process events from the table about adding/removing tables
        protected void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add) // Decide if this node has to add a new child node
            {
                ComColumn column = e.NewItems != null && e.NewItems.Count > 0 ? (ComColumn)e.NewItems[0] : null;
                if (column == null) return;

                if (!Items.Contains(column))
                {
                    Items.Add(column);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                ComColumn column = e.OldItems != null && e.OldItems.Count > 0 ? (ComColumn)e.OldItems[0] : null;
                if (column == null) return;

                if (Items.Contains(column))
                {
                    Items.Remove(column);
                }
            }
        }
    }
}
