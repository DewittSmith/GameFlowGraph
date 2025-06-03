using System;
using System.Collections.Generic;
using GameFlow.Ports;
using GameFlow.Utils;

namespace GameFlow.Nodes
{
    /// <summary>
    /// Represents a node in the game flow graph system. This interface defines the core functionality
    /// that all flow nodes must implement, including port management and node metadata.
    /// </summary>
    public interface IFlowNode
    {
        private static Dictionary<Type, Dictionary<string, PortInfo>> ports;

        /// <summary>
        /// Gets a read-only dictionary of all node types and their associated port information.
        /// This is populated on first access by scanning all available node types.
        /// </summary>
        public static IReadOnlyDictionary<Type, Dictionary<string, PortInfo>> Ports
        {
            get
            {
                if (ports == null)
                {
                    ports = new();
                    foreach (var nodeType in FlowNodeUtils.FindAllNodeTypes())
                    {
                        if (nodeType.IsGenericType)
                        {
                            continue;
                        }

                        var lookup = new Dictionary<string, PortInfo>();
                        ports.Add(nodeType, lookup);

                        var instance = TypeUtils.CreateDefaultNode(nodeType);
                        foreach (var portInfo in FlowNodeUtils.FindPorts(instance))
                        {
                            lookup.Add(portInfo.Name, portInfo);
                        }

                        if (instance is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }

                return ports;
            }
        }

        /// <summary>
        /// Gets a read-only dictionary of port information for this specific node instance.
        /// The ports are cached per node type for performance.
        /// </summary>
        public IReadOnlyDictionary<string, PortInfo> NodePorts
        {
            get
            {
                var nodeType = GetType();
                if (!Ports.TryGetValue(nodeType, out var lookup))
                {
                    lookup = new();
                    ports.Add(nodeType, lookup);

                    foreach (var portInfo in FlowNodeUtils.FindPorts(this))
                    {
                        lookup.Add(portInfo.Name, portInfo);
                    }
                }

                return lookup;
            }
        }

        /// <summary>
        /// Gets the folder path where this node should be displayed in the node creation menu.
        /// Default is the custom folder.
        /// </summary>
        string Folder => NodeFolders.CustomFolder;

        /// <summary>
        /// Gets the display name of this node type.
        /// </summary>
        string Name { get; }
    }
}