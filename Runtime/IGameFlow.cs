using System.Threading;
using Cysharp.Threading.Tasks;

namespace GameFlow
{
    /// <summary>
    /// Game flow interface.
    /// </summary>
    public interface IGameFlow
    {
        /// <summary>
        /// Runs graph.
        /// </summary>
        UniTask RunGraph(IInjectData injectData, CancellationToken cancellationToken);
    }
}