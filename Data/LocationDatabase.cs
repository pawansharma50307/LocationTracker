using LocationTracker.Models;
using SQLite;

namespace LocationTracker.Data;

/// <summary>
/// Data-access class for the local SQLite database. The connection is created lazily
/// the first time it is needed so app start-up is not blocked by disk I/O.
/// </summary>
public class LocationDatabase
{
    private const string DatabaseFileName = "LocationTracker.db3";

    // Open read/write, create if missing, allow use from multiple threads.
    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    private static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    private SQLiteAsyncConnection? _connection;

    /// <summary>Opens the connection and creates the table on first use.</summary>
    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        _connection = new SQLiteAsyncConnection(DatabasePath, Flags);
        await _connection.CreateTableAsync<LocationPoint>();
        return _connection;
    }

    /// <summary>Inserts a new location row.</summary>
    public async Task<int> SaveLocationAsync(LocationPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        var db = await GetConnectionAsync();
        return await db.InsertAsync(point);
    }

    /// <summary>Inserts many locations in a single transaction (much faster than one by one).</summary>
    public async Task<int> SaveLocationsAsync(IEnumerable<LocationPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var db = await GetConnectionAsync();
        return await db.InsertAllAsync(points, runInTransaction: true);
    }

    /// <summary>Returns every stored location ordered from oldest to newest.</summary>
    public async Task<List<LocationPoint>> GetAllLocationsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<LocationPoint>()
                       .OrderBy(p => p.TimestampUtc)
                       .ToListAsync();
    }

    /// <summary>Deletes every stored location.</summary>
    public async Task ClearAllAsync()
    {
        var db = await GetConnectionAsync();
        await db.DeleteAllAsync<LocationPoint>();
    }
}