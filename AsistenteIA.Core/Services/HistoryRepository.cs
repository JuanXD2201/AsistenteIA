using AsistenteIA.Core.Models;
using SQLite;

namespace AsistenteIA.Core.Services;

public interface IHistoryRepository
{
    Task<int> AddAsync(HistoryEntry entry);
    Task<IReadOnlyList<HistoryEntry>> GetAllAsync(string? search = null);
    Task<HistoryEntry?> GetAsync(int id);
    Task<bool> UpdateAsync(HistoryEntry entry);
    Task<bool> DeleteAsync(int id);
}

public sealed class SqliteHistoryRepository : IHistoryRepository
{
    private readonly SQLiteAsyncConnection _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    static SqliteHistoryRepository()
    {
        // En Android el proveedor nativo de SQLite no se registra solo: sin esto falla con "provider not initialized".
        SQLitePCL.Batteries_V2.Init();
    }

    public SqliteHistoryRepository(string databasePath)
    {
        _db = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    private async Task InitAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            await _db.CreateTableAsync<HistoryEntry>();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<int> AddAsync(HistoryEntry entry)
    {
        await InitAsync();
        entry.Id = 0;
        await _db.InsertAsync(entry);
        return entry.Id;
    }

    public async Task<IReadOnlyList<HistoryEntry>> GetAllAsync(string? search = null)
    {
        await InitAsync();
        var query = _db.Table<HistoryEntry>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Prompt.Contains(term) || e.Answer.Contains(term) || e.Category.Contains(term));
        }
        return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    public async Task<HistoryEntry?> GetAsync(int id)
    {
        await InitAsync();
        return await _db.Table<HistoryEntry>().Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(HistoryEntry entry)
    {
        await InitAsync();
        return await _db.UpdateAsync(entry) > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await InitAsync();
        return await _db.DeleteAsync<HistoryEntry>(id) > 0;
    }
}
