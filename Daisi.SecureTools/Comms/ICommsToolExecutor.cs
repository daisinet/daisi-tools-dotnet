using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;

namespace Daisi.SecureTools.Comms;

/// <summary>
/// Interface for individual communications tool executors.
/// </summary>
public interface ICommsToolExecutor
{
    /// <summary>
    /// Execute the tool using the provided HTTP client and access token/credentials.
    /// </summary>
    Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters);
}
