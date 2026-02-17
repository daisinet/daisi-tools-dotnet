using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Social.Reddit.Tools;

/// <summary>
/// Submit a post to a subreddit. Supports text (self), link, and image posts.
/// Requires custom User-Agent header per Reddit API guidelines.
/// </summary>
public class RedditSubmitTool : ISocialToolExecutor
{
    private const string OAuthApiBase = "https://oauth.reddit.com";

    private static readonly Dictionary<string, string> RedditHeaders = new()
    {
        ["User-Agent"] = "daisi-securetools/1.0"
    };

    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var subreddit = parameters.FirstOrDefault(p => p.Name == "subreddit")?.Value;
        if (string.IsNullOrEmpty(subreddit))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'subreddit' parameter is required." };

        var title = parameters.FirstOrDefault(p => p.Name == "title")?.Value;
        if (string.IsNullOrEmpty(title))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'title' parameter is required." };

        var text = parameters.FirstOrDefault(p => p.Name == "text")?.Value;
        var url = parameters.FirstOrDefault(p => p.Name == "url")?.Value;
        var flairId = parameters.FirstOrDefault(p => p.Name == "flairId")?.Value;

        // Determine post kind
        string kind;
        var submitBody = new Dictionary<string, object>
        {
            ["sr"] = subreddit,
            ["title"] = title,
            ["api_type"] = "json"
        };

        if (!string.IsNullOrEmpty(url))
        {
            kind = "link";
            submitBody["kind"] = kind;
            submitBody["url"] = url;
        }
        else
        {
            kind = "self";
            submitBody["kind"] = kind;
            if (!string.IsNullOrEmpty(text))
                submitBody["text"] = text;
        }

        if (!string.IsNullOrEmpty(flairId))
            submitBody["flair_id"] = flairId;

        var result = await httpClient.PostJsonAsync(
            $"{OAuthApiBase}/api/submit", accessToken, submitBody, RedditHeaders);

        // Reddit returns {json: {data: {name, id, url}}}
        string? postId = null;
        string? postUrl = null;
        if (result.TryGetProperty("json", out var json)
            && json.TryGetProperty("data", out var data))
        {
            postId = data.TryGetProperty("name", out var name) ? name.GetString() : null;
            postUrl = data.TryGetProperty("url", out var u) ? u.GetString() : null;
        }

        var output = JsonSerializer.Serialize(new
        {
            id = postId,
            url = postUrl,
            subreddit,
            title,
            kind
        }, SocialHttpClient.JsonOptions);

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Post submitted to r/{subreddit}."
        };
    }
}
