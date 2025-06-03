using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFlow.Utils;

namespace GameFlow.Ports
{
    /// <summary>
    /// Represents an output port that takes no parameters and returns no result.
    /// This is a convenience wrapper around FlowOutput&lt;Unit, Unit&gt;.
    /// </summary>
    public class FlowOutput : FlowOutput<Unit, Unit>
    {
        /// <summary>
        /// Initializes a new instance of the FlowOutput class.
        /// </summary>
        /// <param name="name">Optional name for this output port. If null, a default name will be used.</param>
        public FlowOutput(string name = null) : base(name)
        {
        }

        /// <summary>
        /// Invokes all connected input ports with no parameters.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async UniTask Invoke(CancellationToken cancellationToken)
        {
            if (InvokeList.Count == 0)
            {
                return;
            }

            await UniTask.WhenAll(InvokeList.Select(x => x.Invoke(default, cancellationToken)));
        }
    }

    /// <summary>
    /// Represents an output port that takes a parameter but returns no result.
    /// This is a convenience wrapper around FlowOutput&lt;TParam, Unit&gt;.
    /// </summary>
    /// <typeparam name="TParam">The type of parameter this output provides.</typeparam>
    public class FlowOutput<TParam> : FlowOutput<TParam, Unit>
    {
        /// <summary>
        /// Initializes a new instance of the FlowOutput class.
        /// </summary>
        /// <param name="name">Optional name for this output port. If null, a default name will be used.</param>
        public FlowOutput(string name = null) : base(name)
        {
        }

        /// <summary>
        /// Invokes all connected input ports with the specified parameter.
        /// </summary>
        /// <param name="param">The parameter to pass to connected inputs.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async UniTask Invoke(TParam param, CancellationToken cancellationToken)
        {
            if (InvokeList.Count == 0)
            {
                return;
            }

            await UniTask.WhenAll(InvokeList.Select(x => x.Invoke(param, cancellationToken)));
        }
    }

    /// <summary>
    /// Represents an output port that takes a parameter and returns a result.
    /// This is the base class for all typed output ports in the game flow system.
    /// </summary>
    /// <typeparam name="TParam">The type of parameter this output provides.</typeparam>
    /// <typeparam name="TResult">The type of result this output expects from connected inputs.</typeparam>
    public class FlowOutput<TParam, TResult> : FlowPort
    {
        public delegate UniTask<TResult> InvokeTargetDelegate(TParam param, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the list of functions to invoke when this output is triggered.
        /// Each function represents a connection to an input port.
        /// </summary>
        protected List<InvokeTargetDelegate> InvokeList { get; } = new();

        /// <inheritdoc />
        internal override bool IsInput => false;

        /// <inheritdoc />
        internal override Type ParameterType => typeof(TParam);

        /// <inheritdoc />
        internal override Type ResultType => typeof(TResult);

        /// <summary>
        /// Initializes a new instance of the FlowOutput class.
        /// </summary>
        /// <param name="name">Optional name for this output port. If null, a default name will be used.</param>
        public FlowOutput(string name = null) : base(name)
        {
        }

        /// <summary>
        /// Adds a function to the list of functions to invoke when this output is triggered.
        /// This is typically called when connecting this output to an input port.
        /// </summary>
        /// <param name="invoke">The function to add to the invoke list.</param>
        public void AddInvoke(InvokeTargetDelegate invoke)
        {
            InvokeList.Add(invoke);
        }

        /// <summary>
        /// Invokes all connected input ports and waits for completion sequentially.
        /// </summary>
        /// <param name="param">The parameter to pass to connected inputs.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>An async enumerable that represents sequential inputs.</returns>
        public IUniTaskAsyncEnumerable<WhenEachResult<TResult>> InvokeEach(TParam param, CancellationToken cancellationToken)
        {
            return UniTask.WhenEach(InvokeList.Select(x => x.Invoke(param, cancellationToken)));
        }

        /// <summary>
        /// Invokes all connected input ports and returns the first result that completes.
        /// Other operations are cancelled once the first result is received.
        /// </summary>
        /// <param name="param">The parameter to pass to connected inputs.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents the first result from any connected input.</returns>
        public async UniTask<TResult> InvokeAny(TParam param, CancellationToken cancellationToken)
        {
            if (InvokeList.Count == 0)
            {
                return default;
            }

            using var commonCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken.None);
            var result = await UniTask.WhenAny(InvokeList.Select(x => x.Invoke(param, commonCts.Token)));
            commonCts.Cancel();

            return result.result;
        }

        /// <summary>
        /// Invokes all connected input ports and returns all results.
        /// </summary>
        /// <param name="param">The parameter to pass to connected inputs.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task that represents an array of all results from connected inputs.</returns>
        public async UniTask<TResult[]> InvokeAll(TParam param, CancellationToken cancellationToken)
        {
            if (InvokeList.Count == 0)
            {
                return Array.Empty<TResult>();
            }

            var result = await UniTask.WhenAll(InvokeList.Select(x => x.Invoke(param, cancellationToken)));
            return result;
        }
    }
}