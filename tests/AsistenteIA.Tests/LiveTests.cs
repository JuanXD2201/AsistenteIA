using AsistenteIA.Core.Models;
using AsistenteIA.Core.Services;

namespace AsistenteIA.Tests;

/// <summary>Pruebas contra el servicio real. Solo corren con ASISTENTEIA_LIVE=1 y nunca usan claves reales.</summary>
public class LiveTests
{
    private static bool Enabled => Environment.GetEnvironmentVariable("ASISTENTEIA_LIVE") == "1";

    [Fact]
    public async Task Live_Pollinations_ModelsCatalog_IsPublic()
    {
        if (!Enabled) return;
        var client = new ChatCompletionsClient(new HttpClient());
        var result = await client.ListModelsAsync(ProviderCatalog.Pollinations, null);
        Assert.True(result.Success, result.Message);
        Assert.Equal(200, result.HttpStatus);
        Assert.NotEmpty(result.Value!);
    }

    [Fact]
    public async Task Live_Pollinations_InvalidKey_Returns401()
    {
        if (!Enabled) return;
        var client = new ChatCompletionsClient(new HttpClient());
        var result = await client.SendChatAsync(ProviderCatalog.Pollinations, "clave-falsa-de-prueba", "openai", "ping", 1);
        Assert.False(result.Success);
        Assert.Equal(AiErrorKind.Unauthorized, result.Error);
        Assert.Equal(401, result.HttpStatus);
    }
}
