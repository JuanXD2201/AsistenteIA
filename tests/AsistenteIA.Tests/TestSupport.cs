using System.Net;
using AsistenteIA.Core.Services;

namespace AsistenteIA.Tests;

public sealed class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastBody { get; private set; }

    public FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) => _responder = responder;

    public static FakeHandler Status(HttpStatusCode code, string body = "{}") =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body) }));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return await _responder(request, cancellationToken);
    }
}

public sealed class MemorySecrets : ISecretStore
{
    public Dictionary<string, string> Data { get; } = new();

    public Task<string?> GetAsync(string name) => Task.FromResult(Data.TryGetValue(name, out var v) ? v : null);

    public Task SetAsync(string name, string value)
    {
        Data[name] = value;
        return Task.CompletedTask;
    }

    public void Remove(string name) => Data.Remove(name);
}

public sealed class MemorySettings : ISettingsStore
{
    public Dictionary<string, string> Data { get; } = new();

    public string? Get(string key) => Data.TryGetValue(key, out var v) ? v : null;

    public void Set(string key, string value) => Data[key] = value;

    public void Remove(string key) => Data.Remove(key);
}
