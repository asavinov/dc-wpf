using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Data.ConnectionUI;

using Com.Model;

namespace Samm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        //
        // Data sources
        //
        public ObservableCollection<SetTop> Sources { get; set; }

        public ObservableCollection<SubsetTree> SourcesModel { get; set; } // What is shown in SubsetTree for data sources

        public bool IsInSources(Set set) // Determine if the specified set belongs to some data source
        {
            if (set == null || Sources == null) return false;
            foreach (SetTop t in Sources) { if (set.Top == t) return true; }
            return false;
        }
        public bool IsInSources(Dim dim) // Determine if the specified dimension belongs to some data source
        {
            if (dim == null || Sources == null) return false;
            if (IsInSources(dim.LesserSet) && IsInSources(dim.GreaterSet)) return true;
            return false;
        }
        public SubsetTree SelectedSourceItem { get { if (SourcesView == null || SourcesView.SubsetTree == null) return null; return (SubsetTree)SourcesView.SubsetTree.SelectedItem; } }
        public Set SelectedSourceSet { get { SubsetTree item = SelectedSourceItem; if (item == null) return null; if (item.IsSubsetNode) return item.LesserSet; return null; } }
        public Dim SelectedSourceDim { get { SubsetTree item = SelectedSourceItem; if (item == null) return null; if (item.IsDimensionNode) return item.Dim; return null; } }
        public bool IsSourcesFocused { get { return SourcesView.SubsetTree.IsFocused; } } // TODO: Does not work

        //
        // Mashups (only one is used)
        //
        public ObservableCollection<SetTop> Mashups { get; set; }
        public SetTop MashupTop { get { return Mashups.Count != 0 ? Mashups[0] : null; } }
        public SetRoot MashupRoot { get { return Mashups.Count != 0 ? Mashups[0].Root : null; } }

        public ObservableCollection<SubsetTree> MashupsModel { get; set; } // What is shown in SubsetTree for mashups
        public SubsetTree MashupModelRoot { get { return MashupsModel.Count != 0 ? (SubsetTree)MashupsModel[0] : null; } }

        public bool IsInMashups(Set set) // Determine if the specified set belongs to some mashup
        {
            if (set == null || Mashups == null) return false;
            foreach (SetTop t in Mashups) { if (set.Top == t) return true; }
            return false;
        }
        public bool IsInMashups(Dim dim) // Determine if the specified dimension belongs to some mashup
        {
            if (dim == null || Mashups == null) return false;
            if (IsInMashups(dim.LesserSet) && IsInMashups(dim.GreaterSet)) return true;
            return false;
        }
        public SubsetTree SelectedMashupItem { get { if (MashupsView == null || MashupsView.SubsetTree == null) return null; return (SubsetTree)MashupsView.SubsetTree.SelectedItem; } }
        public Set SelectedMashupSet { get { SubsetTree item = SelectedMashupItem; if (item == null) return null; if (item.IsSubsetNode) return item.LesserSet; return null; } }
        public Dim SelectedMashupDim { get { SubsetTree item = SelectedMashupItem; if (item == null) return null; if (item.IsDimensionNode) return item.Dim; return null; } }


        //
        // Operations and behavior
        //
        public DragDropHelper DragDropHelper { get; protected set; }

        public MainWindow()
        {
            //
            // Initialize data sources
            //
            Sources = new ObservableCollection<SetTop>();
            SourcesModel = new ObservableCollection<SubsetTree>();

            SetTop sourceTop = CreateSampleSchema(); // For testing
            Sources.Add(sourceTop);

            SubsetTree sourceModel = new SubsetTree(sourceTop.Root.SuperDim);
            sourceModel.ExpandTree();
            SourcesModel.Add(sourceModel);

            //
            // Initialize mashups (one empty mashup)
            //
            Mashups = new ObservableCollection<SetTop>();
            MashupsModel = new ObservableCollection<SubsetTree>();

            SetTop mashupTop = new SetTop("My Mashup");
            Mashups.Add(mashupTop);

            SubsetTree mashupModel = new SubsetTree(mashupTop.Root.SuperDim);
            mashupModel.ExpandTree();
            MashupsModel.Add(mashupModel);

            DragDropHelper = new DragDropHelper();

            //this.DataContext = this;
            InitializeComponent();
        }

        public SetTop CreateSampleSchema()
        {
            SetTop ds = new SetTop("My Data Source");
            Dim dim;

            Set departments = new Set("Departments");
            ds.Root.AddSubset(departments);

            dim = ds.GetPrimitiveSubset("String").CreateDefaultLesserDimension("name", departments);
            dim.IsIdentity = true;
            dim.Add();
            dim = ds.GetPrimitiveSubset("String").CreateDefaultLesserDimension("location", departments);
            dim.Add();

            departments.Append();
            departments.SetValue("name", 0, "SALES");
            departments.SetValue("location", 0, "Dresden");
            departments.Append();
            departments.SetValue("name", 1, "HR");
            departments.SetValue("location", 1, "Walldorf");

            Set employees = new Set("Employees");
            ds.Root.AddSubset(employees);

            dim = ds.GetPrimitiveSubset("String").CreateDefaultLesserDimension("name", employees);
            dim.IsIdentity = true;
            dim.Add();
            dim = ds.GetPrimitiveSubset("Double").CreateDefaultLesserDimension("age", employees);
            dim.Add();
            dim = ds.GetPrimitiveSubset("Double").CreateDefaultLesserDimension("salary", employees);
            dim.Add();
            dim = departments.CreateDefaultLesserDimension("dept", employees);
            dim.Add();

            Set managers = new Set("Managers");
            employees.AddSubset(managers);

            dim.Add();
            dim = ds.GetPrimitiveSubset("String").CreateDefaultLesserDimension("title", managers);
            dim.Add();
            dim = ds.GetPrimitiveSubset("Boolean").CreateDefaultLesserDimension("is project manager", managers);
            dim.Add();

            return ds;
        }


        # region Command_Executed (call backs from Commands)

        private void OpenTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedMashupSet == null) return;
            Operation_OpenTable(SelectedMashupSet);
            e.Handled = true;
        }

        private void AccessDatasourceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_AccessDatasource();
            e.Handled = true;
        }

        private void SqlserverDatasourceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_SqlserverDatasource();
            e.Handled = true;
        }

        private void ImportTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedSourceSet == null) return;
            Wizard_ImportTable(SelectedSourceSet, MashupRoot);
            e.Handled = true;
        }

        private void FilteredTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedMashupSet == null) return;
            Wizard_FilteredTable(SelectedMashupSet);
            e.Handled = true;
        }

        private void AddAggregationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Find a set where we want to create a new derived (aggregated) dimension
            Set srcSet = null;
            srcSet = MashupRoot.FindSubset("Customers");

            // Find a set and dimension which have to be used in the definition (a dimension the values of which will be aggregated)
            Dim dstDim = SelectedMashupDim;
            Set dstSet = SelectedMashupSet;
            if(dstSet == null && dstDim != null)
            {
                dstSet = dstDim.LesserSet;
            }

            Wizard_AddAggregation(srcSet, dstSet, dstDim);
            e.Handled = true;
        }

        private void AddCalculationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_AddCalculation(SelectedMashupSet);
            e.Handled = true;
        }

        private void ChangeTypeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Set newTypeSet = MashupRoot.FindSubset("Suppliers"); // TODO: It is for test purposes. We need a new parameter with the desired target table (new type/range)
            Wizard_ChangeType(SelectedMashupDim, newTypeSet);
        }

        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AboutBox dlg = new AboutBox(); // Instantiate the dialog box
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 
        }

        #endregion

        #region Wizards (with user interactions)

        public void Wizard_AccessDatasource()
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
            ofd.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
            ofd.Filter = "Access Files (*.ACCDB)|*.ACCDB|All files (*.*)|*.*";
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() != true) return;

            string filePath = ofd.FileName;
            string safeFilePath = ofd.SafeFileName;

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath;

            // Initialize a new data source schema
            SetTopOledb top = new SetTopOledb("My Data Source");

            top.ConnectionString = connectionString;

            top.Open();
            top.ImportSchema();

            Sources.Add(top); // Append to the list of data sources

            // And also append to the tree model
            SubsetTree sourceModel = new SubsetTree(top.Root.SuperDim);
            sourceModel.ExpandTree();
            SourcesModel.Add(sourceModel);
        }

        private static void Wizard_SqlserverDatasource()
        {
            /*
                                // Read schema: http://www.simple-talk.com/dotnet/.net-framework/schema-and-metadata-retrieval-using-ado.net/

            //                    DataTable schema = connection.GetSchema();
            //                    DataTable schema = connection.GetSchema("Databases", new string[] { "Northwind" });
            //                    DataTable schema = connection.GetSchema("Databases");
            //                    DataTable schema = connection.GetSchema(System.Data.SqlClient.SqlClientMetaDataCollectionNames.Databases);

                                using (DataTableReader tableReader = schema.CreateDataReader())
                                {
                                    while (tableReader.Read())
                                    {
                                        Console.WriteLine(tableReader.ToString());
                                    }
                                }

                                SqlCommand cmd = new SqlCommand("SELECT * FROM sys.Tables", connection);

                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        Console.WriteLine(reader.HasRows);
                                    }
                                }
            */


            /*
                        //
                        // OLEDB Connection string dialog: http://support.microsoft.com/default.aspx?scid=kb;EN-US;310083
                        //
                        // References (COM):
                        // MSDASC: Microsoft OLEDB Service Component 1.0 Type Library
                        MSDASC.DataLinks mydlg = new MSDASC.DataLinks();
                        // ADODB: Microsoft ActiveX Data Objects 2.7
                        ADODB._Connection ADOcon;
                        //Cast the generic object that PromptNew returns to an ADODB._Connection.
                        ADOcon = (ADODB._Connection) mydlg.PromptNew();
                        ADOcon.Open("", "", "", 0);
                        if (ADOcon.State == 1)
                        {
                            MessageBox.Show("Connection Opened");
                            ADOcon.Close();
                        }
                        else
                        {
                            MessageBox.Show("Connection Failed");
                        }
            */
            /*
                        //
                        // Custom dialog
                        //
                        // Instantiate the dialog box
                        Connections.SqlServerDialog dlg = new Connections.SqlServerDialog();
                        // Configure the dialog box
                        dlg.Owner = this;
                        // Open the dialog box modally 
                        dlg.ShowDialog();
            */
            //
            // http://archive.msdn.microsoft.com/Connection
            // http://www.mztools.com/articles/2007/mz2007011.aspx
            // authorized for redistribution since Feb 2010: http://connect.microsoft.com/VisualStudio/feedback/details/423104/redistributable-microsoft-data-connectionui-dll-and-microsoft-data-connectionui-dialog-dll
            //
            // Assemblies: Microsoft.Data.ConnectionUI.dll, Microsoft.Data.ConnectionUI.Dialog.dll
            DataConnectionDialog dcd = new DataConnectionDialog();
            DataConnectionConfiguration dcs = new DataConnectionConfiguration(null);
            dcs.LoadConfiguration(dcd);

            if (DataConnectionDialog.Show(dcd) != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string connectionString = dcd.ConnectionString; // Example: "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\Users\\savinov\\git\\samm\\Northwind.accdb"

            //readOledbSchema(connectionString); //For testing purposes

            dcs.SaveConfiguration(dcd);
        }

        public void Wizard_ImportTable(Set set, Set parent)
        {
            //
            // Compute suggested mapping(s) for importing
            //
            Mapper mapper = new Mapper();
            mapper.SetCreationThreshold = 1.0;
            mapper.MapSet(set, parent.Top);
            Mapping mapping = mapper.GetBestMapping(set, parent.Top);

            MappingModel model = new MappingModel(mapping);

            //
            // Show dialog with recommended mappings for import and the possibility to edit the mappings
            //
            MappingBox dlg = new MappingBox(); // Instantiate the dialog box
            dlg.Owner = this;
            dlg.MappingModel = model;
            dlg.RefreshAll();
            dlg.ShowDialog();

            if (dlg.DialogResult == false) return; // Cancel

            mapping.AddTargetToSchema(parent.Top);
            DimImport dimImport = new DimImport(mapping.SourceSet.Name, mapping.TargetSet, mapping.SourceSet); // Configure first set for import
            dimImport.Add();

            Set targetSet = mapping.TargetSet;
            targetSet.Mapping.Clear();
            targetSet.Mapping.Add(mapping);

            targetSet.Populate();
        }

        public void Wizard_FilteredTable(Set parent)
        {
            //
            // Create new subset
            //
            Set dstSet = new Set("My Table");
            parent.AddSubset(dstSet);

            //
            // Show recommendations and let the user choose one of them
            //
            FilteredTableBox dlg = new FilteredTableBox();
            dlg.Owner = this;
            dlg.SourceTable = parent;
            dlg.FilteredTable = dstSet;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            if (dlg.ExpressionModel == null && dlg.ExpressionModel.Count == 0)
                return;

            Com.Model.Expression expr = dlg.ExpressionModel[0];

            // Populate new set
            dstSet.WhereExpression = expr;
            dstSet.Populate();

            // HACK: refresh the view
            //SetTop mashup = MashupTop;
            //Mashups.RemoveAt(0);
            //Mashups.Add(mashup.Root);
        }

        public void Wizard_AddAggregation(Set srcSet, Set dstSet, Dim dstDim)
        {
            // Source set is where we want to create a new (source) derived dimension
            // Target set and dimension is what we want to use in the definition of the new dimension

            RecommendedAggregations recoms = new RecommendedAggregations();
            recoms.SourceSet = srcSet;
            recoms.TargetSet = dstSet;
            recoms.FactSet = null; // Any

            recoms.Recommend();

            if (dstDim != null) // Try to set the current measure to the specified dimension
            {
                recoms.MeasureDimensions.SelectedObject = dstDim;
            }

            //
            // Show recommendations and let the user choose one of them
            //
            AggregationBox dlg = new AggregationBox();
            dlg.Owner = this;
            dlg.Recommendations = recoms;
            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            if (recoms.IsValidExpression() != null) return;

            //
            // Create new derived dimension
            // Example: (Customers) <- (Orders) <- (Order Details) -> (Products) -> List Price
            //
            Dim aggregDim = (Dim)recoms.MeasureDimensions.SelectedObject;
            string derivedDimName = dlg.SourceColumn.Text;
            Com.Model.Expression aggreExpr = recoms.GetExpression();

            Dim derivedDim = aggregDim.GreaterSet.CreateDefaultLesserDimension(derivedDimName, srcSet);
            derivedDim.SelectExpression = aggreExpr;
            derivedDim.Add();

            // Update new derived dimension
            derivedDim.ComputeValues(); // Call SelectExpression.Evaluate(EvaluationMode.UPDATE);
        }

        public void Wizard_AddCalculation(Set srcSet)
        {
            if (srcSet == null) return;

            // Show recommendations and let the user choose one of them
            ArithmeticBox dlg = new ArithmeticBox();
            dlg.Owner = this;
            dlg.SourceTable = srcSet;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            if (dlg.ExpressionModel == null && dlg.ExpressionModel.Count == 0)
                return;

            Com.Model.Expression expr = expr = dlg.ExpressionModel[0];

            //
            // Create new derived dimension
            // Example: (Customers) <- (Orders) <- (Order Details) -> (Products) -> List Price
            //
            string derivedDimName = dlg.sourceColumn.Text;

            Set dstSet = expr.OutputSet;
            Dim derivedDim = dstSet.CreateDefaultLesserDimension(derivedDimName, srcSet);
            derivedDim.SelectExpression = expr;
            derivedDim.Add();

            // Update new derived dimension
            derivedDim.ComputeValues(); // Call SelectExpression.Evaluate(EvaluationMode.UPDATE);
        }

        public void Wizard_ChangeType(Dim dim, Set newTypeSet)
        {
            if (dim == null) return;
            if (newTypeSet == null) return; // Relevant target types could be proposed in the dialog box (as part of the assistence)

            Dim newDim = newTypeSet.CreateDefaultLesserDimension(dim.Name + " (1)", dim.LesserSet);

            Mapper mapper = new Mapper();
            mapper.MaxMappingsToBuild = 100;
            mapper.MapDim(new DimPath(dim), new DimPath(newDim));
            Mapping mapping = mapper.Mappings[0];

            //
            // Parameterize the mapping model
            //
            MappingModel model = new MappingModel(dim, newDim);
            model.Mapping = mapping;

            //
            // Show mapping editor with recommendations and let the user build the mapping
            //
            MappingBox dlg = new MappingBox();
            dlg.Owner = this;
            dlg.MappingModel = model;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            //
            // Really changing the range
            //

            // The result mapping is between two types (new and old) but in the dim def it must be from newDim.LesserSet to newDim.GreaterSet
            // So insert a prefix
            model.Mapping.InsertFirst(new DimPath(dim), null);

            newDim.Mapping = model.Mapping;
            newDim.Add();
            newDim.ComputeValues(); // Compute the values of the new dimension
        }

        #endregion

        #region Operations (no user interactions)

        public void Operation_OpenTable(Set set)
        {
            lblWorkspace.Content = set.Name;

            Label lbl = new Label();
            lbl.Content = "Content";

            var gridView = new SetGridView(set);
            GridPanel.Content = gridView.Grid;
        }

        #endregion

    }

    public class DragDropHelper
    {
        public bool CanDrag(object data)
        {
            if (data is Set) return true;
            else if (data is Dim) return true;
            else if (data is SubsetTree) return true;
            return false;
        }

        public bool CanDrop(object dropSource, object dropTarget)
        {
            if (dropSource == null || dropTarget == null) return false;
            return true;
        }

        public void Drop(object dropSource, object dropTarget)
        {
            if (dropSource == null || dropTarget == null) return;

            // Transform to one format (Set or Dim) from several possible classes: Set, Dim, SubsetTree
            if (dropSource is SubsetTree)
            {
                if (((SubsetTree)dropSource).IsSubsetNode) dropSource = ((SubsetTree)dropSource).LesserSet;
                else if (((SubsetTree)dropSource).IsDimensionNode) dropSource = ((SubsetTree)dropSource).Dim;
            }
            if (dropTarget is SubsetTree)
            {
                if (((SubsetTree)dropTarget).IsSubsetNode) dropTarget = ((SubsetTree)dropTarget).LesserSet;
                else if (((SubsetTree)dropTarget).IsDimensionNode) dropTarget = ((SubsetTree)dropTarget).Dim;
            }

            //
            // Conditions for importing a set: a set is dropped on a root
            //
            if (dropSource is Set && !(dropSource is SetRoot) && ((MainWindow)App.Current.MainWindow).IsInSources((Set)dropSource))
            {
                if (dropTarget is SetRoot && ((MainWindow)App.Current.MainWindow).IsInMashups((SetRoot)dropTarget))
                {
                    // Some table from a data source is dropped to the mashup: import table
                    ((MainWindow)App.Current.MainWindow).Wizard_ImportTable((Set)dropSource, (SetRoot)dropTarget);
                }
            }

            //
            // Conditions for new aggregated column: dimension is dropped on a set
            //
            if (dropSource is Dim && ((MainWindow)App.Current.MainWindow).IsInMashups((Dim)dropSource))
            {
                if (dropTarget is Set && !(dropTarget is SetRoot) && ((MainWindow)App.Current.MainWindow).IsInMashups((Set)dropTarget))
                {
                    // Note that here source and target have opposite interpretations
                    ((MainWindow)App.Current.MainWindow).Wizard_AddAggregation((Set)dropTarget, null, (Dim)dropSource);
                }
            }

            //
            // Conditions for type change: a set is dropped on a dimension
            //
            if (dropSource is Set && !(dropSource is SetRoot) && ((MainWindow)App.Current.MainWindow).IsInMashups((Set)dropSource))
            {
                if (dropTarget is Dim && !((Dim)dropTarget).IsPrimitive && ((MainWindow)App.Current.MainWindow).IsInMashups((Dim)dropTarget))
                {
                    ((MainWindow)App.Current.MainWindow).Wizard_ChangeType((Dim)dropTarget, (Set)dropSource);
                }
            }

        }

        public void DoDoubleClick(object data)
        {
            if (data == null) return;

            //
            // Conditions for opening a table view
            //
            if (data is SubsetTree && ((SubsetTree)data).IsSubsetNode) 
            {
                Set set = ((SubsetTree)data).LesserSet;
                if(!(set is SetRoot) && ((MainWindow)App.Current.MainWindow).IsInMashups(set)) 
                {
                    // Call a direct operation method for opening a table with the necessary parameters (rather than a command)
                    ((MainWindow)App.Current.MainWindow).Operation_OpenTable(set);
                }
            }
        }
    }

}
