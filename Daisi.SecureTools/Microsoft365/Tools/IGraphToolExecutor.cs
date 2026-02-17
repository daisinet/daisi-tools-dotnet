using Microsoft.Graph;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// Interface for individual tool executors that use the Microsoft Graph API.
/// Each tool receives an authenticated <see cref="GraphServiceClient"/> and
/// a list of parameters from the caller.
/// </summary>
public interface IGraphToolExecutor
{
    /// <summary>
    /// Execute the tool using the provided Graph client and parameters.
    /// </summary>
    Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters);
}
