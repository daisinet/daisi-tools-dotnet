using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

/// <summary>
/// Interface for individual DaisiGit tool executors.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Executes the tool with the given DaisiGit server URL, session ID, and parameters.
    /// </summary>
    Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters);
}
