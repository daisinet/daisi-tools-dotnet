using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common.Models;

namespace SecureToolProvider.Common;

/// <summary>
/// Handles OAuth 2.0 authorization code flow with PKCE.
/// Generic across providers (Google, Microsoft, etc.) — parameterized via OAuthConfig.
/// </summary>
public class OAuthHelper
{
    private readonly OAuthConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    // PKCE verifiers stored per state parameter (short-lived, in-memory)
    private static readonly Dictionary<string, string> _pkceVerifiers = new();
    private static readonly object _pkceLock = new();

    public OAuthHelper(OAuthConfig config, HttpClient httpClient, ILogger logger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Build the authorization URL that the user's browser should navigate to.
    /// Returns the URL and the state parameter for correlation.
    /// </summary>
    public (string AuthorizeUrl, string State) BuildAuthorizeUrl(string installId, string setupKey)
    {
        var state = Convert.ToBase64String(
            JsonSerializer.SerializeToUtf8Bytes(new { installId, setupKey }));

        // Generate PKCE code verifier and challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        lock (_pkceLock)
        {
            _pkceVerifiers[state] = codeVerifier;
        }

        var scopes = string.Join(" ", _config.Scopes);
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _config.ClientId,
            ["redirect_uri"] = _config.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = scopes,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        foreach (var (key, value) in _config.AdditionalAuthParams)
        {
            queryParams[key] = value;
        }

        var queryString = string.Join("&", queryParams.Select(
            kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var authorizeUrl = $"{_config.AuthorizeUrl}?{queryString}";
        return (authorizeUrl, state);
    }

    /// <summary>
    /// Parse the state parameter from the OAuth callback to extract installId and setupKey.
    /// </summary>
    public static (string InstallId, string SetupKey)? ParseState(string state)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(
                Convert.FromBase64String(state));
            var installId = json.GetProperty("installId").GetString();
            var setupKey = json.GetProperty("setupKey").GetString();
            if (installId is not null && setupKey is not null)
                return (installId, setupKey);
        }
        catch { /* invalid state */ }
        return null;
    }

    /// <summary>
    /// Exchange an authorization code for access and refresh tokens.
    /// </summary>
    public async Task<OAuthTokens?> ExchangeCodeForTokensAsync(string code, string state)
    {
        string? codeVerifier;
        lock (_pkceLock)
        {
            _pkceVerifiers.Remove(state, out codeVerifier);
        }

        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _config.RedirectUri,
            ["client_id"] = _config.ClientId,
            ["client_secret"] = _config.ClientSecret
        };

        if (codeVerifier is not null)
        {
            requestBody["code_verifier"] = codeVerifier;
        }

        return await RequestTokensAsync(requestBody);
    }

    /// <summary>
    /// Refresh an expired access token using the refresh token.
    /// </summary>
    public async Task<OAuthTokens?> RefreshTokensAsync(string refreshToken)
    {
        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = _config.ClientId,
            ["client_secret"] = _config.ClientSecret
        };

        return await RequestTokensAsync(requestBody);
    }

    private async Task<OAuthTokens?> RequestTokensAsync(Dictionary<string, string> formData)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _config.TokenUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token request failed: {StatusCode} {Body}", response.StatusCode, responseBody);
                return null;
            }

            var json = JsonSerializer.Deserialize<JsonElement>(responseBody);

            var tokens = new OAuthTokens
            {
                AccessToken = json.GetProperty("access_token").GetString() ?? string.Empty,
                TokenType = json.TryGetProperty("token_type", out var tt) ? tt.GetString() : "Bearer",
                Scope = json.TryGetProperty("scope", out var sc) ? sc.GetString() : null
            };

            if (json.TryGetProperty("refresh_token", out var rt))
                tokens.RefreshToken = rt.GetString();

            if (json.TryGetProperty("expires_in", out var ei))
                tokens.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ei.GetInt32());
            else
                tokens.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1); // default

            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request tokens");
            return null;
        }
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string verifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
