using Microsoft.Extensions.Logging;

namespace LocationTracker.Services;

/// <summary>
/// Default implementation of ILocationTrackingService built on the
/// cross-platform Geolocation API in .NET MAUI.
/// </summary>
public class LocationTrackingService : ILocationTrackingService
{
    // How often the OS should deliver updates while tracking.
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    private readonly ILogger<LocationTrackingService> _logger;

    public LocationTrackingService(ILogger<LocationTrackingService> logger)
    {
        _logger = logger;
    }

    public event EventHandler<Location>? LocationUpdated;

    public bool IsTracking => Geolocation.Default.IsListeningForeground;

    public async Task<bool> RequestPermissionAsync()
    {
        // The permission dialog must be shown from the UI thread.
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            return status == PermissionStatus.Granted;
        });
    }

    public async Task<bool> StartAsync()
    {
        if (IsTracking)
        {
            return true;
        }

        if (!await RequestPermissionAsync())
        {
            _logger.LogWarning("Location permission was denied.");
            return false;
        }

        try
        {
            Geolocation.Default.LocationChanged += OnLocationChanged;

            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, UpdateInterval);
            bool started = await Geolocation.Default.StartListeningForegroundAsync(request);

            if (!started)
            {
                Geolocation.Default.LocationChanged -= OnLocationChanged;
            }

            return started;
        }
        catch (FeatureNotSupportedException ex)
        {
            _logger.LogError(ex, "Location is not supported on this device.");
        }
        catch (FeatureNotEnabledException ex)
        {
            _logger.LogError(ex, "Location services are turned off.");
        }
        catch (PermissionException ex)
        {
            _logger.LogError(ex, "Location permission problem.");
        }

        Geolocation.Default.LocationChanged -= OnLocationChanged;
        return false;
    }

    public void Stop()
    {
        Geolocation.Default.LocationChanged -= OnLocationChanged;

        if (IsTracking)
        {
            Geolocation.Default.StopListeningForeground();
        }
    }

    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            if (!await RequestPermissionAsync())
            {
                return null;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            return await Geolocation.Default.GetLocationAsync(request)
                   ?? await Geolocation.Default.GetLastKnownLocationAsync();
        }
        catch (Exception ex)
        {
            // A one-off read failing is not fatal; the map just keeps its default view.
            _logger.LogError(ex, "Unable to read current location.");
            return null;
        }
    }

    /// <summary>Forwards platform location events to subscribers.</summary>
    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        LocationUpdated?.Invoke(this, e.Location);
    }
}