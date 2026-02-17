using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Firecrawl.Tools;

/// <summary>
/// Interface for individual tool executors within a secure tool provider.
/// </summary>
public interface IToolExecutor
{
    Task<ExecuteResponse> ExecuteAsync(string apiKey, string baseUrl, List<ParameterValue> parameters);
}
