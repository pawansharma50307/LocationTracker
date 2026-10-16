namespace LocationTracker.Services;

/// <summary>
/// Abstraction over the device's location provider so the UI does not depend
/// directly on platform APIs (and so the service can be mocked in tests).
/// </summary>
public interface ILocationTrackingService
{
    /// <summary>Raised every time a new position is received from the device.</summary>
    event EventHandler<Location>? LocationUpdated;

    /// <summary>True while the service is actively listening for updates.</summary>
    bool IsTracking { get; }

    /// <summary>Asks the user for location permission if not granted yet.</summary>
    Task<bool> RequestPermissionAsync();

    /// <summary>Starts listening for location changes.</summary>
    Task<bool> StartAsync();

    /// <summary>Stops listening for location changes.</summary>
    void Stop();

    /// <summary>Gets a single one-off reading of the current position.</summary>
    Task<Location?> GetCurrentLocationAsync();
}