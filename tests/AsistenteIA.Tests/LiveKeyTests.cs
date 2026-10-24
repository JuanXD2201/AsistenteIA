using AsistenteIA.Core.Models;
using AsistenteIA.Core.Services;

namespace AsistenteIA.Tests;

/// <summary>
/// Auditoría end-to-end con una API key real. Solo corre si existe ASISTENTEIA_KEY (nunca se guarda en el repositorio).
/// Comando: ASISTENTEIA_KEY=... ASISTENTEIA_MODEL=gemini-3.1-flash-lite dotnet test --filter LiveKey
/// La clave solo se envía al proveedor que la detección local identifica (Gemini).
/// </summary>
public sealed class LiveKeyTests : IDisposable
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"asistenteia-live-{Guid.NewGuid():N}.db3");
    private static string? Key => Environment.GetEnvironmentVariable("ASISTENTEIA_KEY");
    private static string Model => Environment.GetEnvironmentVariable("ASISTENTEIA_MODEL") ?? "gemini-3.1-flash-lite";

    public void Dispose()
    {
        if (!File.Exists(_db)) return;
        SQLitePCL.raw.sqlite3_shutdown();
        try { File.Delete(_db); } catch (IOException) { }
    }

    private static (AiService ai, AiConfigService cfg, MemorySecrets secrets, MemorySettings settings) Build()
    {
        var secrets = new MemorySecrets();
        var settings = new MemorySettings();
        var cfg = new AiConfigService(secrets, settings);
        return (new AiService(new ChatCompletionsClient(new HttpClient()), cfg), cfg, secrets, settings);
    }

    [Fact]
    public async Task FullFlow_Detect_ListModels_Validate_Save_Ask_Persist()
    {
        if (string.IsNullOrEmpty(Key)) return;
        var (ai, cfg, secrets, settings) = Build();

        var provider = ProviderDetector.Detect(Key);
        Assert.Same(ProviderCatalog.Gemini, provider);

        var models = await ai.ListModelsAsync(provider!, Key);
        Assert.True(models.Success, models.Message);
        Assert.Contains(models.Value!, m => m.Id == Model);
        Assert.DoesNotContain(models.Value!, m => m.Id.Contains("image") || m.Id.Contains("tts"));

        var validation = await ai.ValidateAsync(provider!, Key, Model);
        Assert.True(validation.Success, validation.Message);
        Assert.Equal(200, validation.HttpStatus);

        await cfg.SaveAsync(Key, provider!, Model, true);
        Assert.True(await cfg.IsReadyAsync());
        Assert.DoesNotContain(Key, settings.Data.Values);
        Assert.Equal(Key, secrets.Data["ai_api_key"]);

        var answer = await ai.AskAsync("Responde en una sola frase corta en español: ¿qué es MVVM?");
        Assert.True(answer.Success, answer.Message);
        Assert.False(string.IsNullOrWhiteSpace(answer.Value));

        var repo = new SqliteHistoryRepository(_db);
        var id = await repo.AddAsync(new HistoryEntry { Prompt = "p", Answer = answer.Value!, Provider = provider!.Name, Model = Model });
        Assert.Equal(answer.Value, (await new SqliteHistoryRepository(_db).GetAsync(id))!.Answer);
    }

    [Fact]
    public async Task InvalidModel_ReturnsNotFoundOrBadRequest()
    {
        if (string.IsNullOrEmpty(Key)) return;
        var (ai, _, _, _) = Build();
        var result = await ai.ValidateAsync(ProviderCatalog.Gemini, Key, "modelo-que-no-existe");
        Assert.False(result.Success);
        Assert.True(result.Error is AiErrorKind.NotFound or AiErrorKind.BadRequest, result.Error.ToString());
        Assert.DoesNotContain(Key, result.Message);
    }

    [Fact]
    public async Task EmptyPrompt_NeverReachesNetwork_EvenWithKey()
    {
        if (string.IsNullOrEmpty(Key)) return;
        var (ai, cfg, _, _) = Build();
        await cfg.SaveAsync(Key, ProviderCatalog.Gemini, Model, true);
        Assert.Equal(AiErrorKind.EmptyPrompt, (await ai.AskAsync("   ")).Error);
    }
}
