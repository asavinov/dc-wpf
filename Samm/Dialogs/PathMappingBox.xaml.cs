using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    /// Interaction logic for PathMappingBox.xaml
    /// </summary>
    public partial class PathMappingBox : Window, INotifyPropertyChanged
    {
        bool IsNew { get; set; }

        DcColumn Column { get; set; }

        public string NewColumnName { get; set; }

        public MappingModel MappingModel { get; set; }

        public ObservableCollection<DcTable> TargetTables { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshAll()
        {
            this.GetBindingExpression(PathMappingBox.DataContextProperty).UpdateTarget();

            newColumnName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            sourceTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            targetTables.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            //targetTables.SelectedItem = null; // NewColumnName.Output;

            sourceTree.MatchTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();
            targetTree.MatchTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();

            // !!! Use it for other controls where we update the data context and need to refresh the view
            sourceTree.GetBindingExpression(TreeView.DataContextProperty).UpdateTarget();
            targetTree.GetBindingExpression(TreeView.DataContextProperty).UpdateTarget();
        }

        public PathMappingBox(DcColumn column)
        {
            this.okCommand = new DelegateCommand(this.OkCommand_Executed, this.OkCommand_CanExecute);

            InitializeComponent();

            if (column.Input.Columns.Contains(column)) IsNew = false;
            else IsNew = true;

            Column = column;
            DcTable sourceTable = column.Input;
            DcTable targetTable = column.Output;

            // Name
            NewColumnName = Column.Name;

            // Compute possible target tables
            TargetTables = new ObservableCollection<DcTable>(MappingModel.GetPossibleGreaterSets(sourceTable));

            // TODO: Suggest the best target type
            if (targetTable == null)
            {
                // Compare the quality of gest mappings from the the source set to possible target sets

                DcTable bestTargetTable = TargetTables.Count > 0 ? TargetTables[0] : null;
                double bestSimilarity = 0.0;
                /*
                foreach (ComTable set in targetTables)
                {
                   ComColumn dim2 = set.CreateDefaultLesserDimension(dim.Name, dim.Input);

                    List<Mapping> mappings = m.MapDim(new DimPath(dim), new DimPath(dim2));
                    if (mappings[0].Similarity > bestSimilarity) { bestTargetTable = set; bestSimilarity = mappings[0].Similarity; }
                    m.Mappings.Clear();
                }
                */
                targetTable = bestTargetTable;
            }
            targetTables.SelectedItem = targetTable;
            // In edit mode (for existing column), we do not allow for changing the type
            if (!IsNew)
            {
                targetTables.IsEnabled = false;
            }

            MappingModel = new MappingModel(sourceTable, targetTable);
            if (!IsNew)
            {
                MappingModel.Mapping = Column.Definition.Mapping;
            }

            RefreshAll();
        }

        private void recommendButton_Click(object sender, RoutedEventArgs e)
        {
            MappingModel.SelectBestTarget();
            targetTree.Select(MappingModel.TargetTree.SelectedNode);
        }

        private void addMatchButton_Click(object sender, RoutedEventArgs e)
        {
            MappingModel.AddMatch();

            // Redraw both trees. Use property name like "CanMatch" or String.Empty for all properties (but it does not work because selection is also updated)
            MappingModel.SourceTree.NotifyAllOnPropertyChanged("CanMatch");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("CanMatch");

            MappingModel.SourceTree.NotifyAllOnPropertyChanged("IsMatched");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("IsMatched");
        }

        private void removeMatchButton_Click(object sender, RoutedEventArgs e)
        {
            MappingModel.RemoveMatch();

            // Redraw both trees. Use property name like "CanMatch" or String.Empty for all properties (but it does not work because selection is also updated)
            MappingModel.SourceTree.NotifyAllOnPropertyChanged("CanMatch");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("CanMatch");

            MappingModel.SourceTree.NotifyAllOnPropertyChanged("IsMatched");
            MappingModel.TargetTree.NotifyAllOnPropertyChanged("IsMatched");
        }

        private void TargetTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)e.Source; // or sender
            var set = cb.SelectedItem; // or SelectedValue

            if (set == null) return;

            MappingModel.TargetSet = (DcTable)set;

            RefreshAll(); // Refresh
        }

        private readonly ICommand okCommand;
        public ICommand OkCommand
        {
            get { return this.okCommand; }
        }
        private bool OkCommand_CanExecute(object state)
        {
            if (string.IsNullOrWhiteSpace(newColumnName.Text)) return false;

            if (targetTables.SelectedItem == null) return false;

            if (MappingModel.Mapping.Matches.Count == 0) return false;

            return true;
        }
        private void OkCommand_Executed(object state)
        {
            DcSchema schema = Column.Input.Schema;

            // Column name
            Column.Name = newColumnName.Text;

            // Column type
            if (IsNew)
            {
                Column.Output = (DcTable)targetTables.SelectedItem;
            }

            // Column definition
            Column.Definition.DefinitionType = ColumnDefinitionType.LINK;
            Column.Definition.Mapping = MappingModel.Mapping;
            Column.Definition.IsAppendData = false;

            this.DialogResult = true;
        }
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
        }

    }

    // Usage of validation rules: http://msdn.microsoft.com/en-us/library/aa969773.aspx
    public class ChangeRangeValidationRule : ValidationRule
    {
        double minMargin;
        double maxMargin;

        public double MinMargin
        {
            get { return this.minMargin; }
            set { this.minMargin = value; }
        }

        public double MaxMargin
        {
            get { return this.maxMargin; }
            set { this.maxMargin = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double margin;

            // Is a number? 
            if (!double.TryParse((string)value, out margin))
            {
                return new ValidationResult(false, "Not a number.");
            }

            // Is in range? 
            if ((margin < this.minMargin) || (margin > this.maxMargin))
            {
                string msg = string.Format("Margin must be between {0} and {1}.", this.minMargin, this.maxMargin);
                return new ValidationResult(false, msg);
            }

            // Number is valid 
            return new ValidationResult(true, null);
        }
    }
}
