namespace JG.WebKit.Router.Abstractions;

/// <summary>
/// Executes a single step in a route's execution chain.
/// Returns either Continue (to execute next node) or Stop (to short-circuit).
/// </summary>
public interface IChainNode
{
    /// <summary>
    /// Execute this chain node. Return ChainResult.Next() to continue, or ChainResult.Stop(response) to short-circuit.
    /// </summary>
    ValueTask<ChainResult> ExecuteAsync(RequestContext context, CancellationToken cancellationToken);
}
