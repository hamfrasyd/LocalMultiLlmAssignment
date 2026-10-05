using System.Net.Http.Json;
using System.Text.Json;

namespace LocalLlm.Orchestrator;

public sealed class OllamaClient : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public async Task<string> ChatAsync(
        ModelEndpointSettings endpoint,
        string systemPrompt,
        string userPrompt,
        JsonElement? responseFormat = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new object[]
        {
            new
            {
                role = "system",
                content = systemPrompt
            },
            new
            {
                role = "user",
                content = userPrompt
            }
        };

        var payload = new Dictionary<string, object?>
        {
            ["model"] = endpoint.Model,
            ["messages"] = messages,
            ["stream"] = false,

            // Lower temperature makes repeated runs more comparable.
            ["options"] = new
            {
                temperature = 0,
                num_ctx = 8192,
                num_predict = 1536
            }
        };

        if (responseFormat.HasValue)
        {
            payload["format"] = responseFormat.Value;
        }

        var url = $"{endpoint.BaseUrl.TrimEnd('/')}/api/chat";

        using var response = await _httpClient.PostAsJsonAsync(
            url,
            payload,
            cancellationToken);

        var rawResponse = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Ollama request failed.\n" +
                $"Endpoint: {endpoint.BaseUrl}\n" +
                $"Model: {endpoint.Model}\n" +
                $"Status: {(int)response.StatusCode}\n" +
                $"Response:\n{rawResponse}");
        }

        using var document = JsonDocument.Parse(rawResponse);

        var content = document.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content
            ?? throw new InvalidOperationException(
                "Ollama response contained no message content.");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}