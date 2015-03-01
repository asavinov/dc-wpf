using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Com.Model;

namespace Samm.Dialogs
{
    /// <summary>
    /// It stores all necessary information for editing a mapping and the current state of mapping. 
    /// </summary>
    public class MappingModel
    {
        public MatchTree SourceTree { get; private set; }
        public MatchTree TargetTree { get; private set; }

        private Mapper mapper; // We use it for recommending mappings

        public Mapping Mapping { get; set; } // It is the current state of the mapping. And it is what is initialized and returned. 

        private DcTable _sourceSet;
        public DcTable SourceSet 
        {
            get { return _sourceSet; }
            set 
            {
                Debug.Assert(value != null, "Wrong use: a set in mapping cannot be null (use root instead).");
                if (_sourceSet == value) return;
                _sourceSet = value;

                Mapping.SourceSet = SourceSet; // Update mapper

                // Update tree
                SourceTree.Clear();
                MatchTreeNode node = new MatchTreeNode(SourceSet);
                SourceTree.AddChild(node);
                node.ExpandTree();
                node.AddSourcePaths(Mapping);
            }
        }

        private DcTable _targetSet;
        public DcTable TargetSet
        {
            get { return _targetSet; }
            set
            {
                Debug.Assert(value != null, "Wrong use: a set in mapping cannot be null (use root instead).");
                if (_targetSet == value) return;
                _targetSet = value;

                Mapping.TargetSet = TargetSet; // Update mapper

                //
                // Initialize mapping by recommending best mapping
                //
                mapper.Mappings.Clear();
                //mapper.MapDim(new DimPath(OldDim), new DimPath(NewDim));
                //MappingModel.Mapping = mapper.Mappings[0];
                Mapping = new Mapping(SourceSet, TargetSet);

                // Update tree
                TargetTree.Clear();
                MatchTreeNode node = new MatchTreeNode(TargetSet);
                node.ExpandTree();
                node.AddTargetPaths(Mapping);
                TargetTree.AddChild(node);
            }
        }

        /// <summary>
        /// Primary: returns true if this node has any match and false otherwise (not assigned). 
        /// Secondary: returns true if this node is matched against the currently selected primary node and false otherwise (so only one node in the whole secondary tree is true).
        /// </summary>
        /// <returns></returns>
        public bool IsMatchedSource()
        {
            if (SourceTree.SelectedPath == null) return false;
            return IsMatchedSource(SourceTree.SelectedPath);
        }
        public bool IsMatchedSource(DimPath path)
        {
            PathMatch match = Mapping.GetMatchForSource(path);

            if (match == null) return false;

            if (SourceTree.IsPrimary)
            {
                return true;
            }
            else
            {
                if (TargetTree.SelectedPath == null) return false;
                return match.MatchesTarget(TargetTree.SelectedPath);
            }
        }

        /// <summary>
        /// Primary: returns true if this node has any match and false otherwise (not assigned). 
        /// Secondary: returns true if this node is matched against the currently selected primary node and false otherwise (so only one node in the whole secondary tree is true).
        /// </summary>
        /// <returns></returns>
        public bool IsMatchedTarget()
        {
            if (TargetTree.SelectedPath == null) return false;
            return IsMatchedTarget(TargetTree.SelectedPath);
        }
        public bool IsMatchedTarget(DimPath path)
        {
            PathMatch match = Mapping.GetMatchForTarget(path);

            if (match == null) return false;

            if (TargetTree.IsPrimary)
            {
                return true;
            }
            else
            {
                if (SourceTree.SelectedPath == null) return false;
                return match.MatchesSource(SourceTree.SelectedPath);
            }
        }

        /// <summary>
        /// Secondary: Enabled/disabled status of a secondary node. Whether the current paths can be added as a new match without contradiction to the existing matches.
        /// Secondary: given a primary currently selected node, compute if a match with this secondary node does not contradict to existing matches (so it can be added). Alternatively, if relevances are precomputed then we find if relevance is higher than 0.
        /// Primary: always true (if there is at least one possible secondary match). 
        /// </summary>
        public bool CanMatchTarget(DimPath path)
        {
            if (TargetTree.IsPrimary)
            {
                return true;
            }
            else
            {
                DimPath priPath = SourceTree.SelectedPath;
                if (priPath == null) return false; // Primary node is not selected

                if (!priPath.IsPrimitive || !path.IsPrimitive) return false; // Only primitive paths can be matchd

                return true;
            }
        }

