using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace Kopitra.Api.Tests;

public static class HttpResponseDataExtension
{
    public static async Task<T?> ReadAsJsonAsync<T>(this HttpResponseData response)
    {
        // Copy stream content to memory
        using var memoryStream = new MemoryStream();
        await response.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
        memoryStream.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(memoryStream);
        var body = await reader.ReadToEndAsync().ConfigureAwait(false);

        System.Diagnostics.Debug.WriteLine($"[ReadAsJsonAsync] Body content: {body}");

        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var result = JsonSerializer.Deserialize<T>(body, options);
        System.Diagnostics.Debug.WriteLine($"[ReadAsJsonAsync] Deserialized result: {result}");
        return result;
    }
}
