using LocationTracker.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MapControl = Microsoft.Maui.Controls.Maps.Map;

namespace LocationTracker.Controls;

/// <summary>
/// Extends the standard .NET MAUI Map with heat-map rendering.
/// Stored points are grouped into small grid cells. The number of points in a
/// cell is its "heat", and each cell is drawn as a Circle coloured from blue
/// (rarely visited) to red (most visited). The cell size grows as the user
/// zooms out so the circles always stay visible.
/// </summary>
public class HeatMap : MapControl
{
    // Approximate metres per degree of latitude.
    private const double MetersPerDegreeLatitude = 111_320d;

    // Roughly how many cells should fit across the visible map.
    private const double CellsAcrossScreen = 80d;

    public static readonly BindableProperty PointsProperty = BindableProperty.Create(
        nameof(Points), typeof(IEnumerable<LocationPoint>), typeof(HeatMap), null,
        propertyChanged: (bindable, _, _) => ((HeatMap)bindable).Redraw());

    public static readonly BindableProperty CellSizeMetersProperty = BindableProperty.Create(
        nameof(CellSizeMeters), typeof(double), typeof(HeatMap), 25d,
        propertyChanged: (bindable, _, _) => ((HeatMap)bindable).Redraw());

    public static readonly BindableProperty ShowPathProperty = BindableProperty.Create(
        nameof(ShowPath), typeof(bool), typeof(HeatMap), false,
        propertyChanged: (bindable, _, _) => ((HeatMap)bindable).Redraw());

    // Gradient stops from cold to hot.
    private static readonly Color[] Gradient =
    {
        Color.FromArgb("#1565FF"), // blue
        Color.FromArgb("#00C853"), // green
        Color.FromArgb("#FFD600"), // yellow
        Color.FromArgb("#FF6D00"), // orange
        Color.FromArgb("#D50000")  // red
    };

    // Cell size used for the last draw; lets us skip redraws when the zoom barely changes.
    private double _effectiveCellSize;

    public HeatMap()
    {
        IsShowingUser = true;
        MapType = MapType.Street;
    }

    /// <summary>Points to draw as heat.</summary>
    public IEnumerable<LocationPoint>? Points
    {
        get => (IEnumerable<LocationPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    /// <summary>Smallest heat grid cell size in metres (used when zoomed in).</summary>
    public double CellSizeMeters
    {
        get => (double)GetValue(CellSizeMetersProperty);
        set => SetValue(CellSizeMetersProperty, value);
    }

    /// <summary>Whether to also connect the points with a route line.</summary>
    public bool ShowPath
    {
        get => (bool)GetValue(ShowPathProperty);
        set => SetValue(ShowPathProperty, value);
    }

    /// <summary>Rebuilds all heat circles (and the optional path) from Points.</summary>
    public void Redraw()
    {
        MapElements.Clear();

        var points = Points?.ToList();
        if (points is null || points.Count == 0)
        {
            return;
        }

        if (ShowPath && points.Count > 1)
        {
            DrawPath(points);
        }

        DrawHeat(points);
    }

    /// <summary>Moves the camera so every stored point is visible.</summary>
    public void ZoomToPoints()
    {
        var points = Points?.ToList();
        if (points is null || points.Count == 0)
        {
            return;
        }

        double minLat = points.Min(p => p.Latitude);
        double maxLat = points.Max(p => p.Latitude);
        double minLon = points.Min(p => p.Longitude);
        double maxLon = points.Max(p => p.Longitude);

        var center = new Location((minLat + maxLat) / 2, (minLon + maxLon) / 2);

        // 30% padding plus a minimum span so a single point is not over-zoomed.
        double latSpan = Math.Max((maxLat - minLat) * 1.3, 0.005);
        double lonSpan = Math.Max((maxLon - minLon) * 1.3, 0.005);

        MoveToRegion(new MapSpan(center, latSpan, lonSpan));
    }

    /// <summary>Redraws when the user zooms enough to change the cell size by more than 20%.</summary>
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName != nameof(VisibleRegion))
        {
            return;
        }

        double newSize = GetEffectiveCellSize();
        if (Math.Abs(newSize - _effectiveCellSize) > _effectiveCellSize * 0.2)
        {
            Redraw();
        }
    }

