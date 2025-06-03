using GameFlow.Ports;
using GameFlow.Utils;

namespace GameFlow.Nodes
{
    /// <summary>
    /// A node that represents the entry point of a game flow graph. When the graph starts, this node is triggered.
    /// </summary>
    public sealed class EnterNode : IFlowNode
    {
        /// <inheritdoc />
        public string Folder => NodeFolders.ApplicationFolder;

        /// <inheritdoc />
        public string Name => "Enter";

        /// <summary>
        /// Gets the output port that is triggered when the graph starts.
        /// </summary>
        public FlowOutput OnEnter { get; } = new();
    }
}