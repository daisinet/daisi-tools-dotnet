using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Social;

/// <summary>
/// Interface for individual social media tool executors.
/// </summary>
public interface ISocialToolExecutor
{
    /// <summary>
    /// Execute the tool using the provided HTTP client and OAuth access token.
    /// </summary>
    Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters);
}