    /// <summary>
    /// Cell size actually used for drawing: never smaller than CellSizeMeters, but grows
    /// when zoomed out so the screen holds about 80 cells and circles stay visible.
    /// </summary>
    private double GetEffectiveCellSize()
    {
        double minimum = Math.Max(CellSizeMeters, 1d);

        if (VisibleRegion is null)
        {
            return minimum;
        }

        double screenWidthMeters = VisibleRegion.Radius.Meters * 2;
        return Math.Max(minimum, screenWidthMeters / CellsAcrossScreen);
    }

    /// <summary>Groups points into grid cells and draws one coloured circle per cell.</summary>
    private void DrawHeat(IReadOnlyList<LocationPoint> points)
    {
        double cellSize = GetEffectiveCellSize();
        _effectiveCellSize = cellSize;

        // Convert metres to degrees; longitude degrees shrink with latitude.
        double avgLatRadians = points.Average(p => p.Latitude) * Math.PI / 180d;
        double latStep = cellSize / MetersPerDegreeLatitude;
        double lonStep = cellSize / (MetersPerDegreeLatitude * Math.Max(Math.Cos(avgLatRadians), 0.01));

        var cells = points
            .GroupBy(p => (Row: (long)Math.Floor(p.Latitude / latStep),
                           Col: (long)Math.Floor(p.Longitude / lonStep)))
            .Select(g => new
            {
                Count = g.Count(),
                Latitude = g.Average(p => p.Latitude),
                Longitude = g.Average(p => p.Longitude)
            })
            .ToList();

        int maxCount = cells.Max(c => c.Count);

        // Draw cold cells first so hot cells appear on top.
        foreach (var cell in cells.OrderBy(c => c.Count))
        {
            // Log scale stops one very busy spot from washing out everything else.
            double intensity = maxCount <= 1 ? 0d : Math.Log(cell.Count) / Math.Log(maxCount);
            Color color = ColorForIntensity(intensity);

            MapElements.Add(new Circle
            {
                Center = new Location(cell.Latitude, cell.Longitude),
                Radius = Distance.FromMeters(cellSize * (0.5 + (0.4 * intensity))),
                StrokeColor = color.WithAlpha(0.9f),
                StrokeWidth = 2,
                FillColor = color.WithAlpha(0.55f)
            });
        }
    }

    /// <summary>Connects points in time order with a red line.</summary>
    private void DrawPath(IEnumerable<LocationPoint> points)
    {
        var line = new Polyline
        {
            StrokeColor = Color.FromArgb("#B0D50000"),
            StrokeWidth = 6
        };

        foreach (var p in points.OrderBy(p => p.TimestampUtc))
        {
            line.Geopath.Add(new Location(p.Latitude, p.Longitude));
        }

        MapElements.Add(line);
    }

    /// <summary>Maps intensity (0..1) to a colour by blending the two nearest gradient stops.</summary>
    internal static Color ColorForIntensity(double intensity)
    {
        intensity = Math.Clamp(intensity, 0d, 1d);

        double scaled = intensity * (Gradient.Length - 1);
        int lower = (int)Math.Floor(scaled);
        int upper = Math.Min(lower + 1, Gradient.Length - 1);
        float t = (float)(scaled - lower);

        Color a = Gradient[lower];
        Color b = Gradient[upper];

        return new Color(
            a.Red + ((b.Red - a.Red) * t),
            a.Green + ((b.Green - a.Green) * t),
            a.Blue + ((b.Blue - a.Blue) * t));
    }
}