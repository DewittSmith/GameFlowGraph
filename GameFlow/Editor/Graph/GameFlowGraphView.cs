using System;
using System.Collections.Generic;
using System.Linq;
using GameFlow.Editor.Controls;
using GameFlow.Editor.Windows;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFlow.Editor.Graph
{
    public sealed class GameFlowGraphView : GraphView
    {
        internal static GameFlowGraphView FocusedInstance { get; set; }

        internal GameFlowGraphWindow Window { get; }

        public GameFlowGraphView(GameFlowGraphWindow window, GameFlowGraph data)
        {
            if (data == null)
            {
                Debug.LogError("Tried to create a GameFlowGraphView with a null data!", window);
                return;
            }

            FocusedInstance = this;
            Window = window;

            var searchWindow = ScriptableObject.CreateInstance<GameFlowSearchWindow>();
            searchWindow.Initialize(this);

            nodeCreationRequest = context => SearchWindow.Open(new(context.screenMousePosition), searchWindow);
            graphViewChanged = OnGraphViewChanged;
            elementsAddedToGroup = (_, _) => window.SetDirty(true);
            elementsRemovedFromGroup = (_, _) => window.SetDirty(true);

            AddManipulators();
            AddBackground();
            AddStyles();

            window.SetModificationLock(true);

            var createdNodes = new List<GameFlowNode>();
            foreach (var nodeData in data.Nodes)
            {
                var node = new GameFlowNode(nodeData, data.Edges.Where(x => x.FromPort.NodeGuid == nodeData.Guid || x.ToPort.NodeGuid == nodeData.Guid));

                createdNodes.Add(node);
                AddElement(node);
            }

            foreach (var edgeData in data.Edges)
            {
                var fromNode = createdNodes.Find(x => x.Data.Guid == edgeData.FromPort.NodeGuid);
                var toNode = createdNodes.Find(x => x.Data.Guid == edgeData.ToPort.NodeGuid);

                var fromPort = fromNode.Ports.First(x => ((PortData)x.userData).Name == edgeData.FromPort.Name);
                var toPort = toNode.Ports.First(x => ((PortData)x.userData).Name == edgeData.ToPort.Name);

                var edge = fromPort.ConnectTo(toPort);
                edge.userData = edgeData;
                AddElement(edge);
            }

            foreach (var groupData in data.Groups)
            {
                var containedNodes = groupData.ContainedNodes.ToList();

                groupData.ContainedNodes.Clear();
                var group = new GameFlowGroup(groupData);

                AddElement(group);

                foreach (string containedNodeGuid in containedNodes)
                {
                    var containedNode = createdNodes.Find(x => x.Data.Guid == containedNodeGuid);
                    group.AddElement(containedNode);
                }
            }

            foreach (var gameFlowNode in createdNodes)
            {
                gameFlowNode.RefreshExpandedState();
            }

            window.SetModificationLock(false);
        }

        internal void Recenter()
        {
            const int Border = 10;

            var rectToFit = CalculateRectToFitAll(contentViewContainer);
            CalculateFrameTransform(rectToFit, layout, Border, out var frameTranslation, out var frameScaling);
            UpdateViewTransform(frameTranslation, frameScaling);
        }

        internal GameFlowGroup AddGroup(Vector2 screenMousePosition)
        {
            const string DefaultGroupName = "Group";

            var group = new GameFlowGroup { title = DefaultGroupName };

            AddElement(group, screenMousePosition);
            foreach (var selectedElement in selection)
            {
                if (selectedElement is GameFlowNode gameFlowNode)
                {
                    group.AddElement(gameFlowNode);
                }
            }

            return group;
        }

        internal GameFlowNode AddNode(Type type, Vector2 screenMousePosition)
        {
            var node = new GameFlowNode(type);
            AddElement(node, screenMousePosition);
            return node;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            var startPortNode = (GameFlowNode)startPort.node;
            var startPortInfo = ((PortData)startPort.userData).GetPortInfo(startPortNode.FlowNode);

            ports.ForEach(port =>
            {
                if (startPort == port)
                {
                    return;
                }

                if (startPort.direction == port.direction)
                {
                    return;
                }

                var portNode = (GameFlowNode)port.node;
                var portInfo = ((PortData)port.userData).GetPortInfo(portNode.FlowNode);

                var (start, end) = (startPortInfo, portInfo);
                if (startPort.direction == Direction.Input)
                {
                    (start, end) = (end, start);
                }

                if (start.ParameterType != end.ParameterType || start.ResultType != end.ResultType)
                {
                    return;
                }

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        public void AddElement(GraphElement graphElement, Vector2 screenPosition)
        {
            Window.SetDirty(true);

            var localPosition = contentViewContainer.WorldToLocal(screenPosition - Window.position.position);
            graphElement.SetPosition(new(localPosition, Vector2.zero));
            AddElement(graphElement);
        }

        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale * 2);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }

        private void AddBackground()
        {
            var background = new GridBackground();
            background.StretchToParentSize();

            Insert(0, background);
        }

        private void AddStyles()
        {
            var styleSheet = Resources.Load<StyleSheet>("GameFlowStyles/GameFlowGraphView");
            styleSheets.Add(styleSheet);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            Window.SetDirty(true);

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var outputData = (PortData)edge.output.userData;
                    var inputData = (PortData)edge.input.userData;
                    edge.userData = new EdgeData { FromPort = outputData, ToPort = inputData };
                }
            }

            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var graphElement in graphViewChange.elementsToRemove)
                {
                    if (graphElement is Edge edge)
                    {
                        edge.userData = new EdgeData { FromPort = new(), ToPort = new() };
                    }
                }
            }

            return graphViewChange;
        }
    }
}