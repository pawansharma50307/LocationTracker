using LocationTracker.Models;

namespace LocationTracker.Data;

/// <summary>
/// Generates sample location history for demos and screenshots, so the heat map
/// can be shown without physically travelling the route. Points are interpolated
/// along a list of waypoints, with extra points at stops to create "hot" areas.
/// </summary>
public static class SampleDataSeeder
{
    private const double MetersPerDegreeLatitude = 111_320d;

    /// <summary>Homestead Road from Los Altos to Cupertino, California.</summary>
    public static readonly Location[] MountainViewRoute =
    {
        new(37.3410, -122.0905),
        new(37.3385, -122.0800),
        new(37.3378, -122.0680),
        new(37.3375, -122.0560),
        new(37.3373, -122.0440),
        new(37.3370, -122.0330)
    };

    /// <summary>Roscoff through Saint-Pol-de-Léon to Taulé, Brittany, France.</summary>
    public static readonly Location[] RoscoffRoute =
    {
        new(48.7240, -3.9700),
        new(48.7150, -3.9720),
        new(48.7000, -3.9800),
        new(48.6850, -3.9860),
        new(48.6650, -3.9650),
        new(48.6450, -3.9450),
        new(48.6200, -3.9350),
        new(48.6000, -3.9200),
        new(48.5950, -3.9000)
    };

    /// <summary>Builds a list of points along <paramref name="route"/>.</summary>
    /// <param name="route">Waypoints in travel order.</param>
    /// <param name="spacingMeters">Distance between generated points while moving.</param>
    /// <param name="stopPoints">How many points to add at the start and end stops.</param>
    public static List<LocationPoint> Generate(IReadOnlyList<Location> route, double spacingMeters, int stopPoints)
    {
        ArgumentNullException.ThrowIfNull(route);
        if (route.Count < 2)
        {
            throw new ArgumentException("A route needs at least two waypoints.", nameof(route));
        }

        // Fixed seed so the sample looks the same every time it is loaded.
        var random = new Random(42);
        var points = new List<LocationPoint>();
        DateTime time = DateTime.UtcNow.AddHours(-2);

        // Adds one point with a small random offset to imitate real GPS readings.
        void Add(Location location, double jitterMeters)
        {
            double latRadians = location.Latitude * Math.PI / 180d;
            double dLat = (random.NextDouble() - 0.5) * 2 * jitterMeters / MetersPerDegreeLatitude;
            double dLon = (random.NextDouble() - 0.5) * 2 * jitterMeters / (MetersPerDegreeLatitude * Math.Cos(latRadians));

            points.Add(new LocationPoint
            {
                Latitude = location.Latitude + dLat,
                Longitude = location.Longitude + dLon,
                Accuracy = 5,
                TimestampUtc = time
            });

            time = time.AddSeconds(5);
        }

        // Many readings in one spot = a hot (red) area on the heat map.
        void Linger(Location location, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Add(location, spacingMeters * 0.3);
            }
        }

        Linger(route[0], stopPoints);

        for (int i = 0; i < route.Count - 1; i++)
        {
            Location from = route[i];
            Location to = route[i + 1];

            double meters = Location.CalculateDistance(from, to, DistanceUnits.Kilometers) * 1000d;
            int steps = Math.Max(1, (int)(meters / spacingMeters));

            // Evenly spaced points between the two waypoints.
            for (int s = 0; s < steps; s++)
            {
                double t = (double)s / steps;
                var position = new Location(
                    from.Latitude + ((to.Latitude - from.Latitude) * t),
                    from.Longitude + ((to.Longitude - from.Longitude) * t));

                Add(position, spacingMeters * 0.1);
            }

            // Short pause at each waypoint (e.g. a traffic light) for some warmer cells.
            Linger(to, stopPoints / 4);
        }

        Linger(route[^1], stopPoints);

        return points;
    }
}