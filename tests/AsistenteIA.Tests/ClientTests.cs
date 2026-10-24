using System.Net;
using AsistenteIA.Core.Models;
using AsistenteIA.Core.Services;

namespace AsistenteIA.Tests;

public class ClientTests
{
    private static readonly AiProviderInfo Provider = ProviderCatalog.Pollinations;
    private const string OkBody = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\" Hola \"}}]}";

    private static ChatCompletionsClient Client(FakeHandler h, TimeSpan? timeout = null) => new(new HttpClient(h), timeout);

    [Fact]
    public async Task Chat_200_ReturnsTrimmedText_AndSendsBearerAndModel()
    {
        var handler = FakeHandler.Status(HttpStatusCode.OK, OkBody);
        var result = await Client(handler).SendChatAsync(Provider, "k-test", "openai", "hola");

        Assert.True(result.Success);
        Assert.Equal("Hola", result.Value);
        Assert.Equal(200, result.HttpStatus);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("k-test", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Equal("https://gen.pollinations.ai/v1/chat/completions", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("\"model\":\"openai\"", handler.LastBody);
        Assert.DoesNotContain("max_tokens", handler.LastBody);
    }

    [Fact]
    public async Task Chat_WithMaxTokens_IncludesField()
    {
        var handler = FakeHandler.Status(HttpStatusCode.OK, OkBody);
        await Client(handler).SendChatAsync(Provider, "k", "m", "p", 1);
        Assert.Contains("\"max_tokens\":1", handler.LastBody);
    }

    [Theory]
    [InlineData(400, AiErrorKind.BadRequest)]
    [InlineData(401, AiErrorKind.Unauthorized)]
    [InlineData(403, AiErrorKind.Unauthorized)]
    [InlineData(404, AiErrorKind.NotFound)]
    [InlineData(408, AiErrorKind.Timeout)]
    [InlineData(429, AiErrorKind.RateLimited)]
    [InlineData(500, AiErrorKind.ServerError)]
    [InlineData(503, AiErrorKind.ServerError)]
    public async Task Chat_ErrorStatus_IsMapped(int status, AiErrorKind expected)
    {
        var result = await Client(FakeHandler.Status((HttpStatusCode)status)).SendChatAsync(Provider, "k", "m", "p");

        Assert.False(result.Success);
        Assert.Equal(expected, result.Error);
        Assert.Equal(status, result.HttpStatus);
    }

    [Fact]
    public async Task Chat_ErrorMessage_NeverContainsKey()
    {
        var result = await Client(FakeHandler.Status(HttpStatusCode.Unauthorized)).SendChatAsync(Provider, "SECRET-KEY-123", "m", "p");
        Assert.DoesNotContain("SECRET-KEY-123", result.Message);
    }

    [Fact]
    public async Task Chat_NetworkFailure_MapsToNoConnection()
    {
        var handler = new FakeHandler((_, _) => throw new HttpRequestException("dns"));
        var result = await Client(handler).SendChatAsync(Provider, "k", "m", "p");
        Assert.Equal(AiErrorKind.NoConnection, result.Error);
    }

    [Fact]
    public async Task Chat_Timeout_MapsToTimeout()
    {
        var handler = new FakeHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var result = await Client(handler, TimeSpan.FromMilliseconds(100)).SendChatAsync(Provider, "k", "m", "p");
        Assert.Equal(AiErrorKind.Timeout, result.Error);
    }

    [Fact]
    public async Task Chat_CallerCancel_MapsToCancelled()
    {
        var handler = new FakeHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var result = await Client(handler).SendChatAsync(Provider, "k", "m", "p", null, cts.Token);
        Assert.Equal(AiErrorKind.Cancelled, result.Error);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"choices\":[]}")]
    [InlineData("{\"choices\":[{\"message\":{\"content\":\"  \"}}]}")]
    public async Task Chat_InvalidBody_MapsToInvalidResponse(string body)
    {
        var result = await Client(FakeHandler.Status(HttpStatusCode.OK, body)).SendChatAsync(Provider, "k", "m", "p");
        Assert.Equal(AiErrorKind.InvalidResponse, result.Error);
    }

    [Fact]
    public async Task Models_ExcludesNonChatModels()
    {
        const string body = "{\"data\":[{\"id\":\"models/gemini-3.1-flash-lite\"},{\"id\":\"models/gemini-3-pro-image\"},{\"id\":\"models/gemini-2.5-flash-preview-tts\"},{\"id\":\"models/lyria-3-clip-preview\"},{\"id\":\"models/gemini-3.5-transcribe\"}]}";
        var result = await Client(FakeHandler.Status(HttpStatusCode.OK, body)).ListModelsAsync(ProviderCatalog.Gemini, "k");
        Assert.Equal(new[] { "gemini-3.1-flash-lite" }, result.Value!.Select(m => m.Id));
    }

    [Fact]
    public async Task Chat_EmptyContentWithMaxTokens_IsSuccess_ForReasoningModels()
    {
        const string body = "{\"choices\":[{\"finish_reason\":\"length\",\"message\":{\"role\":\"assistant\"}}]}";
        var result = await Client(FakeHandler.Status(HttpStatusCode.OK, body)).SendChatAsync(Provider, "k", "m", "ping", 1);
        Assert.True(result.Success);
        Assert.Equal(200, result.HttpStatus);
    }

    [Fact]
    public async Task Chat_EmptyContentWithoutMaxTokens_IsInvalidResponse()
    {
        const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\"}}]}";
        var result = await Client(FakeHandler.Status(HttpStatusCode.OK, body)).SendChatAsync(Provider, "k", "m", "p");
        Assert.Equal(AiErrorKind.InvalidResponse, result.Error);
    }

    [Fact]
    public async Task Models_FiltersNonText_AndStripsGeminiPrefix()
    {
        const string body = "{\"data\":[{\"id\":\"a\",\"category\":\"text\",\"title\":\"A\",\"paid_only\":true},{\"id\":\"img\",\"category\":\"image\"},{\"id\":\"models/gem\"}]}";
        var handler = FakeHandler.Status(HttpStatusCode.OK, body);
        var result = await Client(handler).ListModelsAsync(Provider, null);

        Assert.True(result.Success);
        Assert.Equal(new[] { "a", "gem" }, result.Value!.Select(m => m.Id));
        Assert.True(result.Value![0].PaidOnly);
        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }
}
