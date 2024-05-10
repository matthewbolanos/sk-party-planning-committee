using Shared.Models;

namespace LightingAgent.Services
{
    /// <summary>
    /// Represents a service that executes a run asynchronously.
    /// </summary>
    public interface IRunService
    {
        /// <summary>
        /// Executes the specified run asynchronously.
        /// </summary>
        /// <param name="run">The run to execute.</param>
        /// <returns>An asynchronous enumerable of strings representing the execution result.</returns>
        IAsyncEnumerable<string> ExecuteRunAsync(Run run);
    }
}
