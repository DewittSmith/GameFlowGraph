using System.Collections.Generic;
using System.Text;
using GameFlow.Editor.Controls;
using GameFlow.Editor.Graph;
using GameFlow.Editor.Utils;
using GameFlow.Nodes;
using GameFlow.Utils;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFlow.Editor.Windows
{
    public sealed class GameFlowGraphWindow : EditorWindow
    {
        private const string WindowTitle = "Game Flow Graph";
        private const string EditPath = "Assets/Edit Game Flow";
        private const string GraphModifiedMessage = "Graph Modified";

        private static GameFlowGraph selectedGraph;
        private static GameFlowGraphWindow window;

        [SerializeField]
        private GameFlowGraph graphAsset;

        [SerializeField]
        private GameFlowGraph originalGraph;

        [SerializeField]
        private Vector3 translation;

        [SerializeField]
        private Vector3 scale;

        private GameFlowGraphView graphView;

        private bool isModificationLocked;
        private bool isWaitingForSize = true;

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void Update()
        {
            if (graphView == null || !isWaitingForSize)
            {
                return;
            }

            graphView.Recenter();
            isWaitingForSize = false;
        }

        private void OnUndoRedoPerformed()
        {
            if (graphAsset == null || !AssetDatabase.Contains(graphAsset))
            {
                return;
            }

            var viewTransform = graphView.viewTransform;
            CreateGUI();

            graphView.UpdateViewTransform(viewTransform.position, viewTransform.scale);

            hasUnsavedChanges = true;
        }

        private static void OpenWindow()
        {
            window = CreateWindow<GameFlowGraphWindow>(WindowTitle);
            window.saveChangesMessage = "This window has unsaved changes. Would you like to save?";
            window.originalGraph = Instantiate(selectedGraph);
        }

        private void OnFocus()
        {
            window = this;
            GameFlowGraphView.FocusedInstance = graphView;
        }

        private static GameFlowGraph CreateDefaultGraph()
        {
            var graph = CreateInstance<GameFlowGraph>();
            graph.name = "New Game Flow Graph";

            const float Offset = 100;

            graph.Nodes.Add(new()
            {
                Guid = GuidUtils.Generate(),
                Position = new(-Offset, 0),
                IsExpanded = true,
                TypeInfo = new(typeof(EnterNode))
            });

            return graph;
        }

        [MenuItem("Window/Game Flow Graph")]
        private static void OpenWindowMenu()
        {
            selectedGraph = CreateDefaultGraph();
            OpenWindow();
        }

        [MenuItem(EditPath, false, 1100)]
        private static void EditGameFlow()
        {
            selectedGraph = (GameFlowGraph)Selection.activeObject;
            OpenWindow();
        }

        [MenuItem(EditPath, true, 1100)]
        private static bool EditGameFlowValidate()
        {
            return Selection.activeObject is GameFlowGraph;
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            var graphAsset = obj as GameFlowGraph;
            if (graphAsset == null)
            {
                return false;
            }

            selectedGraph = graphAsset;
            OpenWindow();

            return true;
        }

        [MenuItem("Hidden/ %s")]
        private static void Save()
        {
            if (window != null && window.hasUnsavedChanges)
            {
                window.SaveChanges();
            }
        }

        public void SetModificationLock(bool isLocked)
        {
            isModificationLocked = isLocked;
        }

        public void SetDirty(bool value)
        {
            if (isModificationLocked)
            {
                return;
            }

            hasUnsavedChanges = value;
            UpdateAsset(out _, out _);
        }

        private void UpdateAsset(out int enterCount, out List<GameFlowNodeData> invalidNodes)
        {
            var invalidNodesLocal = new List<GameFlowNodeData>();
            int enterCountLocal = 0;

            enterCount = 0;
            invalidNodes = invalidNodesLocal;

            if (graphView == null)
            {
                return;
            }

            // Create a new Undo group for asset modifications
            Undo.RecordObject(graphAsset, GraphModifiedMessage);

            graphAsset.Nodes.Clear();
            graphAsset.Edges.Clear();
            graphAsset.Groups.Clear();

            graphView.graphElements.ForEach(graphElement =>
            {
                switch (graphElement)
                {
                    case GameFlowNode node:
                    {
                        var nodeData = (GameFlowNodeData)node.userData;
                        nodeData.Position = node.GetPosition().position;
                        nodeData.IsExpanded = node.expanded;

                        switch (node.FlowNode)
                        {
                            case MissingNode:
                                nodeData.Guid = GuidUtils.Zero;
                                invalidNodesLocal.Add(nodeData);
                                break;
                            case EnterNode:
                                ++enterCountLocal;
                                break;
                        }

                        graphAsset.Nodes.Add(nodeData);
                        break;
                    }
                    case GameFlowGroup group:
                        var groupData = (GameFlowGroupData)group.userData;
                        groupData.Name = group.title;
                        graphAsset.Groups.Add(groupData);
                        break;
                    case Edge edge:
                        var edgeData = (EdgeData)edge.userData;
                        graphAsset.Edges.Add(edgeData);
                        break;
                }
            });

            enterCount = enterCountLocal;
            invalidNodes = invalidNodesLocal;
        }

        public override void SaveChanges()
        {
            if (!AssetDatabase.Contains(graphAsset))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Flow Graph", graphAsset.name, "asset", "Please enter a file name to save the texture to");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                AssetDatabase.CreateAsset(graphAsset, path);
            }
            else
            {
                // Register complete undo before modifying existing asset
                Undo.RegisterCompleteObjectUndo(graphAsset, "Save Graph");
            }

            UpdateAsset(out int enterCount, out var invalidNodes);

            var sb = new StringBuilder();

            if (enterCount != 1)
            {
                sb.AppendLine("Graph should contain exactly one Enter node.");
            }

            if (invalidNodes.Count > 0)
            {
                sb.AppendLine("Invalid nodes found:");
                foreach (var nodeData in invalidNodes)
                {
                    sb.AppendLine($"    {nodeData.TypeInfo.FullName}");
                }
            }

            if (sb.Length > 0)
            {
                EditorUtility.DisplayDialog("Could not save the graph asset", sb.ToString(), "OK");
                RevertData();
                return;
            }

            EditorUtility.SetDirty(graphAsset);
            AssetDatabase.SaveAssets();

            originalGraph = Instantiate(graphAsset);

            base.SaveChanges();
        }

        private void RevertData()
        {
            graphAsset.Nodes.Clear();
            graphAsset.Edges.Clear();
            graphAsset.Groups.Clear();

            graphAsset.Nodes.AddRange(originalGraph.Nodes);
            graphAsset.Edges.AddRange(originalGraph.Edges);
            graphAsset.Groups.AddRange(originalGraph.Groups);
        }

        public override void DiscardChanges()
        {
            RevertData();
            CreateGUI();
            base.DiscardChanges();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            // Add variables style.
            var styleSheet = Resources.Load<StyleSheet>("GameFlowStyles/GameFlowVariables");
            rootVisualElement.styleSheets.Add(styleSheet);

            // Add graph view.
            if (selectedGraph != null)
            {
                graphAsset = selectedGraph;
            }

            graphView = new(this, graphAsset);

            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);

            if (translation.sqrMagnitude > 0 && scale.sqrMagnitude > 0)
            {
                graphView.UpdateViewTransform(translation, scale);
            }

            graphView.viewTransformChanged = view =>
            {
                translation = view.viewTransform.position;
                scale = view.viewTransform.scale;
            };

            // Add toolbar.
            var toolbar = new Toolbar();
            var assetField = new ObjectField("Asset")
            {
                allowSceneObjects = false,
                objectType = typeof(GameFlowGraph),
                enabledSelf = false
            };

            var serializedObject = new SerializedObject(this);
            assetField.BindProperty(serializedObject.FindProperty(nameof(graphAsset)));

            toolbar.Add(assetField);
            rootVisualElement.Add(toolbar);

            graphView.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.actionKey)
            {
                if (evt.keyCode == KeyCode.S)
                {
                    SaveChanges();
                    evt.StopPropagation(); // Prevent event from bubbling further
                }
            }
        }
    }
}