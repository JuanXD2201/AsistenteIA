using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AsistenteIA.Core.Models;

namespace AsistenteIA.Core.Services;

/// <summary>
/// Cliente REST para APIs con el esquema Chat Completions.
/// La clave se envía solo en el encabezado Authorization de la solicitud al proveedor elegido.
/// </summary>
public sealed class ChatCompletionsClient : IAiClient
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    // Algunos catálogos (p. ej. Gemini) mezclan modelos de imagen, audio o embeddings que no sirven para chat.
    private static readonly System.Text.RegularExpressions.Regex NonChatModel = new(
        "image|tts|audio|transcribe|lyria|embed|nano-banana|veo|imagen|live",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly TimeSpan _timeout;

    public ChatCompletionsClient(HttpClient http, TimeSpan? timeout = null)
    {
        _http = http;
        _timeout = timeout ?? DefaultTimeout;
    }

    public async Task<AiResult<string>> SendChatAsync(AiProviderInfo provider, string apiKey, string model, string prompt, int? maxTokens = null, CancellationToken ct = default)
    {
        var body = new ChatRequest(model, new[] { new ChatMessage("user", prompt) }, maxTokens);
        using var request = new HttpRequestMessage(HttpMethod.Post, provider.BaseUrl + "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        return await ExecuteAsync<string>(request, ct, json =>
        {
            var parsed = JsonSerializer.Deserialize<ChatResponse>(json);
            var message = parsed?.Choices?.FirstOrDefault()?.Message;
            var text = message?.Content;
            if (!string.IsNullOrWhiteSpace(text)) return AiResult<string>.Ok(text.Trim());

            // Validación con max_tokens bajo: los modelos con razonamiento (p. ej. Gemini 3.x) pueden responder
            // 200 con contenido vacío porque agotan el límite pensando. La clave y el modelo son válidos.
            return maxTokens is not null && message is not null
                ? AiResult<string>.Ok(string.Empty)
                : AiResult<string>.Fail(AiErrorKind.InvalidResponse, "El proveedor devolvió una respuesta vacía.");
        });
    }

    public async Task<AiResult<IReadOnlyList<AiModel>>> ListModelsAsync(AiProviderInfo provider, string? apiKey, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, provider.BaseUrl + "models");
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        return await ExecuteAsync<IReadOnlyList<AiModel>>(request, ct, json =>
        {
            var parsed = JsonSerializer.Deserialize<ModelsResponse>(json);
            var models = (parsed?.Data ?? new List<ModelDto>())
                .Where(m => !string.IsNullOrWhiteSpace(m.Id) && (m.Category is null || m.Category == "text") && !NonChatModel.IsMatch(m.Id))
                .Select(m => new AiModel(m.Id!.StartsWith("models/", StringComparison.Ordinal) ? m.Id[7..] : m.Id, m.Title, m.PaidOnly ?? false))
                .ToList();
            return models.Count == 0
                ? AiResult<IReadOnlyList<AiModel>>.Fail(AiErrorKind.InvalidResponse, "El proveedor no devolvió modelos de texto.")
                : AiResult<IReadOnlyList<AiModel>>.Ok(models);
        });
    }

    private async Task<AiResult<T>> ExecuteAsync<T>(HttpRequestMessage request, CancellationToken ct, Func<string, AiResult<T>> parse)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);
        try
        {
            using var response = await _http.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
            var status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode) return AiErrorMapper.FromStatus<T>(status);

            var json = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            try
            {
                var result = parse(json);
                return result with { HttpStatus = status };
            }
            catch (JsonException)
            {
                return AiResult<T>.Fail(AiErrorKind.InvalidResponse, "No se pudo interpretar la respuesta del proveedor.", status);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return AiErrorMapper.FromException<T>(ex, ct.IsCancellationRequested);
        }
    }

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] ChatMessage[] Messages,
        [property: JsonPropertyName("max_tokens"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? MaxTokens);

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")] public MessageDto? Message { get; set; }
    }

    private sealed class MessageDto
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private sealed class ModelsResponse
    {
        [JsonPropertyName("data")] public List<ModelDto>? Data { get; set; }
    }

    private sealed class ModelDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("paid_only")] public bool? PaidOnly { get; set; }
    }
}
