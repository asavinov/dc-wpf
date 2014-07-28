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

        // TODO: we need to represent connections and source tables

        //
        // Mashups (only one is used)
        //
        public ObservableCollection<CsSchema> Mashups { get; set; }
        public CsSchema MashupTop { get { return Mashups.Count != 0 ? Mashups[0] : null; } }
        public CsTable MashupRoot { get { return Mashups.Count != 0 ? Mashups[0].Root : null; } }

        public ObservableCollection<SubsetTree> MashupsModel { get; set; } // What is shown in SubsetTree for mashups
        public SubsetTree MashupModelRoot { get { return MashupsModel.Count != 0 ? (SubsetTree)MashupsModel[0] : null; } }

        public bool IsInMashups(CsTable set) // Determine if the specified set belongs to some mashup
        {
            if (set == null || Mashups == null) return false;
            foreach (CsSchema t in Mashups) { if (set.Top == t) return true; }
            return false;
        }
        public bool IsInMashups(CsColumn dim) // Determine if the specified dimension belongs to some mashup
        {
            if (dim == null || Mashups == null) return false;
            if (IsInMashups(dim.LesserSet) && IsInMashups(dim.GreaterSet)) return true;
            return false;
        }
        public SubsetTree SelectedMashupItem { get { if (MashupsView == null || MashupsView.SubsetTree == null) return null; return (SubsetTree)MashupsView.SubsetTree.SelectedItem; } }
        public CsTable SelectedMashupSet { get { SubsetTree item = SelectedMashupItem; if (item == null) return null; if (item.IsSubsetNode) return item.LesserSet; return null; } }
        public CsColumn SelectedMashupDim { get { SubsetTree item = SelectedMashupItem; if (item == null) return null; if (item.IsDimensionNode) return item.Dim; return null; } }


        //
        // Operations and behavior
        //
        public DragDropHelper DragDropHelper { get; protected set; }

        public MainWindow()
        {
            //
            // Initialize data sources
            //

            //
            // Initialize mashups (one empty mashup)
            //
            Mashups = new ObservableCollection<CsSchema>();
            MashupsModel = new ObservableCollection<SubsetTree>();

            CsSchema mashupTop = CreateSampleSchema(); // new SetTop("New Mashup");
            Mashups.Add(mashupTop);

            SubsetTree mashupModel = new SubsetTree(mashupTop.Root.SuperDim);
            mashupModel.ExpandTree();
            MashupsModel.Add(mashupModel);

            DragDropHelper = new DragDropHelper();

            //this.DataContext = this;
            InitializeComponent();
        }

        public CsSchema CreateSampleSchema()
        {
            CsSchema ds = new SetTop("Sample Mashup");
            CsColumn d1, d2, d3, d4;

            CsTable departments = ds.CreateTable("Departments");
            ds.AddTable(departments, null, null);

            d1 = ds.CreateColumn("name", departments, ds.GetPrimitive("String"), true);
            d1.Add();
            d2 = ds.CreateColumn("location", departments, ds.GetPrimitive("String"), true);
            d2.Add();

            departments.TableData.Append(new CsColumn[] { d1, d2 }, new object[] { "SALES", "Dresden" });
            departments.TableData.Append(new CsColumn[] { d1, d2 }, new object[] { "HR", "Walldorf" });

            CsTable employees = ds.CreateTable("Employees");
            ds.AddTable(employees, null, null);

            d1 = ds.CreateColumn("name", employees, ds.GetPrimitive("String"), true);
            d1.Add();
            d2 = ds.CreateColumn("age", employees, ds.GetPrimitive("Double"), true);
            d2.Add();
            d3 = ds.CreateColumn("salary", employees, ds.GetPrimitive("Double"), true);
            d3.Add();
            d4 = ds.CreateColumn("dept", employees, departments, true);
            d4.Add();

            CsTable managers = ds.CreateTable("Managers");
            ds.AddTable(managers, employees, null);

            d1 = ds.CreateColumn("title", managers, ds.GetPrimitive("String"), false);
            d1.Add();
            d2 = ds.CreateColumn("is project manager", managers, ds.GetPrimitive("Boolean"), false);
            d2.Add();

            return ds;
        }

        # region Command_Executed (call backs from Commands)

        private void OpenTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedMashupSet == null) return;
            Operation_OpenTable(SelectedMashupSet);
            e.Handled = true;
        }

        private void TextDatasourceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_TextDatasource();
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

        private void FilteredTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedMashupSet == null) return;
            Wizard_FilteredTable(SelectedMashupSet);
            e.Handled = true;
        }

        private void ExtractTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedMashupSet != null) 
                Wizard_ExtractTable(SelectedMashupSet);
            else if (SelectedMashupDim != null && SelectedMashupDim.LesserSet != null)
                Wizard_ExtractTable(SelectedMashupDim.LesserSet);
            e.Handled = true;
        }

        private void DeleteTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CsTable set = null;
            if (SelectedMashupSet != null)
                set = SelectedMashupSet;
            else if (SelectedMashupDim != null && SelectedMashupDim.LesserSet != null)
                set = SelectedMashupDim.LesserSet;
            else return;

            // Remove all connections of this set with the schema by deleting all its dimensions
            set.SuperDim.Remove();
            set.SubDims.ToArray().ToList().ForEach(x => x.Remove());

            set.GreaterDims.ToArray().ToList().ForEach(x => x.Remove());
            set.LesserDims.ToArray().ToList().ForEach(x => x.Remove());

            e.Handled = true;
        }

        private void AddAggregationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CsColumn selDim = SelectedMashupDim;
            CsTable srcSet = SelectedMashupSet;
            if (srcSet == null && selDim != null)
            {
                srcSet = selDim.LesserSet;
            }

            Wizard_AddAggregation(srcSet, null, null);
            e.Handled = true;
        }

        private void AddCalculationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Wizard_AddCalculation(SelectedMashupSet);
            e.Handled = true;
        }

        private void ChangeTypeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CsTable newTypeSet = MashupTop.FindTable("Suppliers"); // TODO: It is for test purposes. We need a new parameter with the desired target table (new type/range)
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

        public void Wizard_TextDatasource()
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog(); // Alternative: System.Windows.Forms.OpenFileDialog
            ofd.InitialDirectory = "C:\\Users\\savinov\\git\\samm\\Test";
            ofd.Filter = "Access Files (*.CSV)|*.CSV|All files (*.*)|*.*";
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() != true) return;

            string filePath = ofd.FileName;
            string safeFilePath = ofd.SafeFileName;
            string fileDir = System.IO.Path.GetDirectoryName(filePath);
            string tableName = safeFilePath.Replace('.', '#');

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + fileDir + "; Extended Properties='Text;Excel 12.0;HDR=Yes;FMT=CSVDelimited;'";

            //
            // Initialize a new data source schema
            //
            ConnectionOledb conn = new ConnectionOledb();
            conn.ConnectionString = connectionString;

            SetTopOledb top = new SetTopOledb("");
            top.connection = conn;
            top.LoadSchema(); // Load complete schema
            CsTable sourceTable = top.FindTable(tableName);

            //
            // Configure import by creating a mapping for import dimensions
            //

            CsSchema schema = MashupTop;
            CsTable targetTable = schema.CreateTable(tableName);
            schema.AddTable(targetTable, null, null);

            Mapper mapper = new Mapper(); // Create mapping for an import dimension
            Mapping map = mapper.CreatePrimitive(sourceTable, targetTable); // Complete mapping (all to all)
            map.Matches.ForEach(m => m.TargetPath.Path.ForEach(p => p.Add()));

            CsColumn dim = new Dim(map); // Create generating/import column with this mapping
            dim.Add();

            // Populate this new table (in fact, can be done separately during Update)
            targetTable.TableDefinition.Populate();
        }

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

            //top.ConnectionString = connectionString;

            //top.Open();
            //top.ImportSchema();

            //Sources.Add(top); // Append to the list of data sources

            // And also append to the tree model
            SubsetTree sourceModel = new SubsetTree(top.Root.SuperDim);
            sourceModel.ExpandTree();
            //SourcesModel.Add(sourceModel);
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

        public void Wizard_FilteredTable(CsTable parent)
        {
            /*
            //
            // Create new subset
            //
            CsTable dstSet = new Set("My Table");
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
            */
        }

        public void Wizard_ExtractTable(CsTable set)
        {
            /*
            //
            // Show parameters for set extraction
            //
            ExtractTableBox dlg = new ExtractTableBox();
            dlg.Owner = this;
            dlg.ProjectedSet = set;
            dlg.ProjectionDims = new List<Dim>();
            dlg.ProjectionDims.AddRange(set.GreaterDims);
            dlg.ExtractedSetName = "My Extracted Table";
            dlg.ExtractedDimName = "My Extracted Dimension";
            if (SelectedMashupDim != null)
            {
                dlg.projectionDims.SelectedItem = SelectedMashupDim;
                dlg.ExtractedSetName = SelectedMashupDim.Name + " Group"; // The new table will have the same name as the only extracted dimension
                dlg.ExtractedDimName = SelectedMashupDim.Name + " Group";
            }

            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            if (string.IsNullOrWhiteSpace(dlg.ExtractedSetName) || string.IsNullOrWhiteSpace(dlg.ExtractedDimName) || dlg.projectionDims.SelectedItems.Count == 0) return;

            // Initialize a list of selected dimensions (from the whole list of all greater dimensions
            List<Dim> projectionDims = new List<Dim>();
            foreach (var item in dlg.projectionDims.SelectedItems)
            {
                projectionDims.Add((Dim)item);
            }

            //
            // Create a new (extracted) set
            //
            Set extractedSet = new Set(dlg.ExtractedSetName);
            set.SuperSet.AddSubset(extractedSet);

            //
            // Create identity dimensions for the extracted set and their mapping to the projection dimensions
            //
            Mapping mapping = new Mapping(set, extractedSet);
            foreach (Dim projDim in projectionDims)
            {
                CsTable idSet = projDim.GreaterSet;
                CsColumn idDim = idSet.CreateDefaultLesserDimension(projDim.Name, extractedSet);
                idDim.IsIdentity = true;
                idDim.Add();

                mapping.AddMatch(new PathMatch(new DimPath(projDim), new DimPath(idDim))); 
            }

            //
            // Create a new (mapped) dimension to the new set
            //
            CsColumn extractedDim = extractedSet.CreateDefaultLesserDimension(dlg.ExtractedDimName, set);
            extractedDim.Mapping = mapping;
            extractedDim.Add();
            extractedDim.GreaterSet.ProjectDimensions.Add(extractedDim);

            // 
            // Populate the set and the dimension. The dimension is populated precisely as any (mapped) dimension
            //
            extractedSet.Populate();

            extractedDim.ComputeValues();
            */
        }

        public void Wizard_AddAggregation(CsTable srcSet, CsTable dstSet, CsColumn dstDim)
        {
            /*
            // Source set is where we want to create a new (source) derived dimension
            // Target set and dimension is what we want to use in the definition of the new dimension

            if (dstSet == null && dstDim != null) dstSet = dstDim.LesserSet;

            //
            // Show recommendations and let the user choose one of them
            //
            AggregationBox dlg = new AggregationBox(srcSet, dstSet, dstDim);
            dlg.Owner = this;
            dlg.ShowDialog(); // Open the dialog box modally 
            dlg.RefreshAll();

            if (dlg.DialogResult == false) return; // Cancel

            if (dlg.Recommendations.IsValidExpression() != null) return;

            RecommendedAggregations recoms = dlg.Recommendations;

            //
            // Create new derived dimension
            // Example: (Customers) <- (Orders) <- (Order Details) -> (Products) -> List Price
            //
            string derivedDimName = dlg.SourceColumn.Text;
            CsColumn aggregDim = (CsColumn)recoms.MeasureDimensions.SelectedObject;
            Com.Model.Expression aggreExpr = recoms.GetExpression();

            CsColumn derivedDim = aggregDim.GreaterSet.CreateDefaultLesserDimension(derivedDimName, srcSet);
            derivedDim.Add();

            var funcExpr = ExpressionScope.CreateFunctionDeclaration(derivedDim.Name, derivedDim.LesserSet.Name, derivedDim.GreaterSet.Name);
            funcExpr.Statements[0].Input = aggreExpr; // Return statement
            funcExpr.ResolveFunction(derivedDim.LesserSet.Top);
            funcExpr.Resolve();

            derivedDim.SelectExpression = funcExpr;

            // Update new derived dimension
            derivedDim.ComputeValues();
            */
        }

        public void Wizard_AddCalculation(CsTable srcSet)
        {
            /*
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

            //
            // Create new derived dimension
            // Example: (Customers) <- (Orders) <- (Order Details) -> (Products) -> List Price
            //
            string derivedDimName = dlg.sourceColumn.Text;
            Com.Model.Expression calcExpr = dlg.ExpressionModel[0];
            CsTable dstSet = calcExpr.OutputSet;

            CsColumn derivedDim = dstSet.CreateDefaultLesserDimension(derivedDimName, srcSet);
            derivedDim.Add();

            var funcExpr = ExpressionScope.CreateFunctionDeclaration(derivedDim.Name, derivedDim.LesserSet.Name, derivedDim.GreaterSet.Name);
            funcExpr.Statements[0].Input = calcExpr; // Return statement
            funcExpr.ResolveFunction(derivedDim.LesserSet.Top);
            funcExpr.Resolve();

            derivedDim.SelectExpression = funcExpr;

            // Update new derived dimension
            derivedDim.ComputeValues();
            */
        }

        public void Wizard_ChangeType(CsColumn dim, CsTable newTypeSet)
        {
            /*
            if (dim == null) return; // We must know the dimension the type of which we want to change

            //
            // Suggest the best new type for the new dimension if it has not been specified
            //
            if (newTypeSet == null)
            {
                // Compare quality of mappings from the old type to all possible other sets
                // The possible sets: not old, not the lesser, primitive?, no cycles (here we need an algorithm for detecting cycles)
                Mapper m = new Mapper();
                m.MaxMappingsToBuild = 100;

                CsTable bestSet = null;
                double bestSimilarity = 0.0;
                List<CsTable> newTypes = dim.LesserSet.GetPossibleGreaterSets();
                foreach (CsTable set in newTypes)
                {
                   CsColumn dim2 = set.CreateDefaultLesserDimension(dim.Name, dim.LesserSet);

                    List<Mapping> mappings = m.MapDim(new DimPath(dim), new DimPath(dim2));
                    if (mappings[0].Similarity > bestSimilarity) { bestSet = set; bestSimilarity = mappings[0].Similarity; }
                    m.Mappings.Clear();
                }

                newTypeSet = bestSet;
            }

            if (newTypeSet == null) return; // New type is not found (say, because of having no other tables)

            Dim newDim = newTypeSet.CreateDefaultLesserDimension(dim.Name, dim.LesserSet); // The task is to build a mapping for this new dimension (it is not added yet)

            //
            // Show type change dialog
            //
            ChangeTypeBox dlg = new ChangeTypeBox(dim, newDim);
            dlg.Owner = this;
            dlg.RefreshAll();

            dlg.ShowDialog(); // Open the dialog box modally 

            if (dlg.DialogResult == false) return; // Cancel

            //
            // Really changing the range
            //

            // The result mapping is between two types (new and old sets) but in the dim def it must be from newDim.LesserSet to newDim.GreaterSet
            // So insert a prefix to the mappings
            dlg.MappingModel.Mapping.InsertFirst(new DimPath(dim), null);

            newDim.Mapping = dlg.MappingModel.Mapping;
            newDim.Add();
            newDim.ComputeValues(); // Compute the values of the new dimension
            */
        }

        #endregion

        #region Operations (no user interactions)

        public void Operation_OpenTable(CsTable set)
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
            // Conditions for new aggregated column: dimension is dropped on a set
            //
            if (dropSource is Dim && ((MainWindow)App.Current.MainWindow).IsInMashups((Dim)dropSource))
            {
                if (dropTarget is Set && !(((Set)dropTarget).Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups((Set)dropTarget))
                {
                    // Note that here source and target in terms of DnD have opposite interpretations to the aggregation method
                    ((MainWindow)App.Current.MainWindow).Wizard_AddAggregation((Set)dropTarget, ((Dim)dropSource).LesserSet, (Dim)dropSource);
                }
            }

            //
            // Conditions for type change: a set is dropped on a dimension
            //
            if (dropSource is Set && !(((Set)dropSource).Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups((Set)dropSource))
            {
                if (dropTarget is Dim && ((MainWindow)App.Current.MainWindow).IsInMashups((Dim)dropTarget))
                {
//                    ((MainWindow)App.Current.MainWindow).Wizard_ChangeType((Dim)dropTarget, (Set)dropSource);
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
                CsTable set = ((SubsetTree)data).LesserSet;
                if(!(set.Name == "Root") && ((MainWindow)App.Current.MainWindow).IsInMashups(set)) 
                {
                    // Call a direct operation method for opening a table with the necessary parameters (rather than a command)
                    ((MainWindow)App.Current.MainWindow).Operation_OpenTable(set);
                }
            }
        }
    }

}
