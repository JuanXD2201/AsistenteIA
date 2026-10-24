using System.Net;
using AsistenteIA.Core.Models;
using AsistenteIA.Core.Services;

namespace AsistenteIA.Tests;

public class LogicTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Prompt_Empty_IsInvalid(string? prompt)
    {
        var (valid, error, _) = PromptValidator.Validate(prompt);
        Assert.False(valid);
        Assert.Equal(AiErrorKind.EmptyPrompt, error);
    }

    [Fact]
    public void Prompt_TooLong_IsInvalid()
    {
        var (valid, error, _) = PromptValidator.Validate(new string('a', PromptValidator.MaxLength + 1));
        Assert.False(valid);
        Assert.Equal(AiErrorKind.PromptTooLong, error);
    }

    [Fact]
    public void Prompt_Valid_IsAccepted() => Assert.True(PromptValidator.Validate("¿Qué es MVVM?").IsValid);

    [Fact]
    public void Detector_Gemini_ByPrefixAndLength()
        => Assert.Same(ProviderCatalog.Gemini, ProviderDetector.Detect("AIza" + new string('x', 35)));

    [Fact]
    public void Detector_Gemini_ByAqPrefix()
        => Assert.Same(ProviderCatalog.Gemini, ProviderDetector.Detect("AQ." + new string('x', 40)));

    [Fact]
    public void Catalog_OnlyGeminiAndPollinations()
        => Assert.Equal(new[] { "gemini", "pollinations" }, ProviderCatalog.All.Select(p => p.Id));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc123")]
    [InlineData("sk-proj-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
    [InlineData("sk-ant-api03-xxxxxxxxxxxxxxxxxxxxxxxx")]
    [InlineData("AIzaShort")]
    public void Detector_Ambiguous_ReturnsNull(string? key) => Assert.Null(ProviderDetector.Detect(key));

    [Fact]
    public void Catalog_FindById_IsCaseInsensitive()
    {
        Assert.Same(ProviderCatalog.Pollinations, ProviderCatalog.FindById("POLLINATIONS"));
        Assert.Null(ProviderCatalog.FindById("nope"));
    }

    [Fact]
    public async Task Config_SaveAndClear()
    {
        var secrets = new MemorySecrets();
        var settings = new MemorySettings();
        var config = new AiConfigService(secrets, settings);

        Assert.False(await config.IsReadyAsync());
        await config.SaveAsync("  key  ", ProviderCatalog.Pollinations, null, true);

        Assert.Equal("key", await config.GetKeyAsync());
        Assert.Equal("openai", config.Model);
        Assert.True(await config.IsReadyAsync());
        Assert.DoesNotContain("key", settings.Data.Values);

        config.Clear();
        Assert.False(await config.HasKeyAsync());
        Assert.Null(config.Provider);
        Assert.False(config.IsValidated);
    }

    private sealed class StubClient : IAiClient
    {
        public int Calls;

        public Task<AiResult<string>> SendChatAsync(AiProviderInfo provider, string apiKey, string model, string prompt, int? maxTokens = null, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(AiResult<string>.Fail(AiErrorKind.Unauthorized, "x", 401));
        }

        public Task<AiResult<IReadOnlyList<AiModel>>> ListModelsAsync(AiProviderInfo provider, string? apiKey, CancellationToken ct = default)
            => Task.FromResult(AiResult<IReadOnlyList<AiModel>>.Ok(new List<AiModel>()));
    }

    [Fact]
    public async Task Ask_EmptyPrompt_DoesNotCallClient()
    {
        var client = new StubClient();
        var service = new AiService(client, new AiConfigService(new MemorySecrets(), new MemorySettings()));
        var result = await service.AskAsync("  ");
        Assert.Equal(AiErrorKind.EmptyPrompt, result.Error);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task Ask_NoProvider_ReturnsMissingProvider()
    {
        var service = new AiService(new StubClient(), new AiConfigService(new MemorySecrets(), new MemorySettings()));
        Assert.Equal(AiErrorKind.MissingProvider, (await service.AskAsync("hola")).Error);
    }

    [Fact]
    public async Task Ask_NoKey_ReturnsMissingKey_WithoutCallingClient()
    {
        var settings = new MemorySettings();
        settings.Set("ai_provider", "pollinations");
        var client = new StubClient();
        var service = new AiService(client, new AiConfigService(new MemorySecrets(), settings));
        Assert.Equal(AiErrorKind.MissingKey, (await service.AskAsync("hola")).Error);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task Ask_Unauthorized_InvalidatesConfiguration()
    {
        var config = new AiConfigService(new MemorySecrets(), new MemorySettings());
        await config.SaveAsync("k", ProviderCatalog.Pollinations, null, true);
        var service = new AiService(new StubClient(), config);

        var result = await service.AskAsync("hola");

        Assert.Equal(AiErrorKind.Unauthorized, result.Error);
        Assert.False(config.IsValidated);
    }

    [Fact]
    public async Task Validate_EmptyKey_ReturnsMissingKey()
    {
        var service = new AiService(new StubClient(), new AiConfigService(new MemorySecrets(), new MemorySettings()));
        Assert.Equal(AiErrorKind.MissingKey, (await service.ValidateAsync(ProviderCatalog.Pollinations, " ", null)).Error);
    }

    [Fact]
    public async Task Ask_Success_SendsConfiguredModel()
    {
        var handler = FakeHandler.Status(HttpStatusCode.OK, "{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}");
        var config = new AiConfigService(new MemorySecrets(), new MemorySettings());
        await config.SaveAsync("k", ProviderCatalog.Pollinations, "mi-modelo", true);
        var service = new AiService(new ChatCompletionsClient(new HttpClient(handler)), config);

        var result = await service.AskAsync(" hola ");

        Assert.True(result.Success);
        Assert.Contains("\"model\":\"mi-modelo\"", handler.LastBody);
        Assert.Contains("\"content\":\"hola\"", handler.LastBody);
    }
}
