using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFlow.Attributes;
using GameFlow.Ports;
using GameFlow.Utils;

namespace GameFlow.Nodes
{
    /// <summary>
    /// A node that routes execution to different outputs based on the value of an enum input.
    /// Each enum value corresponds to a separate output case.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to switch on.</typeparam>
    [GenerateGenericSubclasses(ParameterTypeBase = typeof(Enum))]
    public sealed class SwitchNode<TEnum> : IFlowNode where TEnum : Enum
    {
        private static readonly TEnum[] EnumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
        private static readonly string[] EnumNames = Enum.GetNames(typeof(TEnum));

        /// <inheritdoc />
        public string Folder => NodeFolders.ControlFlowFolder;

        /// <inheritdoc />
        public string Name => $"Switch<{typeof(TEnum).Name}>";

        /// <summary>
        /// Gets the input port that receives the enum value to switch on.
        /// </summary>
        public FlowInput<TEnum> Switch { get; }

        /// <summary>
        /// Gets the output ports, one for each enum value.
        /// </summary>
        [AddInvokeOverride(nameof(AddInvoke))]
        public FlowOutput[] Cases { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchNode{TEnum}"/> class.
        /// </summary>
        public SwitchNode()
        {
            Cases = Array.ConvertAll(EnumValues, x => new FlowOutput(x.ToString()));
            Switch = new(SwitchValue, typeof(TEnum).Name);
        }

        private async UniTask SwitchValue(TEnum value, CancellationToken cancellationToken)
        {
            int index = Array.IndexOf(EnumValues, value);
            await Cases[index].Invoke(cancellationToken);
        }

        public void AddInvoke(string portName, FlowOutput.InvokeTargetDelegate invoke)
        {
            int index = Array.IndexOf(EnumNames, portName);
            Cases[index].AddInvoke(invoke);
        }
    }
}