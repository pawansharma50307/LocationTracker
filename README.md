# Location Tracker – .NET MAUI Heat Map App

**Course:** MSCS-533-A01 Software Engineering and Multiplatform App Development
**Author:** Pawan Sharma
**Project:** Final Software Development Project – Multiplatform "Location Tracker" App

A cross-platform mobile app built with C# and .NET MAUI. It tracks the user's location, saves every reading to a local SQLite database, and displays the saved locations as a heat map on top of the .NET MAUI Maps control.

\---

## Features

* **Location tracking** using the .NET MAUI `Geolocation` API (foreground listening, about every 5 seconds).
* **SQLite storage** with `sqlite-net-pcl`. Readings are only saved when the user has moved 5 m or more, or 15 seconds have passed, which filters out GPS jitter.
* **Heat map** built by extending the .NET MAUI `Map` control:

  * Points are grouped into grid cells.
  * Each cell is drawn as a circle coloured **blue → green → yellow → orange → red** based on how many readings it holds.
  * A logarithmic scale keeps one busy spot from washing out the rest.
  * Cell size grows when zooming out so the heat stays visible at any zoom level.
* **Sample routes** (Mountain View, CA and Roscoff, France) for demos and screenshots.
* Runs on **Android** and **iOS** from one code base.

\---

## Project Structure

```
LocationTracker/
├── Controls/
│   └── HeatMap.cs                    # Custom Map control that renders the heat map
├── Data/
│   ├── LocationDatabase.cs           # SQLite data access (save, load, clear)
│   └── SampleDataSeeder.cs           # Generates sample routes for demos
├── Models/
│   └── LocationPoint.cs              # SQLite table model for one location reading
├── Services/
│   ├── ILocationTrackingService.cs   # Location service interface
│   └── LocationTrackingService.cs    # Wraps the MAUI Geolocation API
├── Platforms/
│   ├── Android/AndroidManifest.xml   # Android permissions + Google Maps API key
│   └── iOS/Info.plist                # iOS location permission messages
├── App.xaml / App.xaml.cs            # App entry, "MainView" navigation bar
├── MainPage.xaml / MainPage.xaml.cs  # Main screen (full-screen heat map)
├── MauiProgram.cs                    # Dependency injection + UseMauiMaps()
└── LocationTracker.csproj
```

\---

## Technologies

|Technology|Purpose|
|-|-|
|C# / .NET MAUI|Cross-platform UI and app logic|
|Microsoft.Maui.Controls.Maps|Map control (Google Maps on Android, Apple Maps on iOS)|
|sqlite-net-pcl + SQLitePCLRaw.bundle\_green|Local SQLite database|
|Microsoft.Extensions.DependencyInjection|Service registration|

\---

## Requirements

* Visual Studio 2022 or newer with the **.NET Multi-platform App UI development** workload
* .NET 9 or .NET 10 SDK
* An Android phone or emulator. For an emulator, use a **Google APIs** or **Google Play** system image.
* A **Google Maps API key** with **Maps SDK for Android** enabled and billing linked
* (Optional) A Mac with Xcode to run on iOS

\---

