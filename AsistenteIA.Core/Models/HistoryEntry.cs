using SQLite;

namespace AsistenteIA.Core.Models;

public class HistoryEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Prompt { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Category { get; set; } = "General";

    public bool IsVerified { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
}
