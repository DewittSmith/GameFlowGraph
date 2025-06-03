using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFlow.Attributes;
using GameFlow.Editor.Graph;
using GameFlow.Nodes;
using GameFlow.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GameFlow.Editor.Windows
{
    public sealed class GameFlowPortSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private GameFlowGraphView graphView;
        private Type parameterType;
        private Type resultType;
        private bool isInput;

        private Texture2D indentationIcon;

        private Port inputPort;
        private Port outputPort;

        public void Initialize(GameFlowGraphView graphView, Type parameterType, Type resultType, bool isInput)
        {
            this.graphView = graphView;
            this.parameterType = parameterType;
            this.resultType = resultType;
            this.isInput = isInput;

            indentationIcon = new(1, 1);
            indentationIcon.SetPixel(0, 0, Color.clear);
            indentationIcon.Apply();
        }

        public void SetEdge(Edge edge)
        {
            inputPort = edge.input;
            outputPort = edge.output;
        }

        private void OnDestroy()
        {
            DestroyImmediate(indentationIcon);
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new("Create Node"))
            };

            var otherFolders = new HashSet<string>();
            var createdFolders = new HashSet<string>();
            var groupedEntries = new Dictionary<string, List<PortCreationInfo>>();

            foreach (var nodeType in FlowNodeUtils.FindAllNodeTypes())
            {
                var type = nodeType;

                // Ignore if hidden.
                if (type.GetCustomAttribute(typeof(SearchHiddenAttribute)) != null)
                {
                    continue;
                }

                if (type.IsGenericType)
                {
                    var generateGenericSubclasses = type.GetCustomAttribute<GenerateGenericSubclassesAttribute>();

                    if (generateGenericSubclasses == null)
                    {
                        continue;
                    }

                    switch (type.GetGenericArguments().Length)
                    {
                        case 1 when generateGenericSubclasses.ParameterTypeBase != null:
                        {
                            if (!generateGenericSubclasses.ParameterTypeBase.IsAssignableFrom(parameterType))
                            {
                                continue;
                            }

                            type = nodeType.MakeGenericType(parameterType);
                            break;
                        }
                        case 1 when generateGenericSubclasses.ResultTypeBase != null:
                        {
                            if (!generateGenericSubclasses.ResultTypeBase.IsAssignableFrom(resultType))
                            {
                                continue;
                            }

                            type = nodeType.MakeGenericType(resultType);
                            break;
                        }
                        case 2:
                        {
                            if (generateGenericSubclasses.ParameterTypeBase == null ||
                                generateGenericSubclasses.ResultTypeBase == null)
                            {
                                continue;
                            }

                            if (!generateGenericSubclasses.ParameterTypeBase.IsAssignableFrom(parameterType) ||
                                !generateGenericSubclasses.ResultTypeBase.IsAssignableFrom(resultType))
                            {
                                continue;
                            }

                            type = nodeType.MakeGenericType(parameterType, resultType);
                            break;
                        }
                        default:
                            continue;
                    }
                }

                var instance = TypeUtils.CreateDefaultNode(type);
                if (!groupedEntries.TryGetValue(instance.Folder, out var list))
                {
                    list = new();
                    groupedEntries.Add(instance.Folder, list);
                    otherFolders.Add(instance.Folder);
                }

                foreach (var portInfo in instance.NodePorts.Values)
                {
                    if (portInfo.IsInput == isInput)
                    {
                        continue;
                    }

                    if (portInfo.ParameterType != parameterType || portInfo.ResultType != resultType)
                    {
                        continue;
                    }

                    list.Add(new(type, instance.Name, portInfo.Name));
                }

                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            otherFolders.Remove(NodeFolders.ApplicationFolder);
            otherFolders.Remove(NodeFolders.ControlFlowFolder);
            otherFolders.Remove(NodeFolders.CustomFolder);

            AddFolder(NodeFolders.ApplicationFolder);
            AddFolder(NodeFolders.ControlFlowFolder);

            foreach (string otherFolder in otherFolders)
            {
                AddFolder(otherFolder);
            }

            AddFolder(NodeFolders.CustomFolder);

            return entries;

            void AddFolder(string folder)
            {
                if (!groupedEntries.Remove(folder, out var list))
                {
                    return;
                }

                if (list.Count == 0)
                {
                    return;
                }

                int level = 1;
                foreach (string folderEntry in folder.Split('/', StringSplitOptions.RemoveEmptyEntries))
                {
                    string folderName = ObjectNames.NicifyVariableName(folderEntry);
                    if (createdFolders.Add(folderName))
                    {
                        entries.Add(new SearchTreeGroupEntry(new(folderName), level));
                    }

                    level++;
                }

                entries.AddRange(list.Select(x =>
                {
                    string entryName = $"{ObjectNames.NicifyVariableName(x.NodeName)} ({ObjectNames.NicifyVariableName(x.PortName)})";
                    return new SearchTreeEntry(new(entryName, indentationIcon))
                    {
                        level = level,
                        userData = x
                    };
                }));
            }
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is not PortCreationInfo portCreationInfo)
            {
                return false;
            }

            var node = graphView.AddNode(portCreationInfo.NodeType, context.screenMousePosition);
            var nodePort = node.Ports.First(x => ((PortData)x.userData).Name == portCreationInfo.PortName);

            if (nodePort.direction == Direction.Input)
            {
                var edge = nodePort.ConnectTo(outputPort);
                edge.userData = new EdgeData
                {
                    FromPort = (PortData)outputPort.userData,
                    ToPort = (PortData)nodePort.userData
                };

                graphView.AddElement(edge);
            }
            else
            {
                var edge = inputPort.ConnectTo(nodePort);
                edge.userData = new EdgeData
                {
                    FromPort = (PortData)nodePort.userData,
                    ToPort = (PortData)inputPort.userData
                };

                graphView.AddElement(edge);
            }

            return true;
        }

        private sealed class PortCreationInfo
        {
            public Type NodeType { get; }
            public string NodeName { get; }
            public string PortName { get; }

            public PortCreationInfo(Type nodeType, string nodeName, string portName)
            {
                NodeType = nodeType;
                NodeName = nodeName;
                PortName = portName;
            }
        }
    }
}