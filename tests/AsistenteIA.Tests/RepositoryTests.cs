using AsistenteIA.Core.Models;
using AsistenteIA.Core.Services;

namespace AsistenteIA.Tests;

public sealed class RepositoryTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"asistenteia-test-{Guid.NewGuid():N}.db3");

    public void Dispose()
    {
        SQLitePCL.raw.sqlite3_shutdown();
        foreach (var f in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            try { File.Delete(f); } catch (IOException) { }
        }
    }

    private static HistoryEntry NewEntry(string prompt = "p", string category = "General") =>
        new() { Prompt = prompt, Answer = "a", Category = category, CreatedAt = DateTime.UtcNow };

    [Fact]
    public async Task Crud_CreateReadUpdateDelete()
    {
        var repo = new SqliteHistoryRepository(_path);

        var id = await repo.AddAsync(NewEntry("consulta uno"));
        Assert.True(id > 0);

        var loaded = await repo.GetAsync(id);
        Assert.Equal("consulta uno", loaded!.Prompt);
        Assert.False(loaded.IsVerified);

        loaded.Category = "Estudio";
        loaded.IsVerified = true;
        Assert.True(await repo.UpdateAsync(loaded));
        var updated = await repo.GetAsync(id);
        Assert.Equal("Estudio", updated!.Category);
        Assert.True(updated.IsVerified);

        Assert.True(await repo.DeleteAsync(id));
        Assert.Null(await repo.GetAsync(id));
        Assert.False(await repo.DeleteAsync(id));
    }

    [Fact]
    public async Task GetAll_OrdersByNewestFirst_AndFilters()
    {
        var repo = new SqliteHistoryRepository(_path);
        await repo.AddAsync(new HistoryEntry { Prompt = "antigua sobre C#", Answer = "x", CreatedAt = DateTime.UtcNow.AddDays(-1) });
        await repo.AddAsync(new HistoryEntry { Prompt = "reciente sobre SQL", Answer = "y", CreatedAt = DateTime.UtcNow });

        var all = await repo.GetAllAsync();
        Assert.Equal("reciente sobre SQL", all[0].Prompt);

        var filtered = await repo.GetAllAsync("SQL");
        Assert.Single(filtered);
        Assert.Empty(await repo.GetAllAsync("inexistente"));
    }

    [Fact]
    public async Task Data_PersistsAcrossRepositoryInstances()
    {
        var first = new SqliteHistoryRepository(_path);
        var id = await first.AddAsync(NewEntry("persistente"));

        var second = new SqliteHistoryRepository(_path);
        Assert.Equal("persistente", (await second.GetAsync(id))!.Prompt);
    }

    [Fact]
    public async Task Update_NonExisting_ReturnsFalse()
    {
        var repo = new SqliteHistoryRepository(_path);
        var entry = NewEntry();
        entry.Id = 999;
        Assert.False(await repo.UpdateAsync(entry));
    }
}
