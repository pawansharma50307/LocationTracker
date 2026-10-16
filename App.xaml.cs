namespace LocationTracker;

/// <summary>
/// Root application class. Hosts MainPage inside a NavigationPage so the
/// purple "MainView" title bar shows on Android and iOS.
/// </summary>
public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var navigationPage = new NavigationPage(_mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#512BD4"),
            BarTextColor = Colors.White
        };

        return new Window(navigationPage);
    }
}