        public bool CanMatchSource(DimPath path)
        {
            // TODO: Copy-paste when ready
            return true;
        }

        /// <summary>
        /// Best target for the current source.
        /// </summary>
        public void SelectBestTarget()
        {
            DimPath sourcePath = SourceTree.SelectedPath;
            if (sourcePath == null) return; // Primary node is not selected

            if (!sourcePath.IsPrimitive) return; // Only primitive paths can be matchd

            DimPath pargetPath = mapper.MapDim(sourcePath, TargetSet); // Find best path starting from the target set and corresponding to the source path

            TargetTree.SelectedPath = pargetPath;
        }

        /// <summary>
        /// Primary: does not do anything (or calls the same method of the secondary tree). 
        /// Secondary: takes the currently selected primary node and this secondary node and adds this match to the list. Previous match is deleted. Contradictory matches are removed. Match status of nodes needs to be redrawn.
        /// </summary>
        public PathMatch AddMatch()
        {
            if (SourceTree.SelectedPath == null || TargetTree.SelectedPath == null) return null;

            PathMatch match = new PathMatch(SourceTree.SelectedPath, TargetTree.SelectedPath, 1.0);
            Mapping.AddMatch(match); // Some existing matches (which contradict to the added one) will be removed

            return match;
        }

        /// <summary>
        /// Remove the mathc corresponding to the current selections. 
        /// </summary>
        public void RemoveMatch()
        {
            if (SourceTree.SelectedPath == null || TargetTree.SelectedPath == null) return;

            Mapping.RemoveMatch(SourceTree.SelectedPath, TargetTree.SelectedPath); // Also other matches can be removed
        }


        public static List<DcTable> GetPossibleGreaterSets(DcTable table)
        {
            // Possible target sets: all sets from the schema excepting: 
            // not source, not lesser, primitive?, no cycles (here we need an algorithm for detecting cycles)

            List<DcTable> all = table.Schema.Root.AllSubTables;
            List<DcTable> result = new List<DcTable>();
            foreach (DcTable set in all)
            {
                if (set == table) continue; // We cannot point to itself
                if (set.IsInput(table)) continue; // We cannot point to a lesser set (cycle)

                result.Add(set);
            }

            return result;
        }

        public static List<DcTable> GetPossibleLesserSets(DcTable table) // Fact sets
        {
            // Possible target sets: all sets from the schema excepting: 
            // not source, not greater, there is lesser path

            List<DcTable> all = table.Schema.Root.AllSubTables;
            List<DcTable> result = new List<DcTable>();
            foreach (DcTable set in all)
            {
                if (set == table) continue; // We cannot point to itself
                if (!set.IsInput(table)) continue;

                result.Add(set);
            }

            return result;
        }

        public MappingModel(DcColumn sourceDim, DcColumn targetDim)
            : this(sourceDim.Output, targetDim.Output)
        {
            SourceTree.Children[0].Dim = sourceDim;
            TargetTree.Children[0].Dim = targetDim;
        }

        public MappingModel(DcTable sourceSet, DcTable targetSet)
        {
            mapper = new Mapper();
            mapper.MaxMappingsToBuild = 100;

            Mapping = new Mapping(sourceSet, targetSet);

            SourceTree = new MatchTree(this);
            SourceTree.IsPrimary = true;
            TargetTree = new MatchTree(this);
            TargetTree.IsPrimary = false;

            SourceSet = sourceSet; // Here also the tree will be constructed
            TargetSet = targetSet;
        }

        public MappingModel(Mapping mapping)
        {
            mapper = new Mapper();
            mapper.MaxMappingsToBuild = 100;

            Mapping = mapping;

            SourceTree = new MatchTree(this);
            SourceTree.IsPrimary = true;
            TargetTree = new MatchTree(this);
            TargetTree.IsPrimary = false;

            SourceSet = mapping.SourceSet; // Here also the tree will be constructed
            TargetSet = mapping.TargetSet;
        }
    }

    /// <summary>
    /// It displays the current state of mapping between two sets as properties of the tree nodes depending on the role of the tree. 
    /// </summary>
    public class MatchTree : MatchTreeNode
    {
        public MappingModel MappingModel { get; set; }

