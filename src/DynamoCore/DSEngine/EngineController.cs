﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.DesignScript.Interfaces;
using Dynamo.Models;
using Dynamo.Utilities;
using ProtoCore.AST.AssociativeAST;
using ProtoCore.Mirror;
using ProtoScript.Runners;

namespace Dynamo.DSEngine
{
    /// <summary>
    /// A controller to coordinate the interactions between some DesignScript
    /// sub components like library managment, live runner and so on.
    /// </summary>
    public class EngineController: IAstNodeContainer
    {
        public static EngineController Instance = new EngineController();
        private LiveRunnerServices liveRunnerServices;
        private LibraryServices libraryServices;
        private AstBuilder astBuilder;
        private SyncDataManager syncDataManager;

        public AstBuilder Builder
        {
            get { return astBuilder; }
        }

        private EngineController()
        {
            libraryServices = new LibraryServices();
            libraryServices.LibraryLoading += this.LibraryLoading;
            libraryServices.LibraryLoadFailed += this.LibraryLoadFailed;
            libraryServices.LibraryLoaded += this.LibraryLoaded;

            liveRunnerServices = new LiveRunnerServices(this);
            liveRunnerServices.ReloadAllLibraries(libraryServices.BuiltinLibraries);

            astBuilder = new AstBuilder(this);

            syncDataManager = new SyncDataManager();

            dynSettings.Controller.DynamoModel.NodeDeleted += NodeDeleted;
        }

        /// <summary>
        /// Load builtin functions and libraries into Dynamo.
        /// </summary>
        public void LoadBuiltinLibraries()
        {
            LoadFunctions(libraryServices[LibraryServices.BuiltInCategories.BUILT_INS]);
            LoadFunctions(libraryServices[LibraryServices.BuiltInCategories.OPERATORS]);

            foreach (var library in libraryServices.BuiltinLibraries)
            {
                LoadFunctions(libraryServices[library]);
            }
        }

        /// <summary>
        /// Import a list of libraries.
        /// </summary>
        /// <param name="libraries"></param>
        public void ImportLibraries(List<string> libraries)
        {
            foreach (string library in libraries)
            {
                if (library.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase) ||
                    library.EndsWith(".ds", StringComparison.InvariantCultureIgnoreCase))
                {
                    libraryServices.ImportLibrary(library);
                }
            }
        }

        /// <summary>
        /// Get DesignScript core.
        /// </summary>
        public ProtoCore.Core LiveRunnerCore
        {
            get
            {
                return liveRunnerServices.Core;
            }
        }

        /// <summary>
        /// Get runtime mirror for variable.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public RuntimeMirror GetMirror(string variableName)
        {
            RuntimeMirror mirror = null;
            try
            {
                mirror = liveRunnerServices.GetMirror(variableName);
            }
            catch (Exception ex)
            {
                DynamoLogger.Instance.Log("Failed to get mirror for variable: " + variableName + "; reason: " + ex.Message);
            }

            return mirror;
        }

        public string ConvertNodesToCode(List<NodeModel> nodesToConvert)
        {
            string code = string.Empty;
            if (nodesToConvert.Count > 0)
            {
                code = Dynamo.DSEngine.NodeToCodeUtils.ConvertNodesToCode(nodesToConvert);
            }
            return code;
        }

        /// <summary>
        /// Get string representation of the value of variable.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public string GetStringValue(string variableName)
        {
            RuntimeMirror mirror = GetMirror(variableName);
            return null == mirror ? "null" : mirror.GetStringData();
        }

        /// <summary>
        /// Get a list of IGraphicItem of variable if it is a geometry object;
        /// otherwise returns null.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public List<IGraphicItem> GetGraphicItems(string variableName)
        {
            RuntimeMirror mirror = GetMirror(variableName);
            return null == mirror ? null : mirror.GetData().GetGraphicsItems();
        }

        /// <summary>
        /// Get graph sync data.
        /// </summary>
        /// <returns></returns>
        public GraphSyncData GetSyncData()
        {
            return syncDataManager.GetSyncData();
        }

        /// <summary>
        /// Update graph with graph sync data.
        /// </summary>
        /// <param name="graphData"></param>
        public void UpdateGraph(GraphSyncData graphData)
        {
            try
            {
                liveRunnerServices.UpdateGraph(graphData);
            }
            catch (Exception e)
            {
                DynamoLogger.Instance.Log("Update graph failed: " + e.Message);
            }
            finally
            {
                syncDataManager.ResetStates();
            }
        }

        /// <summary>
        /// Load DesignScript functions into Dynamo.
        /// </summary>
        /// <param name="functions"></param>
        private void LoadFunctions(List<FunctionItem> functions)
        {
            if (null == functions)
            {
                return;
            }

            var searchViewModel = dynSettings.Controller.SearchViewModel;
            var controller = dynSettings.Controller;

            foreach (var function in functions)
            {
                searchViewModel.Add(function);

                if (!controller.DSImportedFunctions.ContainsKey(function.DisplayName))
                {
                    controller.DSImportedFunctions.Add(function.DisplayName, function);
                }
            }
        }

        /// <summary>
        /// LibraryLoading event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryLoading(object sender, LibraryServices.LibraryLoadingEventArgs e)
        {
        }

        /// <summary>
        /// LibraryLoadFailed event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryLoadFailed(object sender, LibraryServices.LibraryLoadFailedEventArgs e)
        {
        }

        /// <summary>
        /// LibraryLoaded event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LibraryLoaded(object sender, LibraryServices.LibraryLoadedEventArgs e)
        {
            // Load all functions defined in that library.
            string newLibrary = e.LibraryPath;
            LoadFunctions(libraryServices[newLibrary]);

            // Reset the VM
            List<string> libraries = new List<string>();
            libraries.AddRange(libraryServices.BuiltinLibraries);
            libraries.AddRange(libraryServices.ImportedLibraries);
            liveRunnerServices.ReloadAllLibraries(libraries);

            // Mark all nodes as dirty so that AST for the whole graph will be
            // regenerated.
            foreach (var node in dynSettings.Controller.DynamoViewModel.Model.HomeSpace.Nodes)
            {
                node.RequiresRecalc = true;
            }
        }

        #region Implement IAstNodeContainer interface
        public void OnAstNodeBuilding(NodeModel node)
        {
            syncDataManager.MarkForAdding(node.GUID);
        }

        public void OnAstNodeBuilt(NodeModel node, IEnumerable<AssociativeNode> astNodes)
        {
            foreach (var astNode in astNodes)
            {
                syncDataManager.AddNode(node.GUID, astNode); 
            }
        }
        #endregion

        /// <summary>
        /// NodeDeleted event handler.
        /// </summary>
        /// <param name="node"></param>
        private void NodeDeleted(NodeModel node)
        {
            syncDataManager.DeleteNodes(node.GUID);
        }
    }
}