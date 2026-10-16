using LocationTracker.Data;
using LocationTracker.Services;
using Microsoft.Extensions.Logging;

namespace LocationTracker;

/// <summary>
/// Configures the MAUI host, registers the Maps handler and sets up
/// dependency injection for the app's services.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiMaps() // Google Maps on Android, MapKit on iOS
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // One shared SQLite connection for the whole app.
        builder.Services.AddSingleton<LocationDatabase>();
        builder.Services.AddSingleton<ILocationTrackingService, LocationTrackingService>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}