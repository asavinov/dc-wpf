using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for LinkColumnBox.xaml
    /// </summary>
    public partial class LinkColumnBox : Window
    {
        public string NewColumnName { get; set; }

        public MappingModel MappingModel { get; set; }

        public List<Set> TargetTables { get; set; }

        private Mapper mapper; // We use it for building mappings

        public void SetNewType(Set newType) // Reconfigure the dialog objects for mapping to this new type
        {
            if (newType == null)
            {
                MappingModel = null;
                return;
            }

            // Store the new type in parameters
            //NewDim.GreaterSet = newType;

            // Compute/suggest new mappings for the new set
            mapper.Mappings.Clear();
            // mapper.MapDim(new DimPath(OldDim), new DimPath(NewDim)); // From source set to the target set

            // Update the model to be shown in UI
            //MappingModel = new MappingModel(OldDim, NewDim); // We simply create new (update is more difficult)
            MappingModel.Mapping = mapper.Mappings[0];
        }

        public void RefreshAll()
        {
            this.GetBindingExpression(LinkColumnBox.DataContextProperty).UpdateTarget();

            sourceTableName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

            sourceTree.MatchTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();
            targetTree.MatchTree.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();

            newColumnName.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            targetTables.GetBindingExpression(ComboBox.ItemsSourceProperty).UpdateTarget();
            targetTables.SelectedItem = null; // NewColumnName.GreaterSet;

            // !!! Use it for other controls where we update the data context and need to refresh the view
            sourceTree.GetBindingExpression(TreeView.DataContextProperty).UpdateTarget();
            targetTree.GetBindingExpression(TreeView.DataContextProperty).UpdateTarget();
        }

        public LinkColumnBox(CsTable sourceTable, List<CsTable> targetTables, CsTable targetTable)
            : this()
        {
            if (targetTable != null)
            {
                NewColumnName = targetTable.Name;
            }
            else
            {
                NewColumnName = "New Link Column";
            }

            mapper = new Mapper();
            mapper.MaxMappingsToBuild = 100;

            MappingModel = new MappingModel(sourceTable, targetTable);

            //
            // Initialize mapping by recommending best mapping
            //

            //mapper.MapDim(new DimPath(OldDim), new DimPath(NewDim));

            MappingModel.Mapping = mapper.Mappings[0];


            // !!! Target table is always chosen. 

            // What has priority: MappingModel or Mapping
            // - MappingModel is constant (the same object) because the dialog is bound to it
            //   - MappingModel is a mapping and two trees/lists for editing (selecting)
            // - Mapping could also be constant because some controls could be bound to it in future (say, a list of mappings)


            // We need a method which is called when a new target table is chosen
            // - initialize/configure the mapping
            // - refresh controls
            // - ??? should we store somewhere the results of the previous mapping editing which will be used if we return to this target later?

            // We need a method for updating target table (list/tree control and the model) after selection.

        }

        public LinkColumnBox()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void recommendButton_Click(object sender, RoutedEventArgs e)
        {

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

            SetNewType((Set)set);

            RefreshAll(); // Refresh
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
