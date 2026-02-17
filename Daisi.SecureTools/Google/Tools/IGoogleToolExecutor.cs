using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// Interface for individual Google tool executors within the secure tool provider.
/// </summary>
public interface IGoogleToolExecutor
{
    /// <summary>
    /// Execute the tool using the provided Google service factory and access token.
    /// </summary>
    Task<ExecuteResponse> ExecuteAsync(GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters);
}