        public bool IsSource { get { return MappingModel.SourceTree == this;  } } // Whether this tree corresponds to source paths in the mappings.
        public bool IsTarget { get { return MappingModel.TargetTree == this; } } // Whether this tree corresponds to target paths in the mappings. 

        public bool IsPrimary { get; set; } // Defines how the node properties are computed and displayed as well as the logic of the tree. 
        public bool OnlyPrimitive { get; set; } // Only primitive dimensions/paths can be matched (not intermediate). So intermediate elemens are not matched (but might display information about matches derived from primitive elements).

        // This is important for generation of the current status: disabled/enabled, relevance etc.
        public MatchTreeNode SelectedNode { get; set; } // Selected in this tree. Bind tree view selected item to this field.
        public DimPath SelectedPath // Transform selected node into valid selected path
        {
            get
            {
                if (SelectedNode == null) return null;
                DimPath selectedPath = SelectedNode.DimPath;

                // Trimm to source or target set of the mapping in the root
                if(IsSource)
                    selectedPath.RemoveFirst(MappingModel.SourceSet);
                else
                    selectedPath.RemoveFirst(MappingModel.TargetSet);

                return selectedPath;
            }

            set
            {
                // Find node corresponding to the specified path
                DimTree root = Children[0];
                DimTree node = root.FindPath(value);
                SelectedNode = (MatchTreeNode)node;
            }
        }

        public MatchTree CounterTree { get { return IsSource ? MappingModel.TargetTree : MappingModel.SourceTree; } } // Another tree

        public MatchTree(MappingModel model)
            : base()
        {
            MappingModel = model;
        }
    }

    /// <summary>
    /// It provides methods and propoerties for a node which depend on the current mappings and role of the tree. 
    /// The class assumes that the root of the tree is a special node storing mappings, tree roles and other necessary data. 
    /// </summary>
    public class MatchTreeNode : DimTree
    {
        public DimPath MappingPath // Trimmed to source or target set of the mapping in the root
        {
            get
            {
                MatchTree root = (MatchTree)Root;
                DimPath path = DimPath;
                if (root.IsSource)
                    path.RemoveFirst(root.MappingModel.SourceSet);
                else
                    path.RemoveFirst(root.MappingModel.TargetSet);
                return path;
            }
        }

        /// <summary>
        /// Primary: either not shown or 1.0 or relevance of the current (existing) match if it exists for this element retrieved from the mapper. 
        /// Secondary: if it is already matched then relevance of existing match (the same as for primary) and if it is not chosen (not current) then the relevance computed by the recommender. 
        /// </summary>
        public double MatchRelevance
        {
            get
            {
                return 1.0;
            }
        }

        public bool IsMatched
        {
            get
            {
                MatchTree root = (MatchTree)Root;
                MappingModel model = root.MappingModel;

                if (root.IsSource) return model.IsMatchedSource(MappingPath);
                else return model.IsMatchedTarget(MappingPath);
            }
        }

        public bool CanMatch
        {
            get
            {
                MatchTree root = (MatchTree)Root;
                MappingModel model = root.MappingModel;

                if (root.IsSource) return model.CanMatchSource(MappingPath);
                else return model.CanMatchTarget(MappingPath);
            }
        }

        public PathMatch AddMatch()
        {
            MatchTree root = (MatchTree)Root;
            return root.MappingModel.AddMatch();
        }

        /// <summary>
        /// Primary: does not do anything (or calls the same method of the secondary tree). 
        /// Secondary: remove the current match so this secondary is node (and the corresponding primary node) are not matched anymore. Works only if the two nodes are currently matched. 
        /// </summary>
        public PathMatch RemoveMatch()
        {
            throw new NotImplementedException();
        }

        public MatchTreeNode(DcColumn dim, DimTree parent = null)
            : base(dim, parent)
        {
        }

        public MatchTreeNode(DcTable set, DimTree parent = null)
            : base(set, parent)
        {
        }

        public MatchTreeNode()
            : base()
        {
        }
    }

    public enum MappingDirection
    {
        SOURCE, // Data flow in the direction FROM this set to a target set
        TARGET, // Data flow in the direction TO this element from a source set
    }

}
