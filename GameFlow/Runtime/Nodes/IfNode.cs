using System.Threading;
using Cysharp.Threading.Tasks;
using GameFlow.Ports;
using GameFlow.Utils;

namespace GameFlow.Nodes
{
    /// <summary>
    /// A node that branches execution based on a boolean condition.
    /// If the condition is true, the OnTrue output is triggered; otherwise, OnFalse is triggered.
    /// </summary>
    public sealed class IfNode : IFlowNode
    {
        /// <inheritdoc />
        public string Folder => NodeFolders.ControlFlowFolder;

        /// <inheritdoc />
        public string Name => "If";

        /// <summary>
        /// Gets the input port that receives the condition to check.
        /// </summary>
        public FlowInput<bool> Condition { get; }

        /// <summary>
        /// Gets the output port that is triggered if the condition is true.
        /// </summary>
        public FlowOutput OnTrue { get; } = new();

        /// <summary>
        /// Gets the output port that is triggered if the condition is false.
        /// </summary>
        public FlowOutput OnFalse { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="IfNode"/> class.
        /// </summary>
        public IfNode()
        {
            Condition = new(CheckCondition);
        }

        private async UniTask CheckCondition(bool condition, CancellationToken cancellationToken)
        {
            if (condition)
            {
                await OnTrue.Invoke(cancellationToken);
            }
            else
            {
                await OnFalse.Invoke(cancellationToken);
            }
        }
    }
}