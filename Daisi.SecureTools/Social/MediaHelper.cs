namespace Daisi.SecureTools.Social;

/// <summary>
/// Utility for resolving media from URLs or base64-encoded data.
/// </summary>
public static class MediaHelper
{
    /// <summary>
    /// Resolve media bytes and content type from either a URL or base64 string.
    /// Returns null if neither is provided.
    /// </summary>
    public static async Task<(byte[] Data, string ContentType)?> ResolveMediaAsync(
        IHttpClientFactory httpClientFactory, string? mediaUrl, string? mediaBase64)
    {
        if (!string.IsNullOrEmpty(mediaBase64))
        {
            var data = Convert.FromBase64String(mediaBase64);
            var contentType = DetectContentType(data);
            return (data, contentType);
        }

        if (!string.IsNullOrEmpty(mediaUrl))
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(mediaUrl);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType
                ?? DetectContentType(data);
            return (data, contentType);
        }

        return null;
    }

    /// <summary>
    /// Detect content type from file magic bytes.
    /// </summary>
    internal static string DetectContentType(byte[] data)
    {
        if (data.Length < 4)
            return "application/octet-stream";

        // JPEG: FF D8 FF
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "image/jpeg";

        // PNG: 89 50 4E 47
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return "image/png";

        // GIF: 47 49 46 38
        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38)
            return "image/gif";

        // WebP: 52 49 46 46 ... 57 45 42 50
        if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
            && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return "image/webp";

        // MP4: ... 66 74 79 70 (ftyp at offset 4)
        if (data.Length >= 8 && data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70)
            return "video/mp4";

        return "application/octet-stream";
    }
}
