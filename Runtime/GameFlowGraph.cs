using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFlow.Nodes;
using GameFlow.Ports;
using GameFlow.Utils;
using UnityEngine;

namespace GameFlow
{
    /// <summary>
    /// Represents a graph of flow nodes that can be used to create game logic.
    /// This is the main container for all nodes, groups, and connections in a game flow graph.
    /// </summary>
    [CreateAssetMenu(fileName = "GameFlowGraph", menuName = "Game Flow/Graph", order = 0)]
    public sealed class GameFlowGraph : ScriptableObject, IGameFlow
    {
        [SerializeField]
        private TypeInfo gameFlowType;

        /// <summary>
        /// Gets the list of all nodes in the graph.
        /// </summary>
        [field: SerializeField]
        public List<GameFlowNodeData> Nodes { get; private set; } = new();

        /// <summary>
        /// Gets the list of all groups in the graph.
        /// </summary>
        [field: SerializeField]
        public List<GameFlowGroupData> Groups { get; private set; } = new();

        /// <summary>
        /// Gets the list of all connections (edges) between nodes in the graph.
        /// </summary>
        [field: SerializeField]
        public List<EdgeData> Edges { get; private set; } = new();

        private void OnEnable()
        {
            hideFlags = HideFlags.NotEditable;
        }

        /// <inheritdoc />
        public UniTask RunGraph(IInjectData injectData, CancellationToken cancellationToken)
        {
            var gameFlow = (IGameFlow)Activator.CreateInstance(gameFlowType.SystemType);
            return gameFlow.RunGraph(injectData, cancellationToken);
        }
    }

    /// <summary>
    /// Represents the serializable data for a node in the game flow graph.
    /// </summary>
    [Serializable]
    public sealed class GameFlowNodeData
    {
        /// <summary>
        /// Gets or sets the unique identifier for this node.
        /// </summary>
        [field: SerializeField]
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the position of this node in the graph view.
        /// </summary>
        [field: SerializeField]
        public Vector2 Position { get; set; } = Vector2.zero;

        /// <summary>
        /// Gets or sets if this node is expanded.
        /// </summary>
        [field: SerializeField]
        public bool IsExpanded { get; set; } = true;

        /// <summary>
        /// Gets or sets the type information for this node.
        /// </summary>
        [field: SerializeField]
        public TypeInfo TypeInfo { get; set; } = new();
    }

    /// <summary>
    /// Represents the serializable data for a group of nodes in the game flow graph.
    /// </summary>
    [Serializable]
    public sealed class GameFlowGroupData
    {
        /// <summary>
        /// Gets or sets the unique identifier for this group.
        /// </summary>
        [field: SerializeField]
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of this group.
        /// </summary>
        [field: SerializeField]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the list of node GUIDs that are contained within this group.
        /// </summary>
        [field: SerializeField]
        public List<string> ContainedNodes { get; private set; } = new();
    }

    /// <summary>
    /// Represents the serializable data for a port (input or output) on a node.
    /// </summary>
    [Serializable]
    public sealed class PortData
    {
        /// <summary>
        /// Gets or sets the GUID of the node this port belongs to.
        /// </summary>
        [field: SerializeField]
        public string NodeGuid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of this port.
        /// </summary>
        [field: SerializeField]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets AddInvoke override for this port. 
        /// </summary>
        [field: SerializeField]
        public string AddInvokeOverride { get; set; } = string.Empty;

        /// <summary>
        /// Gets the port information for this port from a node instance.
        /// </summary>
        /// <param name="node">The node instance to get port information from.</param>
        /// <returns>The port information for this port.</returns>
        /// <exception cref="Exception">Thrown when the port is not found on the node.</exception>
        public PortInfo GetPortInfo(IFlowNode node)
        {
            if (node.NodePorts.TryGetValue(Name, out var portInfo))
            {
                return portInfo;
            }

            throw new($"Port {Name} was not found on {node.Name}.");
        }

        /// <summary>
        /// Gets the port information for this port from a node type.
        /// </summary>
        /// <param name="nodeType">The type of node to get port information from.</param>
        /// <returns>The port information for this port.</returns>
        /// <exception cref="Exception">Thrown when the port is not found on the node type.</exception>
        public PortInfo GetPortInfo(Type nodeType)
        {
            if (IFlowNode.Ports.TryGetValue(nodeType, out var lookup))
            {
                if (lookup.TryGetValue(Name, out var portInfo))
                {
                    return portInfo;
                }

                throw new($"Port '{Name}' was not found in type '{nodeType.Name}'.");
            }

            throw new($"Node '{nodeType.Name}' was not found.");
        }
    }

    /// <summary>
    /// Represents a connection between two ports in the game flow graph.
    /// </summary>
    [Serializable]
    public sealed class EdgeData
    {
        /// <summary>
        /// Gets or sets the source port of this connection.
        /// </summary>
        [field: SerializeField]
        public PortData FromPort { get; set; }

        /// <summary>
        /// Gets or sets the destination port of this connection.
        /// </summary>
        [field: SerializeField]
        public PortData ToPort { get; set; }
    }

    /// <summary>
    /// Represents type information that can be serialized and used to reconstruct types at runtime.
    /// </summary>
    [Serializable]
    public struct TypeInfo : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string assemblyQualifiedName;

        /// <summary>
        /// Gets or sets the assembly-qualified name of the type.
        /// </summary>
        public string AssemblyQualifiedName
        {
            get => assemblyQualifiedName;
            set
            {
                assemblyQualifiedName = value;
                UpdateProperties();
            }
        }

        /// <summary>
        /// Gets the full name of the type without the assembly information.
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// Gets the short name of the type (without namespace).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns System.Type from info.
        /// </summary>
        public Type SystemType { get; private set; }

        public TypeInfo(Type systemType) : this()
        {
            assemblyQualifiedName = systemType?.AssemblyQualifiedName ?? string.Empty;
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            try
            {
                TypeUtils.GetNames(Type.GetType(assemblyQualifiedName), out assemblyQualifiedName, out string fullName, out string name);
                SystemType = Type.GetType(assemblyQualifiedName);
                FullName = fullName;
                Name = name;
            }
            catch
            {
                // Ignored
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            UpdateProperties();
        }
    }
}