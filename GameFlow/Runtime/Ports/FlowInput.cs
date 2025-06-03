using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFlow.Utils;

namespace GameFlow.Ports
{
    /// <summary>
    /// Represents an input port that takes no parameters and returns no result.
    /// This is a convenience wrapper around FlowInput&lt;Unit, Unit&gt;.
    /// </summary>
    public class FlowInput : FlowInput<Unit, Unit>
    {
        private readonly Func<CancellationToken, UniTask> method;

        /// <summary>
        /// Initializes a new instance of the FlowInput class.
        /// </summary>
        /// <param name="method">The async method to execute when this input is triggered.</param>
        /// <param name="name">Optional name for this input port. If null, a default name will be used.</param>
        public FlowInput(Func<CancellationToken, UniTask> method, string name = null) : base(null, name)
        {
            this.method = method;
            Method = Wrapper;
        }

        private async UniTask<Unit> Wrapper(Unit param, CancellationToken cancellationToken)
        {
            await method.Invoke(cancellationToken);
            return default;
        }
    }

    /// <summary>
    /// Represents an input port that takes a parameter but returns no result.
    /// This is a convenience wrapper around FlowInput&lt;TParam, Unit&gt;.
    /// </summary>
    /// <typeparam name="TParam">The type of parameter this input accepts.</typeparam>
    public class FlowInput<TParam> : FlowInput<TParam, Unit>
    {
        private readonly Func<TParam, CancellationToken, UniTask> method;

        /// <summary>
        /// Initializes a new instance of the FlowInput class.
        /// </summary>
        /// <param name="method">The async method to execute when this input is triggered.</param>
        /// <param name="name">Optional name for this input port. If null, a default name will be used.</param>
        public FlowInput(Func<TParam, CancellationToken, UniTask> method, string name = null) : base(null, name)
        {
            this.method = method;
            Method = Wrapper;
        }

        private async UniTask<Unit> Wrapper(TParam param, CancellationToken cancellationToken)
        {
            await method.Invoke(param, cancellationToken);
            return default;
        }
    }

    /// <summary>
    /// Represents an input port that takes a parameter and returns a result.
    /// This is the base class for all typed input ports in the game flow system.
    /// </summary>
    /// <typeparam name="TParam">The type of parameter this input accepts.</typeparam>
    /// <typeparam name="TResult">The type of result this input returns.</typeparam>
    public class FlowInput<TParam, TResult> : FlowPort
    {
        /// <summary>
        /// Gets or sets the async method to execute when this input is triggered.
        /// </summary>
        public FlowOutput<TParam, TResult>.InvokeTargetDelegate Method { get; protected set; }

        /// <inheritdoc />
        internal override bool IsInput => true;

        /// <inheritdoc />
        internal override Type ParameterType => typeof(TParam);

        /// <inheritdoc />
        internal override Type ResultType => typeof(TResult);

        /// <summary>
        /// Initializes a new instance of the FlowInput class.
        /// </summary>
        /// <param name="method">The async method to execute when this input is triggered.</param>
        /// <param name="name">Optional name for this input port. If null, a default name will be used.</param>
        public FlowInput(FlowOutput<TParam, TResult>.InvokeTargetDelegate method, string name = null) : base(name)
        {
            Method = method;
        }
    }
}