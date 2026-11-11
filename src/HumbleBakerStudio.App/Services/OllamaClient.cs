using System.Net.Http.Json;
using System.Text.Json;

namespace HumbleBakerStudio.Services;

internal sealed class OllamaClient : IDisposable
{
    private readonly HttpClient _http;

    public OllamaClient(string baseUrl)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    public string BaseUrl { get; set; }

    public async Task<string[]> ListModelsAsync(CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.GetAsync(BaseUrl + "/api/tags", ct);
            resp.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                return Array.Empty<string>();
            return models.EnumerateArray()
                .Select(m => m.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "")
                .Where(s => s.Length > 0)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public Task<string> ChatAsync(string model, string system, string user, bool json, CancellationToken ct = default)
        => ChatAsync(model, system, user, json, null, ct);

    public async Task<string> ChatAsync(string model, string system, string user, bool json, Action<string>? onToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Choose an Ollama model first.");

        var body = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["stream"] = onToken != null,
            ["messages"] = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            },
            ["options"] = new { temperature = 0.7, top_p = 0.95 }
        };
        if (json) body["format"] = "json";

        using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/api/chat")
        {
            Content = JsonContent.Create(body)
        };
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var detail = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Ollama returned {(int)resp.StatusCode}: {detail}");
        }

        if (onToken == null)
        {
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return doc.RootElement.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var c)
                ? c.GetString() ?? ""
                : "";
        }

        // Streaming: Ollama sends newline-delimited JSON objects.
        var sb = new System.Text.StringBuilder();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var c))
                {
                    var piece = c.GetString() ?? "";
                    sb.Append(piece);
                    if (piece.Length > 0) onToken(piece);
                }
                if (doc.RootElement.TryGetProperty("done", out var done) && done.GetBoolean()) break;
            }
            catch (JsonException)
            {
                // Ignore malformed keep-alive lines.
            }
        }
        return sb.ToString();
    }

    public void Dispose() => _http.Dispose();
}
