using System.Collections.ObjectModel;
using LocationTracker.Data;
using LocationTracker.Models;
using LocationTracker.Services;
using Microsoft.Maui.Maps;

namespace LocationTracker;

/// <summary>
/// Main screen. Loads saved locations from SQLite, draws them on the HeatMap,
/// and records new GPS readings while the page is visible.
/// </summary>
public partial class MainPage : ContentPage
{
    /// <summary>
    /// Which data the app shows:
    /// <list type="bullet">
    /// <item>SampleRoute.MountainView / SampleRoute.Roscoff: resets the database to a sample
    /// route on start-up (used for the report screenshots).</item>
    /// <item>SampleRoute.None: normal mode, tracks the device's real GPS position.</item>
    /// </list>
    /// </summary>
    private const SampleRoute DataSource = SampleRoute.MountainView;

    // Ignore readings closer than this to the last saved point (GPS jitter)...
    private const double MinDistanceMeters = 5d;

    // ...unless this much time has passed, so staying in one place builds up heat.
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(15);

    private readonly ILocationTrackingService _locationService;
    private readonly LocationDatabase _database;

    // In-memory copy of stored points that the heat map is bound to.
    private readonly ObservableCollection<LocationPoint> _points = new();

    private LocationPoint? _lastSaved;
    private bool _isLoaded;

    public MainPage(ILocationTrackingService locationService, LocationDatabase database)
    {
        InitializeComponent();

        _locationService = locationService;
        _database = database;

        _locationService.LocationUpdated += OnLocationUpdated;
        HeatMapView.Points = _points;
    }

    /// <summary>Options for <see cref="DataSource"/>.</summary>
    private enum SampleRoute
    {
        None,
        MountainView,
        Roscoff
    }

    /// <summary>Loads data and (in live mode) starts tracking when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            if (!_isLoaded)
            {
                _isLoaded = true;

                if (DataSource != SampleRoute.None)
                {
                    await LoadSampleRouteAsync(DataSource);
                }

                await LoadSavedPointsAsync();
                await CenterMapAsync();
            }

            // Live mode: start recording the user's real position automatically.
            if (DataSource == SampleRoute.None && !_locationService.IsTracking)
            {
                bool started = await _locationService.StartAsync();
                if (!started)
                {
                    await DisplayAlert(
                        "Location unavailable",
                        "Please allow location access and make sure location services are on.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load locations: {ex.Message}", "OK");
        }
    }

    /// <summary>Stops tracking when the page is hidden to save battery.</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _locationService.Stop();
    }

    /// <summary>Replaces the database contents with the chosen sample route.</summary>
    private async Task LoadSampleRouteAsync(SampleRoute route)
    {
        List<LocationPoint> sample = route switch
        {
            SampleRoute.MountainView =>
                SampleDataSeeder.Generate(SampleDataSeeder.MountainViewRoute, spacingMeters: 30, stopPoints: 24),
            SampleRoute.Roscoff =>
                SampleDataSeeder.Generate(SampleDataSeeder.RoscoffRoute, spacingMeters: 150, stopPoints: 12),
            _ => new List<LocationPoint>()
        };

        await _database.ClearAllAsync();
        await _database.SaveLocationsAsync(sample);
    }

    /// <summary>Reads all points from SQLite and draws them.</summary>
    private async Task LoadSavedPointsAsync()
    {
        var saved = await _database.GetAllLocationsAsync();

        _points.Clear();
        foreach (var point in saved)
        {
            _points.Add(point);
        }

        _lastSaved = saved.LastOrDefault();
        HeatMapView.Redraw();
    }

    /// <summary>Centres on the saved history, or on the current position if there is none.</summary>
    private async Task CenterMapAsync()
    {
        if (_points.Count > 0)
        {
            HeatMapView.ZoomToPoints();
            return;
        }

        var current = await _locationService.GetCurrentLocationAsync();
        if (current is not null)
        {
            HeatMapView.MoveToRegion(MapSpan.FromCenterAndRadius(current, Distance.FromKilometers(1)));
        }
    }

    /// <summary>Called for every new GPS reading (live mode only).</summary>
    private async void OnLocationUpdated(object? sender, Location location)
    {
        try
        {
            if (!ShouldSave(location))
            {
                return;
            }

            var point = new LocationPoint
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Accuracy = location.Accuracy,
                TimestampUtc = DateTime.UtcNow
            };

            await _database.SaveLocationAsync(point);
            _lastSaved = point;

            // Map elements must be changed on the main thread.
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _points.Add(point);
                HeatMapView.Redraw();
            });
        }
        catch (Exception ex)
        {
            // async void handler: never let an exception crash the app.
            System.Diagnostics.Debug.WriteLine($"Failed to save location: {ex}");
        }
    }

    /// <summary>Decides whether a reading is worth storing.</summary>
    private bool ShouldSave(Location location)
    {
        if (_lastSaved is null)
        {
            return true;
        }

        var previous = new Location(_lastSaved.Latitude, _lastSaved.Longitude);
        double movedMeters = Location.CalculateDistance(previous, location, DistanceUnits.Kilometers) * 1000d;
        TimeSpan elapsed = DateTime.UtcNow - _lastSaved.TimestampUtc;

        return movedMeters >= MinDistanceMeters || elapsed >= MinInterval;
    }
}