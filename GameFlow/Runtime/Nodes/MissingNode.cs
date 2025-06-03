using System.Collections.Generic;
using GameFlow.Attributes;
using GameFlow.Ports;

namespace GameFlow.Nodes
{
    /// <summary>
    /// A node used as a placeholder when a node type is missing or cannot be loaded.
    /// This allows the graph to remain valid even if some node types are unavailable.
    /// </summary>
    [SearchHidden]
    public struct MissingNode : IFlowNode
    {
        /// <inheritdoc />
        public string Name { get; }

        private readonly IReadOnlyDictionary<string, PortInfo> ports;

        /// <summary>
        /// Gets the ports for this node, as defined by the missing type.
        /// </summary>
        IReadOnlyDictionary<string, PortInfo> IFlowNode.NodePorts => ports;

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingNode"/> class.
        /// </summary>
        /// <param name="typeInfo">The type information for the missing node.</param>
        /// <param name="ports">The ports that the missing node would have had.</param>
        public MissingNode(TypeInfo typeInfo, IReadOnlyDictionary<string, PortInfo> ports)
        {
            Name = typeInfo.FullName;
            this.ports = ports;
        }
    }
}