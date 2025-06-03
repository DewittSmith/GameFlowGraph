using System;
using System.Collections.Generic;
using GameFlow.Editor.Utils;
using GameFlow.Editor.Windows;
using GameFlow.Nodes;
using GameFlow.Ports;
using GameFlow.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GameFlow.Editor.Graph
{
    public sealed class GameFlowNode : Node
    {
        private static readonly Dictionary<(Type, Type), Type> ConstructedTypes = new();

        public GameFlowNodeData Data => (GameFlowNodeData)userData;
        public IReadOnlyList<Port> Ports { get; private set; }
        public IFlowNode FlowNode { get; }

        public GameFlowNode(Type type)
        {
            userData = new GameFlowNodeData { Guid = GuidUtils.Generate(), TypeInfo = new(type) };
            FlowNode = TypeUtils.CreateDefaultNode(type);
            CreateGUI();
        }

        public GameFlowNode(GameFlowNodeData data, IEnumerable<EdgeData> edges)
        {
            userData = data;
            expanded = data.IsExpanded;

            if (data.TypeInfo.SystemType == null)
            {
                var ports = new Dictionary<string, PortInfo>();
                foreach (var edgeData in edges)
                {
                    if (edgeData.FromPort.NodeGuid == data.Guid)
                    {
                        string name = edgeData.FromPort.Name;
                        ports.TryAdd(name, new(name, name, false, typeof(Unit), typeof(Unit), null));
                    }
                    else if (edgeData.ToPort.NodeGuid == data.Guid)
                    {
                        string name = edgeData.ToPort.Name;
                        ports.TryAdd(name, new(name, name, true, typeof(Unit), typeof(Unit), null));
                    }
                    else
                    {
                        throw new("Unrelated edge detected.");
                    }
                }

                FlowNode = new MissingNode(data.TypeInfo, ports);
            }
            else
            {
                FlowNode = TypeUtils.CreateDefaultNode(data.TypeInfo.SystemType);
            }

            SetPosition(new(data.Position, Vector2.zero));

            CreateGUI();
        }

        private void CreateGUI()
        {
            title = FlowNode.Name;

            var ports = new List<Port>();
            foreach (var portInfo in FlowNode.NodePorts.Values)
            {
                var types = (portInfo.ParameterType, portInfo.ResultType);
                if (!ConstructedTypes.TryGetValue(types, out var portType))
                {
                    portType = typeof(ValueTuple<,>).MakeGenericType(portInfo.ParameterType, portInfo.ResultType);
                    ConstructedTypes.Add(types, portType);
                }

                var port = InstantiatePort(
                    Orientation.Horizontal,
                    portInfo.IsInput ? Direction.Input : Direction.Output,
                    Port.Capacity.Multi,
                    portType
                );

                var searchWindow = ScriptableObject.CreateInstance<GameFlowPortSearchWindow>();
                searchWindow.Initialize(GameFlowGraphView.FocusedInstance, portInfo.ParameterType, portInfo.ResultType, portInfo.IsInput);

                port.AddManipulator(new EdgeConnector<Edge>(new DroppableEdgeConnectorListener(searchWindow)));
                port.RegisterCallback<DetachFromPanelEvent>(_ => Object.DestroyImmediate(searchWindow));

                port.portName = ObjectNames.NicifyVariableName(portInfo.DisplayName);
                port.portColor = TypeColors.GetColor(portInfo.ParameterType);
                port.userData = new PortData { NodeGuid = Data.Guid, Name = portInfo.Name, AddInvokeOverride = portInfo.AddInvokeOverride };
                ports.Add(port);

                if (portInfo.IsInput)
                {
                    inputContainer.Add(port);
                }
                else
                {
                    outputContainer.Add(port);
                }
            }

            Ports = ports;
        }

        private sealed class DroppableEdgeConnectorListener : IEdgeConnectorListener
        {
            private readonly GameFlowPortSearchWindow searchWindow;

            public DroppableEdgeConnectorListener(GameFlowPortSearchWindow searchWindow)
            {
                this.searchWindow = searchWindow;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                searchWindow.SetEdge(edge);

                var graphView = GameFlowGraphView.FocusedInstance;
                var worldPoint = position + graphView.Window.position.position;
                SearchWindow.Open(new(worldPoint), searchWindow);
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
            }
        }
    }
}