using SQLite;

namespace LocationTracker.Models;

/// <summary>
/// A single recorded position of the user. Each instance maps to one row
/// in the LocationPoints SQLite table.
/// </summary>
[Table("LocationPoints")]
public class LocationPoint
{
    /// <summary>Auto-incrementing primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Latitude in decimal degrees.</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude in decimal degrees.</summary>
    public double Longitude { get; set; }

    /// <summary>Reported horizontal accuracy in metres (may be null on some devices).</summary>
    public double? Accuracy { get; set; }

    /// <summary>UTC time the location was captured. Indexed for time-ordered queries.</summary>
    [Indexed]
    public DateTime TimestampUtc { get; set; }
}