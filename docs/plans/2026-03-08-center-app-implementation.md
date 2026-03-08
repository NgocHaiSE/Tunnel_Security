# Center App Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the WinUI 3 Center monitoring app — custom dark-theme sidebar shell with 5 pages (Dashboard, Stations, Alerts, Data Streams, System Logs) matching `center.html` reference.

**Architecture:** Custom sidebar + `Frame` navigation (no NavigationView), MVVM with CommunityToolkit.Mvvm, WebView2 for map, SignalR for real-time feed. Dark-only theme matching center.html color palette.

**Tech Stack:** WinUI 3 / Windows App SDK 1.8, CommunityToolkit.Mvvm 8.4, Microsoft.Web.WebView2, Microsoft.AspNetCore.SignalR.Client, DotNetEnv

**Design reference:** `center.html` — `background-dark: #111621`, `panel-dark: #1c2230`, `primary: #144bb8`, `accent-success: #0bda5e`, `accent-warning: #fa6238`

---

## Task 1: Update Colors.xaml + Add Styles.xaml

**Files:**
- Modify: `Center/Styles/Colors.xaml` — replace with dark theme from center.html
- Create: `Center/Styles/Styles.xaml` — shared card/badge/button styles
- Modify: `Center/App.xaml` — merge Styles.xaml

**Step 1: Replace Colors.xaml with dark theme palette**

Open `Center/Styles/Colors.xaml` and replace entire content:

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Brand Colors (from center.html) -->
    <Color x:Key="PrimaryColor">#144bb8</Color>
    <Color x:Key="BackgroundDarkColor">#111621</Color>
    <Color x:Key="PanelDarkColor">#1c2230</Color>
    <Color x:Key="AccentSuccessColor">#0bda5e</Color>
    <Color x:Key="AccentWarningColor">#fa6238</Color>

    <!-- Text -->
    <Color x:Key="TextPrimaryColor">#f1f5f9</Color>
    <Color x:Key="TextMutedColor">#94a3b8</Color>
    <Color x:Key="TextSubtleColor">#64748b</Color>

    <!-- Borders -->
    <Color x:Key="BorderColor">#1e293b</Color>

    <!-- Semantic overlay colors -->
    <Color x:Key="SuccessBgColor">#0a2e18</Color>
    <Color x:Key="WarningBgColor">#2e1a0a</Color>

    <!-- Brushes -->
    <SolidColorBrush x:Key="PrimaryBrush"          Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="BackgroundDarkBrush"   Color="{StaticResource BackgroundDarkColor}"/>
    <SolidColorBrush x:Key="PanelDarkBrush"        Color="{StaticResource PanelDarkColor}"/>
    <SolidColorBrush x:Key="AccentSuccessBrush"    Color="{StaticResource AccentSuccessColor}"/>
    <SolidColorBrush x:Key="AccentWarningBrush"    Color="{StaticResource AccentWarningColor}"/>
    <SolidColorBrush x:Key="TextPrimaryBrush"      Color="{StaticResource TextPrimaryColor}"/>
    <SolidColorBrush x:Key="TextMutedBrush"        Color="{StaticResource TextMutedColor}"/>
    <SolidColorBrush x:Key="TextSubtleBrush"       Color="{StaticResource TextSubtleColor}"/>
    <SolidColorBrush x:Key="BorderBrush"           Color="{StaticResource BorderColor}"/>

    <!-- Corner Radii -->
    <CornerRadius x:Key="RadiusSm">4</CornerRadius>
    <CornerRadius x:Key="RadiusMd">8</CornerRadius>
    <CornerRadius x:Key="RadiusLg">12</CornerRadius>
    <CornerRadius x:Key="RadiusFull">9999</CornerRadius>
</ResourceDictionary>
```

**Step 2: Create Styles.xaml**

Create `Center/Styles/Styles.xaml`:

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Stat Card container -->
    <Style x:Key="StatCardStyle" TargetType="Border">
        <Setter Property="Background" Value="{StaticResource PanelDarkBrush}"/>
        <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="{StaticResource RadiusLg}"/>
        <Setter Property="Padding" Value="20"/>
    </Style>

    <!-- Primary Button -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="CornerRadius" Value="{StaticResource RadiusMd}"/>
        <Setter Property="Padding" Value="16,10"/>
        <Setter Property="BorderThickness" Value="0"/>
    </Style>

    <!-- Sidebar Nav Item (inactive) -->
    <Style x:Key="NavItemStyle" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource TextMutedBrush}"/>
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="FontWeight" Value="Medium"/>
        <Setter Property="HorizontalAlignment" Value="Stretch"/>
        <Setter Property="HorizontalContentAlignment" Value="Left"/>
        <Setter Property="CornerRadius" Value="{StaticResource RadiusMd}"/>
        <Setter Property="Padding" Value="12,10"/>
        <Setter Property="BorderThickness" Value="0"/>
    </Style>

    <!-- Section title -->
    <Style x:Key="SectionTitleStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="16"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
    </Style>

    <!-- Muted label (card subtitle) -->
    <Style x:Key="MutedLabelStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="11"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{StaticResource TextMutedBrush}"/>
    </Style>
</ResourceDictionary>
```

**Step 3: Merge Styles.xaml into App.xaml**

Open `Center/App.xaml`, add `Styles.xaml` after `Colors.xaml`:

```xml
<ResourceDictionary Source="Styles/Colors.xaml"/>
<ResourceDictionary Source="Styles/Styles.xaml"/>
```

Also override system theme background in App.xaml resources:

```xml
<!-- After MergedDictionaries -->
<SolidColorBrush x:Key="ApplicationPageBackgroundThemeBrush" Color="#111621"/>
```

**Step 4: Build to verify**

```bash
cd "d:/Visual Studio/Tunnel_Security"
dotnet build Center/Center.csproj
```
Expected: Build succeeded, 0 errors.

**Step 5: Commit**

```bash
git add Center/Styles/Colors.xaml Center/Styles/Styles.xaml Center/App.xaml
git commit -m "feat(center): add dark theme colors and shared styles from center.html"
```

---

## Task 2: Add NuGet Packages

**Files:**
- Modify: `Center/Center.csproj`

**Step 1: Add packages**

Open `Center/Center.csproj`. Inside the existing `<ItemGroup>` with PackageReferences, add:

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.3595.46" />
<PackageReference Include="DotNetEnv" Version="3.1.1" />
```

Also add `.env` file link (same as Station project):

```xml
<ItemGroup>
    <Content Include="..\\.env">
        <Link>.env</Link>
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
</ItemGroup>
```

**Step 2: Restore and build**

```bash
cd "d:/Visual Studio/Tunnel_Security"
dotnet restore Center/Center.csproj
dotnet build Center/Center.csproj
```
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add Center/Center.csproj
git commit -m "feat(center): add SignalR client, WebView2, DotNetEnv packages"
```

---

## Task 3: Create Models

**Files:**
- Create: `Center/Models/StationInfo.cs`
- Create: `Center/Models/AlertEntry.cs`
- Create: `Center/Models/LogEntry.cs`

**Step 1: Create `Center/Models/StationInfo.cs`**

```csharp
using System;

namespace Center.Models
{
    public class StationInfo
    {
        public string StationCode { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public StationStatus Status { get; set; }
        public int LatencyMs { get; set; }
        public double TransferRateMbps { get; set; }
        public DateTimeOffset LastHeartbeat { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public enum StationStatus { Online, Warning, Offline }
}
```

**Step 2: Create `Center/Models/AlertEntry.cs`**

```csharp
using System;

namespace Center.Models
{
    public class AlertEntry
    {
        public Guid Id { get; set; }
        public string StationCode { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public bool IsAcknowledged { get; set; }
    }

    public enum AlertSeverity { Info, Warning, Critical }
}
```

**Step 3: Create `Center/Models/LogEntry.cs`**

```csharp
using System;

namespace Center.Models
{
    public class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public enum LogLevel { Debug, Info, Warning, Error, System }
}
```

**Step 4: Build to verify**

```bash
dotnet build Center/Center.csproj
```
Expected: Build succeeded.

**Step 5: Commit**

```bash
git add Center/Models/
git commit -m "feat(center): add StationInfo, AlertEntry, LogEntry models"
```

---

## Task 4: Create Services

**Files:**
- Create: `Center/Services/CenterApiService.cs`
- Create: `Center/Services/SignalRService.cs`
- Create: `Center/Config/EnvironmentConfig.cs`

**Step 1: Create `Center/Config/EnvironmentConfig.cs`**

```csharp
using DotNetEnv;
using System;
using System.IO;

namespace Center.Config
{
    public static class EnvironmentConfig
    {
        private static bool _loaded;

        public static void Load()
        {
            if (_loaded) return;
            var paths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".env")
            };
            foreach (var p in paths)
            {
                var full = Path.GetFullPath(p);
                if (File.Exists(full)) { Env.Load(full); _loaded = true; return; }
            }
        }

        public static string BackendBaseUrl
        {
            get { Load(); return Env.GetString("CENTER_BACKEND_URL", "http://localhost:5281"); }
        }
    }
}
```

**Step 2: Create `Center/Services/CenterApiService.cs`**

```csharp
using Center.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Center.Services
{
    public class CenterApiService
    {
        private readonly HttpClient _http;

        public CenterApiService(string baseUrl)
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<List<StationInfo>> GetStationsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<StationInfo>>("/api/stations")
                       ?? new List<StationInfo>();
            }
            catch { return new List<StationInfo>(); }
        }

        public async Task<List<AlertEntry>> GetAlertsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<AlertEntry>>("/api/alerts")
                       ?? new List<AlertEntry>();
            }
            catch { return new List<AlertEntry>(); }
        }

        public async Task<bool> AcknowledgeAlertAsync(Guid alertId)
        {
            try
            {
                var resp = await _http.PostAsync($"/api/alerts/{alertId}/acknowledge", null);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
```

**Step 3: Create `Center/Services/SignalRService.cs`**

```csharp
using Center.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Center.Services
{
    public class SignalRService
    {
        private HubConnection? _connection;

        public event Action<StationInfo>? StationUpdated;
        public event Action<AlertEntry>? AlertReceived;
        public event Action<LogEntry>? LogReceived;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public async Task StartAsync(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<StationInfo>("StationUpdated",  s => StationUpdated?.Invoke(s));
            _connection.On<AlertEntry>( "AlertReceived",   a => AlertReceived?.Invoke(a));
            _connection.On<LogEntry>(   "LogReceived",     l => LogReceived?.Invoke(l));

            try { await _connection.StartAsync(); }
            catch { /* connection failed, will retry via AutoReconnect */ }
        }

        public async Task StopAsync()
        {
            if (_connection != null)
                await _connection.StopAsync();
        }
    }
}
```

**Step 4: Build to verify**

```bash
dotnet build Center/Center.csproj
```
Expected: Build succeeded.

**Step 5: Commit**

```bash
git add Center/Config/ Center/Services/
git commit -m "feat(center): add EnvironmentConfig, CenterApiService, SignalRService"
```

---

## Task 5: MainViewModel + ViewModels Skeleton

**Files:**
- Create: `Center/ViewModels/MainViewModel.cs`
- Create: `Center/ViewModels/DashboardViewModel.cs`
- Create: `Center/ViewModels/StationsViewModel.cs`
- Create: `Center/ViewModels/AlertsViewModel.cs`
- Create: `Center/ViewModels/DataStreamsViewModel.cs`
- Create: `Center/ViewModels/SystemLogsViewModel.cs`

**Step 1: Create `Center/ViewModels/MainViewModel.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Center.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pageTitle = "Dashboard";

        [ObservableProperty]
        private int _unreadAlertCount = 0;

        [ObservableProperty]
        private bool _hasUnreadAlerts = false;

        partial void OnUnreadAlertCountChanged(int value)
        {
            HasUnreadAlerts = value > 0;
        }
    }
}
```

**Step 2: Create `Center/ViewModels/DashboardViewModel.cs`**

```csharp
using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Center.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty] private int _totalStations;
        [ObservableProperty] private int _activeStreams;
        [ObservableProperty] private double _systemHealthPercent;
        [ObservableProperty] private string _dataRateDisplay = "0 MB/s";

        public ObservableCollection<StationInfo> RecentStations { get; } = new();
        public ObservableCollection<LogEntry> LiveLogs { get; } = new();

        public void AddLog(LogEntry entry)
        {
            if (LiveLogs.Count > 200) LiveLogs.RemoveAt(0);
            LiveLogs.Add(entry);
        }

        public void UpdateStation(StationInfo station)
        {
            for (int i = 0; i < RecentStations.Count; i++)
            {
                if (RecentStations[i].StationCode == station.StationCode)
                {
                    RecentStations[i] = station;
                    return;
                }
            }
            if (RecentStations.Count < 5)
                RecentStations.Add(station);
        }
    }
}
```

**Step 3: Create `Center/ViewModels/StationsViewModel.cs`**

```csharp
using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Center.ViewModels
{
    public partial class StationsViewModel : ObservableObject
    {
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string _statusFilter = "All";

        public ObservableCollection<StationInfo> Stations { get; } = new();
        public ObservableCollection<StationInfo> FilteredStations { get; } = new();

        partial void OnSearchQueryChanged(string value) => ApplyFilter();
        partial void OnStatusFilterChanged(string value) => ApplyFilter();

        private void ApplyFilter()
        {
            FilteredStations.Clear();
            foreach (var s in Stations)
            {
                bool matchesSearch = string.IsNullOrEmpty(SearchQuery) ||
                    s.StationCode.Contains(SearchQuery, System.StringComparison.OrdinalIgnoreCase) ||
                    s.StationName.Contains(SearchQuery, System.StringComparison.OrdinalIgnoreCase);
                bool matchesStatus = StatusFilter == "All" || s.Status.ToString() == StatusFilter;
                if (matchesSearch && matchesStatus)
                    FilteredStations.Add(s);
            }
        }

        public void LoadStations(System.Collections.Generic.List<StationInfo> stations)
        {
            Stations.Clear();
            foreach (var s in stations) Stations.Add(s);
            ApplyFilter();
        }
    }
}
```

**Step 4: Create `Center/ViewModels/AlertsViewModel.cs`**

```csharp
using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Center.ViewModels
{
    public partial class AlertsViewModel : ObservableObject
    {
        [ObservableProperty] private string _severityFilter = "All";
        [ObservableProperty] private string _stationFilter = "All";

        public ObservableCollection<AlertEntry> Alerts { get; } = new();
        public ObservableCollection<AlertEntry> FilteredAlerts { get; } = new();

        public void AddAlert(AlertEntry alert)
        {
            Alerts.Insert(0, alert);
            ApplyFilter();
        }

        partial void OnSeverityFilterChanged(string value) => ApplyFilter();
        partial void OnStationFilterChanged(string value) => ApplyFilter();

        private void ApplyFilter()
        {
            FilteredAlerts.Clear();
            foreach (var a in Alerts)
            {
                bool matchSev = SeverityFilter == "All" || a.Severity.ToString() == SeverityFilter;
                bool matchSta = StationFilter == "All" || a.StationCode == StationFilter;
                if (matchSev && matchSta) FilteredAlerts.Add(a);
            }
        }
    }
}
```

**Step 5: Create `Center/ViewModels/DataStreamsViewModel.cs`**

```csharp
using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Center.ViewModels
{
    public partial class DataStreamsViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isPaused = false;
        [ObservableProperty] private string _stationFilter = "All";

        public ObservableCollection<LogEntry> Entries { get; } = new();

        public void AddEntry(LogEntry entry)
        {
            if (IsPaused) return;
            if (StationFilter != "All" && !entry.Source.Contains(StationFilter)) return;
            if (Entries.Count > 500) Entries.RemoveAt(0);
            Entries.Add(entry);
        }

        public void Clear() => Entries.Clear();
    }
}
```

**Step 6: Create `Center/ViewModels/SystemLogsViewModel.cs`**

```csharp
using Center.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Center.ViewModels
{
    public partial class SystemLogsViewModel : ObservableObject
    {
        [ObservableProperty] private string _levelFilter = "All";
        [ObservableProperty] private string _keyword = string.Empty;

        public ObservableCollection<LogEntry> Logs { get; } = new();
        public ObservableCollection<LogEntry> FilteredLogs { get; } = new();

        partial void OnLevelFilterChanged(string value) => ApplyFilter();
        partial void OnKeywordChanged(string value) => ApplyFilter();

        public void AddLog(LogEntry entry)
        {
            Logs.Insert(0, entry);
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            FilteredLogs.Clear();
            foreach (var l in Logs)
            {
                bool matchLevel = LevelFilter == "All" || l.Level.ToString() == LevelFilter;
                bool matchKey = string.IsNullOrEmpty(Keyword) ||
                    l.Message.Contains(Keyword, System.StringComparison.OrdinalIgnoreCase) ||
                    l.Source.Contains(Keyword, System.StringComparison.OrdinalIgnoreCase);
                if (matchLevel && matchKey) FilteredLogs.Add(l);
            }
        }
    }
}
```

**Step 7: Build to verify**

```bash
dotnet build Center/Center.csproj
```
Expected: Build succeeded.

**Step 8: Commit**

```bash
git add Center/ViewModels/
git commit -m "feat(center): add all ViewModels with filtering and observable properties"
```

---

## Task 6: MainWindow Shell (Sidebar + Header + Frame)

**Files:**
- Modify: `Center/MainWindow.xaml`
- Modify: `Center/MainWindow.xaml.cs`

**Step 1: Replace `Center/MainWindow.xaml`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Window
    x:Class="Center.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:Center"
    Title="Tunnel Security — Control Center">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="256"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- SIDEBAR -->
        <Border Grid.Column="0"
                Background="{StaticResource PanelDarkBrush}"
                BorderBrush="{StaticResource BorderBrush}"
                BorderThickness="0,0,1,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <!-- Logo -->
                <StackPanel Grid.Row="0" Margin="24,28,24,20">
                    <TextBlock Text="Tunnel Security"
                               FontSize="18" FontWeight="Bold"
                               Foreground="{StaticResource PrimaryBrush}"/>
                    <TextBlock Text="Control Center v1.0"
                               FontSize="11"
                               Foreground="{StaticResource TextMutedBrush}"
                               Margin="0,2,0,0"/>
                </StackPanel>

                <!-- Nav Items -->
                <StackPanel Grid.Row="1" Margin="12,0" Spacing="2">
                    <Button x:Name="NavDashboard" Style="{StaticResource NavItemStyle}"
                            Click="NavDashboard_Click">
                        <StackPanel Orientation="Horizontal" Spacing="12">
                            <FontIcon Glyph="&#xE80F;" FontSize="18"/>
                            <TextBlock Text="Dashboard" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Button>
                    <Button x:Name="NavStations" Style="{StaticResource NavItemStyle}"
                            Click="NavStations_Click">
                        <StackPanel Orientation="Horizontal" Spacing="12">
                            <FontIcon Glyph="&#xE968;" FontSize="18"/>
                            <TextBlock Text="Stations" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Button>
                    <Button x:Name="NavAlerts" Style="{StaticResource NavItemStyle}"
                            Click="NavAlerts_Click">
                        <Grid>
                            <StackPanel Orientation="Horizontal" Spacing="12">
                                <FontIcon Glyph="&#xE7BA;" FontSize="18"/>
                                <TextBlock Text="Alerts" VerticalAlignment="Center"/>
                            </StackPanel>
                            <!-- Alert badge -->
                            <Border x:Name="AlertBadge"
                                    Background="{StaticResource AccentWarningBrush}"
                                    CornerRadius="9999"
                                    MinWidth="18" Height="18"
                                    HorizontalAlignment="Right"
                                    VerticalAlignment="Center"
                                    Padding="4,0"
                                    Visibility="Collapsed">
                                <TextBlock x:Name="AlertBadgeText"
                                           FontSize="10" FontWeight="Bold"
                                           Foreground="White"
                                           HorizontalAlignment="Center"/>
                            </Border>
                        </Grid>
                    </Button>
                    <Button x:Name="NavDataStreams" Style="{StaticResource NavItemStyle}"
                            Click="NavDataStreams_Click">
                        <StackPanel Orientation="Horizontal" Spacing="12">
                            <FontIcon Glyph="&#xE9D9;" FontSize="18"/>
                            <TextBlock Text="Data Streams" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Button>
                    <Button x:Name="NavSystemLogs" Style="{StaticResource NavItemStyle}"
                            Click="NavSystemLogs_Click">
                        <StackPanel Orientation="Horizontal" Spacing="12">
                            <FontIcon Glyph="&#xE9F9;" FontSize="18"/>
                            <TextBlock Text="System Logs" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Button>
                </StackPanel>

                <!-- Add Station Button -->
                <Border Grid.Row="2"
                        BorderBrush="{StaticResource BorderBrush}"
                        BorderThickness="0,1,0,0"
                        Padding="16,16">
                    <Button Style="{StaticResource PrimaryButtonStyle}"
                            HorizontalAlignment="Stretch"
                            Click="AddStation_Click">
                        <StackPanel Orientation="Horizontal" Spacing="8" HorizontalAlignment="Center">
                            <FontIcon Glyph="&#xE710;" FontSize="16" Foreground="White"/>
                            <TextBlock Text="Add Station" Foreground="White"/>
                        </StackPanel>
                    </Button>
                </Border>
            </Grid>
        </Border>

        <!-- MAIN AREA -->
        <Grid Grid.Column="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="64"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- HEADER -->
            <Border Grid.Row="0"
                    Background="{StaticResource PanelDarkBrush}"
                    BorderBrush="{StaticResource BorderBrush}"
                    BorderThickness="0,0,0,1">
                <Grid Margin="32,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <!-- Page title -->
                    <StackPanel Grid.Column="0" Orientation="Horizontal"
                                Spacing="12" VerticalAlignment="Center">
                        <Border Background="#1d2d4d" CornerRadius="8" Padding="8">
                            <FontIcon x:Name="HeaderIcon" Glyph="&#xE80F;"
                                      FontSize="18"
                                      Foreground="{StaticResource PrimaryBrush}"/>
                        </Border>
                        <TextBlock x:Name="PageTitleText"
                                   Text="Dashboard"
                                   FontSize="17" FontWeight="SemiBold"
                                   Foreground="{StaticResource TextPrimaryBrush}"
                                   VerticalAlignment="Center"/>
                    </StackPanel>

                    <!-- Header controls -->
                    <StackPanel Grid.Column="1" Orientation="Horizontal"
                                Spacing="8" VerticalAlignment="Center">
                        <!-- Notification button -->
                        <Button x:Name="NotificationBtn"
                                Background="#1e293b"
                                BorderThickness="0"
                                CornerRadius="8"
                                Width="40" Height="40"
                                Click="Notification_Click">
                            <Grid>
                                <FontIcon Glyph="&#xEA8F;" FontSize="18"
                                          Foreground="{StaticResource TextMutedBrush}"/>
                                <Ellipse x:Name="NotifDot"
                                         Width="8" Height="8"
                                         Fill="{StaticResource AccentWarningBrush}"
                                         HorizontalAlignment="Right" VerticalAlignment="Top"
                                         Margin="0,4,4,0"
                                         Visibility="Collapsed"/>
                            </Grid>
                        </Button>
                        <!-- Settings button -->
                        <Button Background="#1e293b"
                                BorderThickness="0"
                                CornerRadius="8"
                                Width="40" Height="40"
                                Click="Settings_Click">
                            <FontIcon Glyph="&#xE713;" FontSize="18"
                                      Foreground="{StaticResource TextMutedBrush}"/>
                        </Button>
                        <!-- Time display -->
                        <TextBlock x:Name="ClockText"
                                   FontSize="14" FontWeight="SemiBold"
                                   Foreground="{StaticResource TextPrimaryBrush}"
                                   VerticalAlignment="Center"
                                   Margin="8,0,0,0"/>
                    </StackPanel>
                </Grid>
            </Border>

            <!-- PAGE FRAME -->
            <Frame x:Name="ContentFrame" Grid.Row="1"
                   Background="{StaticResource BackgroundDarkBrush}"/>
        </Grid>
    </Grid>
</Window>
```

**Step 2: Replace `Center/MainWindow.xaml.cs`**

```csharp
using Center.ViewModels;
using Center.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Center
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new();
        private Button? _activeNav;

        public MainWindow()
        {
            InitializeComponent();
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1600, 960));

            // Start clock
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (_, _) => ClockText.Text = DateTimeOffset.Now.ToString("HH:mm:ss");
            timer.Start();
            ClockText.Text = DateTimeOffset.Now.ToString("HH:mm:ss");

            // Navigate to Dashboard on load
            Loaded += (_, _) => SetActivePage(NavDashboard, "Dashboard", "\uE80F", typeof(DashboardPage));
        }

        private void SetActivePage(Button nav, string title, string glyph, Type pageType)
        {
            // Reset previous active
            if (_activeNav != null)
            {
                _activeNav.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                _activeNav.Foreground = (SolidColorBrush)Application.Current.Resources["TextMutedBrush"];
            }

            // Set new active
            _activeNav = nav;
            nav.Background = (SolidColorBrush)Application.Current.Resources["PrimaryBrush"];
            nav.Foreground = new SolidColorBrush(Colors.White);

            PageTitleText.Text = title;
            HeaderIcon.Glyph = glyph;
            ContentFrame.Navigate(pageType);
        }

        private void NavDashboard_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavDashboard, "Dashboard", "\uE80F", typeof(DashboardPage));

        private void NavStations_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavStations, "Stations", "\uE968", typeof(StationsPage));

        private void NavAlerts_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavAlerts, "Alerts", "\uE7BA", typeof(AlertsPage));

        private void NavDataStreams_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavDataStreams, "Data Streams", "\uE9D9", typeof(DataStreamsPage));

        private void NavSystemLogs_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavSystemLogs, "System Logs", "\uE9F9", typeof(SystemLogsPage));

        private void AddStation_Click(object s, RoutedEventArgs e)
        {
            // TODO: open AddStationDialog
        }

        private void Notification_Click(object s, RoutedEventArgs e)
            => SetActivePage(NavAlerts, "Alerts", "\uE7BA", typeof(AlertsPage));

        private void Settings_Click(object s, RoutedEventArgs e)
        {
            // TODO: settings flyout
        }

        public void SetAlertBadge(int count)
        {
            AlertBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            AlertBadgeText.Text = count > 99 ? "99+" : count.ToString();
            NotifDot.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
```

**Step 3: Build to verify**

```bash
dotnet build Center/Center.csproj
```
Expected: Errors about missing page types (DashboardPage etc.) — that's expected. Fix by creating stub pages in Task 7.

**Step 4: Commit**

```bash
git add Center/MainWindow.xaml Center/MainWindow.xaml.cs
git commit -m "feat(center): implement custom sidebar shell with header and Frame navigation"
```

---

## Task 7: Stub Pages (compile fix)

**Files:**
- Create: `Center/Views/DashboardPage.xaml` + `.cs`
- Create: `Center/Views/StationsPage.xaml` + `.cs`
- Create: `Center/Views/AlertsPage.xaml` + `.cs`
- Create: `Center/Views/DataStreamsPage.xaml` + `.cs`
- Create: `Center/Views/SystemLogsPage.xaml` + `.cs`

**Step 1: For each page, create minimal XAML stub**

Pattern — replace `DashboardPage` with each page name:

`Center/Views/DashboardPage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="Center.Views.DashboardPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Background="{StaticResource BackgroundDarkBrush}">
    <TextBlock Text="Dashboard" Foreground="White" Margin="32" FontSize="24"/>
</Page>
```

`Center/Views/DashboardPage.xaml.cs`:
```csharp
using Microsoft.UI.Xaml.Controls;
namespace Center.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardPage() { InitializeComponent(); }
    }
}
```

Repeat for `StationsPage`, `AlertsPage`, `DataStreamsPage`, `SystemLogsPage`.

**Step 2: Build to verify — all errors resolved**

```bash
dotnet build Center/Center.csproj
```
Expected: Build succeeded, 0 errors.

**Step 3: Commit**

```bash
git add Center/Views/
git commit -m "feat(center): add stub pages for all 5 navigation targets"
```

---

## Task 8: DashboardPage — Stat Cards + Station Table

**Files:**
- Modify: `Center/Views/DashboardPage.xaml`
- Modify: `Center/Views/DashboardPage.xaml.cs`

**Step 1: Implement DashboardPage.xaml**

Replace the stub with full layout (4 stat cards + station table preview). The `ScrollViewer` wraps everything so long content scrolls:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="Center.Views.DashboardPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="using:Center.Models"
    Background="{StaticResource BackgroundDarkBrush}">

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="32" Spacing="32">

            <!-- Row 1: Stat Cards -->
            <Grid ColumnSpacing="16">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition/><ColumnDefinition/>
                    <ColumnDefinition/><ColumnDefinition/>
                </Grid.ColumnDefinitions>

                <!-- Total Stations -->
                <Border Grid.Column="0" Style="{StaticResource StatCardStyle}">
                    <StackPanel Spacing="4">
                        <TextBlock Text="TOTAL STATIONS" Style="{StaticResource MutedLabelStyle}"/>
                        <Grid>
                            <TextBlock x:Name="TotalStationsText" Text="0"
                                       FontSize="28" FontWeight="Bold"
                                       Foreground="{StaticResource TextPrimaryBrush}"/>
                            <TextBlock x:Name="OnlineCountText" Text="0 online"
                                       FontSize="12"
                                       Foreground="{StaticResource AccentSuccessBrush}"
                                       HorizontalAlignment="Right"
                                       VerticalAlignment="Bottom"/>
                        </Grid>
                    </StackPanel>
                </Border>

                <!-- Active Alerts -->
                <Border Grid.Column="1" Style="{StaticResource StatCardStyle}">
                    <StackPanel Spacing="4">
                        <TextBlock Text="ACTIVE ALERTS" Style="{StaticResource MutedLabelStyle}"/>
                        <Grid>
                            <TextBlock x:Name="ActiveAlertsText" Text="0"
                                       FontSize="28" FontWeight="Bold"
                                       Foreground="{StaticResource TextPrimaryBrush}"/>
                            <TextBlock x:Name="CriticalCountText" Text="0 critical"
                                       FontSize="12"
                                       Foreground="{StaticResource AccentWarningBrush}"
                                       HorizontalAlignment="Right"
                                       VerticalAlignment="Bottom"/>
                        </Grid>
                    </StackPanel>
                </Border>

                <!-- System Health -->
                <Border Grid.Column="2" Style="{StaticResource StatCardStyle}">
                    <StackPanel Spacing="4">
                        <TextBlock Text="SYSTEM HEALTH" Style="{StaticResource MutedLabelStyle}"/>
                        <Grid>
                            <TextBlock x:Name="SystemHealthText" Text="—"
                                       FontSize="28" FontWeight="Bold"
                                       Foreground="{StaticResource TextPrimaryBrush}"/>
                            <TextBlock x:Name="HealthLabelText" Text="Optimal"
                                       FontSize="12"
                                       Foreground="{StaticResource TextMutedBrush}"
                                       HorizontalAlignment="Right"
                                       VerticalAlignment="Bottom"/>
                        </Grid>
                    </StackPanel>
                </Border>

                <!-- Data Rate -->
                <Border Grid.Column="3" Style="{StaticResource StatCardStyle}">
                    <StackPanel Spacing="4">
                        <TextBlock Text="DATA RATE" Style="{StaticResource MutedLabelStyle}"/>
                        <Grid>
                            <TextBlock x:Name="DataRateText" Text="0 MB/s"
                                       FontSize="28" FontWeight="Bold"
                                       Foreground="{StaticResource TextPrimaryBrush}"/>
                            <TextBlock Text="Peak"
                                       FontSize="12"
                                       Foreground="{StaticResource PrimaryBrush}"
                                       HorizontalAlignment="Right"
                                       VerticalAlignment="Bottom"/>
                        </Grid>
                    </StackPanel>
                </Border>
            </Grid>

            <!-- Row 2: Station Table Preview -->
            <StackPanel Spacing="12">
                <Grid>
                    <TextBlock Text="Station Overview" Style="{StaticResource SectionTitleStyle}"/>
                    <HyperlinkButton Content="View All →"
                                     HorizontalAlignment="Right"
                                     Foreground="{StaticResource PrimaryBrush}"
                                     Click="ViewAllStations_Click"/>
                </Grid>

                <Border Style="{StaticResource StatCardStyle}" Padding="0">
                    <ListView x:Name="StationsList"
                              ItemsSource="{x:Bind ViewModel.RecentStations}"
                              SelectionMode="None"
                              Padding="0">
                        <ListView.ItemContainerStyle>
                            <Style TargetType="ListViewItem">
                                <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
                                <Setter Property="Padding" Value="24,12"/>
                                <Setter Property="Background" Value="Transparent"/>
                            </Style>
                        </ListView.ItemContainerStyle>
                        <ListView.ItemTemplate>
                            <DataTemplate x:DataType="models:StationInfo">
                                <Grid ColumnSpacing="16">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="2*"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="*"/>
                                    </Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="{x:Bind StationCode}"
                                                   FontWeight="SemiBold" FontSize="13"
                                                   Foreground="{StaticResource TextPrimaryBrush}"/>
                                        <TextBlock Text="{x:Bind Area}"
                                                   FontSize="11"
                                                   Foreground="{StaticResource TextMutedBrush}"/>
                                    </StackPanel>
                                    <TextBlock Grid.Column="1"
                                               Text="{x:Bind Status}"
                                               FontSize="12"
                                               Foreground="{StaticResource TextMutedBrush}"
                                               VerticalAlignment="Center"/>
                                    <TextBlock Grid.Column="2"
                                               Text="{x:Bind LatencyMs}"
                                               FontSize="12"
                                               Foreground="{StaticResource TextMutedBrush}"
                                               VerticalAlignment="Center"/>
                                    <TextBlock Grid.Column="3"
                                               Text="{x:Bind LastHeartbeat}"
                                               FontSize="11"
                                               Foreground="{StaticResource TextSubtleBrush}"
                                               VerticalAlignment="Center"/>
                                </Grid>
                            </DataTemplate>
                        </ListView.ItemTemplate>
                    </ListView>
                </Border>
            </StackPanel>

        </StackPanel>
    </ScrollViewer>
</Page>
```

**Step 2: Implement DashboardPage.xaml.cs**

```csharp
using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Center.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; } = new();

        public DashboardPage()
        {
            InitializeComponent();
            LoadMockData();
        }

        private void LoadMockData()
        {
            TotalStationsText.Text = "12";
            OnlineCountText.Text = "10 online";
            ActiveAlertsText.Text = "3";
            CriticalCountText.Text = "1 critical";
            SystemHealthText.Text = "98.2%";
            DataRateText.Text = "842 MB/s";

            ViewModel.RecentStations.Add(new StationInfo
            {
                StationCode = "TRM-001", StationName = "Trạm Hầm Thủ Thiêm",
                Area = "TP.HCM", Status = StationStatus.Online,
                LatencyMs = 12, LastHeartbeat = DateTimeOffset.Now.AddSeconds(-5)
            });
            ViewModel.RecentStations.Add(new StationInfo
            {
                StationCode = "TRM-002", StationName = "Trạm Hầm Bình Điền",
                Area = "TP.HCM", Status = StationStatus.Warning,
                LatencyMs = 142, LastHeartbeat = DateTimeOffset.Now.AddSeconds(-15)
            });
            ViewModel.RecentStations.Add(new StationInfo
            {
                StationCode = "TRM-003", StationName = "Trạm Cầu Cần Thơ",
                Area = "Cần Thơ", Status = StationStatus.Online,
                LatencyMs = 22, LastHeartbeat = DateTimeOffset.Now.AddSeconds(-3)
            });
        }

        private void ViewAllStations_Click(object s, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Frame.Navigate(typeof(StationsPage));
        }
    }
}
```

**Step 3: Build to verify**

```bash
dotnet build Center/Center.csproj
```
Expected: Build succeeded.

**Step 4: Commit**

```bash
git add Center/Views/DashboardPage.xaml Center/Views/DashboardPage.xaml.cs
git commit -m "feat(center): implement DashboardPage with stat cards and station table"
```

---

## Task 9: StationsPage — Full Station Table

**Files:**
- Modify: `Center/Views/StationsPage.xaml`
- Modify: `Center/Views/StationsPage.xaml.cs`

**Step 1: Implement StationsPage.xaml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="Center.Views.StationsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="using:Center.Models"
    Background="{StaticResource BackgroundDarkBrush}">

    <Grid Margin="32" RowSpacing="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Title -->
        <TextBlock Grid.Row="0" Text="Station Management"
                   Style="{StaticResource SectionTitleStyle}" FontSize="20"/>

        <!-- Toolbar -->
        <Grid Grid.Row="1" ColumnSpacing="12">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="300"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <AutoSuggestBox Grid.Column="0"
                            PlaceholderText="Search stations..."
                            Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay}"
                            QueryIcon="Find"
                            Background="#1e293b"
                            Foreground="{StaticResource TextPrimaryBrush}"/>

            <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
                <Button Content="All" Tag="All" Click="FilterBtn_Click"
                        x:Name="FilterAll" Style="{StaticResource PrimaryButtonStyle}"/>
                <Button Content="Online" Tag="Online" Click="FilterBtn_Click"
                        x:Name="FilterOnline" Background="#0a2e18"
                        Foreground="{StaticResource AccentSuccessBrush}"
                        BorderThickness="1" BorderBrush="{StaticResource AccentSuccessBrush}"
                        CornerRadius="8" Padding="16,10"/>
                <Button Content="Warning" Tag="Warning" Click="FilterBtn_Click"
                        x:Name="FilterWarning" Background="#2e1a0a"
                        Foreground="{StaticResource AccentWarningBrush}"
                        BorderThickness="1" BorderBrush="{StaticResource AccentWarningBrush}"
                        CornerRadius="8" Padding="16,10"/>
                <Button Content="Offline" Tag="Offline" Click="FilterBtn_Click"
                        x:Name="FilterOffline" Background="#1e293b"
                        Foreground="{StaticResource TextMutedBrush}"
                        BorderThickness="1" BorderBrush="{StaticResource BorderBrush}"
                        CornerRadius="8" Padding="16,10"/>
            </StackPanel>
        </Grid>

        <!-- Table -->
        <Border Grid.Row="2" Style="{StaticResource StatCardStyle}" Padding="0">
            <ListView ItemsSource="{x:Bind ViewModel.FilteredStations}"
                      SelectionMode="None">
                <ListView.Header>
                    <Grid Padding="24,12" Background="#16202e">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="2*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="120"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="STATION" Style="{StaticResource MutedLabelStyle}"/>
                        <TextBlock Grid.Column="1" Text="STATUS"  Style="{StaticResource MutedLabelStyle}"/>
                        <TextBlock Grid.Column="2" Text="LATENCY" Style="{StaticResource MutedLabelStyle}"/>
                        <TextBlock Grid.Column="3" Text="LAST HEARTBEAT" Style="{StaticResource MutedLabelStyle}"/>
                        <TextBlock Grid.Column="4" Text="ACTION" Style="{StaticResource MutedLabelStyle}" HorizontalAlignment="Right"/>
                    </Grid>
                </ListView.Header>
                <ListView.ItemContainerStyle>
                    <Style TargetType="ListViewItem">
                        <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
                        <Setter Property="Padding" Value="24,14"/>
                    </Style>
                </ListView.ItemContainerStyle>
                <ListView.ItemTemplate>
                    <DataTemplate x:DataType="models:StationInfo">
                        <Grid ColumnSpacing="16">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="2*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="120"/>
                            </Grid.ColumnDefinitions>
                            <StackPanel Grid.Column="0">
                                <TextBlock Text="{x:Bind StationCode}" FontWeight="SemiBold" FontSize="13"
                                           Foreground="{StaticResource TextPrimaryBrush}"/>
                                <TextBlock Text="{x:Bind StationName}" FontSize="11"
                                           Foreground="{StaticResource TextMutedBrush}"/>
                            </StackPanel>
                            <!-- Status badge rendered via converter or code-behind -->
                            <TextBlock Grid.Column="1" Text="{x:Bind Status}" FontSize="12"
                                       Foreground="{StaticResource TextMutedBrush}"
                                       VerticalAlignment="Center"/>
                            <TextBlock Grid.Column="2" FontSize="12" VerticalAlignment="Center"
                                       Foreground="{StaticResource TextPrimaryBrush}">
                                <Run Text="{x:Bind LatencyMs}"/><Run Text=" ms"/>
                            </TextBlock>
                            <TextBlock Grid.Column="3" FontSize="11" VerticalAlignment="Center"
                                       Foreground="{StaticResource TextMutedBrush}"
                                       Text="{x:Bind LastHeartbeat}"/>
                            <Button Grid.Column="4"
                                    Style="{StaticResource PrimaryButtonStyle}"
                                    Content="CONNECT"
                                    FontSize="11" Padding="12,6"
                                    HorizontalAlignment="Right"/>
                        </Grid>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </Border>
    </Grid>
</Page>
```

**Step 2: Implement StationsPage.xaml.cs**

```csharp
using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace Center.Views
{
    public sealed partial class StationsPage : Page
    {
        public StationsViewModel ViewModel { get; } = new();

        public StationsPage()
        {
            InitializeComponent();
            ViewModel.LoadStations(GetMockStations());
        }

        private void FilterBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StatusFilter = (sender as Button)?.Tag?.ToString() ?? "All";
        }

        private static List<StationInfo> GetMockStations() => new()
        {
            new() { StationCode="TRM-001", StationName="Hầm Thủ Thiêm", Area="TP.HCM",
                    Status=StationStatus.Online, LatencyMs=12, LastHeartbeat=DateTimeOffset.Now.AddSeconds(-5) },
            new() { StationCode="TRM-002", StationName="Hầm Bình Điền", Area="TP.HCM",
                    Status=StationStatus.Warning, LatencyMs=142, LastHeartbeat=DateTimeOffset.Now.AddSeconds(-20) },
            new() { StationCode="TRM-003", StationName="Cầu Cần Thơ", Area="Cần Thơ",
                    Status=StationStatus.Online, LatencyMs=22, LastHeartbeat=DateTimeOffset.Now.AddSeconds(-3) },
            new() { StationCode="TRM-004", StationName="Hầm Đèo Cả", Area="Phú Yên",
                    Status=StationStatus.Offline, LatencyMs=0, LastHeartbeat=DateTimeOffset.Now.AddMinutes(-30) },
        };
    }
}
```

**Step 3: Build + commit**

```bash
dotnet build Center/Center.csproj
git add Center/Views/StationsPage.xaml Center/Views/StationsPage.xaml.cs
git commit -m "feat(center): implement StationsPage with filterable station table"
```

---

## Task 10: AlertsPage

**Files:**
- Modify: `Center/Views/AlertsPage.xaml`
- Modify: `Center/Views/AlertsPage.xaml.cs`

**Step 1: Implement AlertsPage.xaml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="Center.Views.AlertsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="using:Center.Models"
    Background="{StaticResource BackgroundDarkBrush}">

    <Grid Margin="32" RowSpacing="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="Alerts" Style="{StaticResource SectionTitleStyle}" FontSize="20"/>

        <!-- Filter bar -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Spacing="8">
            <Button Content="All"      Tag="All"      Click="SeverityFilter_Click" Style="{StaticResource PrimaryButtonStyle}"/>
            <Button Content="Critical" Tag="Critical" Click="SeverityFilter_Click"
                    Background="#2e0a0a" Foreground="#ef4444"
                    BorderThickness="1" BorderBrush="#ef4444" CornerRadius="8" Padding="16,10"/>
            <Button Content="Warning"  Tag="Warning"  Click="SeverityFilter_Click"
                    Background="#2e1a0a" Foreground="{StaticResource AccentWarningBrush}"
                    BorderThickness="1" BorderBrush="{StaticResource AccentWarningBrush}" CornerRadius="8" Padding="16,10"/>
            <Button Content="Info"     Tag="Info"     Click="SeverityFilter_Click"
                    Background="#0a1e2e" Foreground="{StaticResource PrimaryBrush}"
                    BorderThickness="1" BorderBrush="{StaticResource PrimaryBrush}" CornerRadius="8" Padding="16,10"/>
        </StackPanel>

        <!-- Alert list -->
        <ListView Grid.Row="2"
                  ItemsSource="{x:Bind ViewModel.FilteredAlerts}"
                  SelectionMode="None">
            <ListView.ItemContainerStyle>
                <Style TargetType="ListViewItem">
                    <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
                    <Setter Property="Padding" Value="0"/>
                    <Setter Property="Margin" Value="0,4"/>
                </Style>
            </ListView.ItemContainerStyle>
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="models:AlertEntry">
                    <Border Style="{StaticResource StatCardStyle}" Padding="20,16">
                        <Grid ColumnSpacing="16">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="120"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0" Text="{x:Bind Severity}"
                                       FontSize="11" FontWeight="Bold"
                                       Foreground="{StaticResource AccentWarningBrush}"
                                       VerticalAlignment="Center"/>
                            <StackPanel Grid.Column="1">
                                <TextBlock Text="{x:Bind Message}" FontSize="13"
                                           Foreground="{StaticResource TextPrimaryBrush}"/>
                                <TextBlock Text="{x:Bind StationCode}" FontSize="11"
                                           Foreground="{StaticResource TextMutedBrush}"/>
                            </StackPanel>
                            <TextBlock Grid.Column="2" Text="{x:Bind Timestamp}"
                                       FontSize="11" Foreground="{StaticResource TextSubtleBrush}"
                                       VerticalAlignment="Center"/>
                            <Button Grid.Column="3" Content="Acknowledge"
                                    FontSize="11" Padding="12,6"
                                    Background="#1e293b" Foreground="{StaticResource TextMutedBrush}"
                                    BorderThickness="1" BorderBrush="{StaticResource BorderBrush}"
                                    CornerRadius="6" HorizontalAlignment="Right"/>
                        </Grid>
                    </Border>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Grid>
</Page>
```

**Step 2: Implement AlertsPage.xaml.cs**

```csharp
using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Center.Views
{
    public sealed partial class AlertsPage : Page
    {
        public AlertsViewModel ViewModel { get; } = new();

        public AlertsPage()
        {
            InitializeComponent();
            ViewModel.AddAlert(new AlertEntry { Id=Guid.NewGuid(), StationCode="TRM-002",
                Severity=AlertSeverity.Warning, Message="High latency detected (142ms)",
                Timestamp=DateTimeOffset.Now.AddMinutes(-2) });
            ViewModel.AddAlert(new AlertEntry { Id=Guid.NewGuid(), StationCode="TRM-004",
                Severity=AlertSeverity.Critical, Message="Station offline — heartbeat timeout",
                Timestamp=DateTimeOffset.Now.AddMinutes(-30) });
            ViewModel.AddAlert(new AlertEntry { Id=Guid.NewGuid(), StationCode="TRM-001",
                Severity=AlertSeverity.Info, Message="Data sync completed",
                Timestamp=DateTimeOffset.Now.AddMinutes(-1) });
        }

        private void SeverityFilter_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SeverityFilter = (sender as Button)?.Tag?.ToString() ?? "All";
        }
    }
}
```

**Step 3: Build + commit**

```bash
dotnet build Center/Center.csproj
git add Center/Views/AlertsPage.xaml Center/Views/AlertsPage.xaml.cs
git commit -m "feat(center): implement AlertsPage with severity filtering"
```

---

## Task 11: DataStreamsPage — Live Terminal

**Files:**
- Modify: `Center/Views/DataStreamsPage.xaml`
- Modify: `Center/Views/DataStreamsPage.xaml.cs`

**Step 1: Implement DataStreamsPage.xaml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="Center.Views.DataStreamsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="using:Center.Models"
    Background="{StaticResource BackgroundDarkBrush}">

    <Grid Margin="32" RowSpacing="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="Data Streams"
                   Style="{StaticResource SectionTitleStyle}" FontSize="20"/>

        <!-- Toolbar -->
        <Grid Grid.Row="1" ColumnSpacing="12">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="200"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <ComboBox Grid.Column="0" x:Name="StationFilterCombo"
                      PlaceholderText="All Stations"
                      Background="#1e293b" Foreground="{StaticResource TextPrimaryBrush}"
                      SelectionChanged="StationFilter_Changed"/>
            <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="4" VerticalAlignment="Center">
                <Ellipse Width="8" Height="8" Fill="{StaticResource AccentSuccessBrush}" x:Name="StreamDot"/>
                <TextBlock Text="STREAMING" FontSize="10" FontWeight="Bold"
                           Foreground="{StaticResource PrimaryBrush}"/>
            </StackPanel>
            <StackPanel Grid.Column="3" Orientation="Horizontal" Spacing="8">
                <ToggleButton x:Name="PauseBtn" Content="⏸ Pause" Click="Pause_Click"
                              Background="#1e293b" Foreground="{StaticResource TextMutedBrush}"
                              BorderThickness="1" BorderBrush="{StaticResource BorderBrush}"
                              CornerRadius="8" Padding="12,8"/>
                <Button Content="Clear" Click="Clear_Click"
                        Background="#1e293b" Foreground="{StaticResource TextMutedBrush}"
                        BorderThickness="1" BorderBrush="{StaticResource BorderBrush}"
                        CornerRadius="8" Padding="12,8"/>
            </StackPanel>
        </Grid>

        <!-- Terminal -->
        <Border Grid.Row="2" CornerRadius="{StaticResource RadiusLg}"
                Background="#020617"
                BorderBrush="{StaticResource BorderBrush}" BorderThickness="1">
            <Grid>
                <!-- Terminal header -->
                <Border BorderBrush="{StaticResource BorderBrush}" BorderThickness="0,0,0,1"
                        Padding="16,8" VerticalAlignment="Top" Background="#0d1117" CornerRadius="12,12,0,0">
                    <Grid>
                        <TextBlock x:Name="SessionText" Text="SESSION: CENTER-MAIN"
                                   FontFamily="Consolas" FontSize="10"
                                   Foreground="{StaticResource TextSubtleBrush}"/>
                        <FontIcon Glyph="&#xE756;" FontSize="14"
                                  Foreground="{StaticResource TextSubtleBrush}"
                                  HorizontalAlignment="Right"/>
                    </Grid>
                </Border>

                <!-- Log entries -->
                <ScrollViewer x:Name="LogScrollViewer"
                              Margin="0,40,0,0"
                              VerticalScrollBarVisibility="Auto">
                    <ItemsRepeater ItemsSource="{x:Bind ViewModel.Entries}">
                        <ItemsRepeater.ItemTemplate>
                            <DataTemplate x:DataType="models:LogEntry">
                                <TextBlock FontFamily="Consolas" FontSize="12"
                                           Margin="16,2"
                                           Foreground="#cbd5e1"
                                           TextWrapping="Wrap">
                                    <Run Foreground="#0bda5e" Text="{x:Bind Timestamp}"/>
                                    <Run Text=" "/>
                                    <Run Text="{x:Bind Source}"/>
                                    <Run Text=": "/>
                                    <Run Text="{x:Bind Message}"/>
                                </TextBlock>
                            </DataTemplate>
                        </ItemsRepeater.ItemTemplate>
                    </ItemsRepeater>
                </ScrollViewer>
            </Grid>
        </Border>
    </Grid>
</Page>
```

**Step 2: Implement DataStreamsPage.xaml.cs**

```csharp
using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Center.Views
{
    public sealed partial class DataStreamsPage : Page
    {
        public DataStreamsViewModel ViewModel { get; } = new();
        private DispatcherTimer? _mockTimer;

        public DataStreamsPage()
        {
            InitializeComponent();
            StartMockFeed();
        }

        private void StartMockFeed()
        {
            _mockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            var rng = new Random();
            string[] stations = { "TRM-001", "TRM-002", "TRM-003" };
            string[] msgs = {
                "Heartbeat acknowledged", "Data sync complete",
                "Sensor reading: temp=28.4°C", "Radar scan nominal",
                "Alert threshold check passed", "Camera feed stable"
            };
            _mockTimer.Tick += (_, _) =>
            {
                ViewModel.AddEntry(new LogEntry
                {
                    Timestamp = DateTimeOffset.Now,
                    Level = LogLevel.Info,
                    Source = stations[rng.Next(stations.Length)],
                    Message = msgs[rng.Next(msgs.Length)]
                });
                LogScrollViewer.ScrollToVerticalOffset(LogScrollViewer.ExtentHeight);
            };
            _mockTimer.Start();
        }

        private void Pause_Click(object s, RoutedEventArgs e)
        {
            ViewModel.IsPaused = !ViewModel.IsPaused;
            (s as ToggleButton)!.Content = ViewModel.IsPaused ? "▶ Resume" : "⏸ Pause";
        }

        private void Clear_Click(object s, RoutedEventArgs e) => ViewModel.Clear();

        private void StationFilter_Changed(object s, SelectionChangedEventArgs e)
        {
            var item = (s as ComboBox)?.SelectedItem?.ToString() ?? "All";
            ViewModel.StationFilter = item;
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            _mockTimer?.Stop();
        }
    }
}
```

**Step 3: Build + commit**

```bash
dotnet build Center/Center.csproj
git add Center/Views/DataStreamsPage.xaml Center/Views/DataStreamsPage.xaml.cs
git commit -m "feat(center): implement DataStreamsPage with live terminal feed"
```

---

## Task 12: SystemLogsPage

**Files:**
- Modify: `Center/Views/SystemLogsPage.xaml`
- Modify: `Center/Views/SystemLogsPage.xaml.cs`

**Step 1: Implement SystemLogsPage.xaml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="Center.Views.SystemLogsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="using:Center.Models"
    Background="{StaticResource BackgroundDarkBrush}">

    <Grid Margin="32" RowSpacing="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <Grid Grid.Row="0">
            <TextBlock Text="System Logs" Style="{StaticResource SectionTitleStyle}" FontSize="20"/>
            <Button Content="Export CSV" HorizontalAlignment="Right"
                    Style="{StaticResource PrimaryButtonStyle}"
                    Click="Export_Click"/>
        </Grid>

        <!-- Filters -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Spacing="12">
            <AutoSuggestBox PlaceholderText="Search logs..."
                            Text="{x:Bind ViewModel.Keyword, Mode=TwoWay}"
                            QueryIcon="Find" Width="260"
                            Background="#1e293b" Foreground="{StaticResource TextPrimaryBrush}"/>
            <ComboBox x:Name="LevelCombo" PlaceholderText="All Levels"
                      SelectionChanged="LevelFilter_Changed"
                      Background="#1e293b" Foreground="{StaticResource TextPrimaryBrush}">
                <ComboBoxItem Content="All"/>
                <ComboBoxItem Content="Info"/>
                <ComboBoxItem Content="Warning"/>
                <ComboBoxItem Content="Error"/>
            </ComboBox>
        </StackPanel>

        <!-- Table -->
        <Border Grid.Row="2" Style="{StaticResource StatCardStyle}" Padding="0">
            <ListView ItemsSource="{x:Bind ViewModel.FilteredLogs}" SelectionMode="None">
                <ListView.Header>
                    <Grid Padding="24,12" Background="#16202e">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="180"/>
                            <ColumnDefinition Width="100"/>
                            <ColumnDefinition Width="150"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="TIMESTAMP" Style="{StaticResource MutedLabelStyle}"/>
                        <TextBlock Grid.Column="1" Text="LEVEL"     Style="{StaticResource MutedLabelStyle}"/>
                        <TextBlock Grid.Column="2" Text="SOURCE"    Style="{StaticResource MutedLabelStyle}"/>
                        <TextBlock Grid.Column="3" Text="MESSAGE"   Style="{StaticResource MutedLabelStyle}"/>
                    </Grid>
                </ListView.Header>
                <ListView.ItemContainerStyle>
                    <Style TargetType="ListViewItem">
                        <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
                        <Setter Property="Padding" Value="24,12"/>
                    </Style>
                </ListView.ItemContainerStyle>
                <ListView.ItemTemplate>
                    <DataTemplate x:DataType="models:LogEntry">
                        <Grid ColumnSpacing="16">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="180"/>
                                <ColumnDefinition Width="100"/>
                                <ColumnDefinition Width="150"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0" Text="{x:Bind Timestamp}" FontSize="11"
                                       FontFamily="Consolas" Foreground="{StaticResource TextSubtleBrush}"/>
                            <TextBlock Grid.Column="1" Text="{x:Bind Level}" FontSize="11"
                                       FontWeight="SemiBold" Foreground="{StaticResource TextMutedBrush}"/>
                            <TextBlock Grid.Column="2" Text="{x:Bind Source}" FontSize="12"
                                       Foreground="{StaticResource TextMutedBrush}"/>
                            <TextBlock Grid.Column="3" Text="{x:Bind Message}" FontSize="12"
                                       Foreground="{StaticResource TextPrimaryBrush}" TextWrapping="Wrap"/>
                        </Grid>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </Border>
    </Grid>
</Page>
```

**Step 2: Implement SystemLogsPage.xaml.cs**

```csharp
using Center.Models;
using Center.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Text;
using Windows.Storage.Pickers;

namespace Center.Views
{
    public sealed partial class SystemLogsPage : Page
    {
        public SystemLogsViewModel ViewModel { get; } = new();

        public SystemLogsPage()
        {
            InitializeComponent();
            LoadMockLogs();
        }

        private void LoadMockLogs()
        {
            ViewModel.AddLog(new LogEntry { Timestamp=DateTimeOffset.Now.AddMinutes(-1), Level=LogLevel.Info,    Source="CenterBackend", Message="Station TRM-001 heartbeat received" });
            ViewModel.AddLog(new LogEntry { Timestamp=DateTimeOffset.Now.AddMinutes(-5), Level=LogLevel.Warning, Source="CenterBackend", Message="Station TRM-002 latency spike: 142ms" });
            ViewModel.AddLog(new LogEntry { Timestamp=DateTimeOffset.Now.AddMinutes(-30),Level=LogLevel.Error,   Source="CenterBackend", Message="Station TRM-004 connection lost" });
            ViewModel.AddLog(new LogEntry { Timestamp=DateTimeOffset.Now.AddHours(-1),   Level=LogLevel.Info,    Source="System",        Message="Scheduled integrity check completed" });
        }

        private void LevelFilter_Changed(object s, SelectionChangedEventArgs e)
        {
            ViewModel.LevelFilter = (LevelCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        }

        private async void Export_Click(object s, RoutedEventArgs e)
        {
            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add("CSV", new[] { ".csv" });
            picker.SuggestedFileName = $"system-logs-{DateTime.Now:yyyy-MM-dd}";
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSaveFileAsync();
            if (file == null) return;
            var sb = new StringBuilder("Timestamp,Level,Source,Message\n");
            foreach (var log in ViewModel.Logs)
                sb.AppendLine($"{log.Timestamp},{log.Level},{log.Source},\"{log.Message}\"");
            await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString());
        }
    }
}
```

**Step 3: Expose MainWindow from App.cs**

Open `Center/App.xaml.cs`, add a public property:

```csharp
public static Window? MainWindow { get; private set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    MainWindow = new MainWindow();
    MainWindow.Activate();
}
```

**Step 4: Final build**

```bash
dotnet build Center/Center.csproj
```
Expected: Build succeeded, 0 errors.

**Step 5: Commit**

```bash
git add Center/Views/SystemLogsPage.xaml Center/Views/SystemLogsPage.xaml.cs Center/App.xaml.cs
git commit -m "feat(center): implement SystemLogsPage with filter and CSV export"
```

---

## Task 13: Final Polish + Dark Theme Force

**Files:**
- Modify: `Center/App.xaml` — force dark theme
- Modify: `Center/MainWindow.xaml` — set MicaBackdrop

**Step 1: Force dark theme in App.xaml**

In `Center/App.xaml`, add `RequestedTheme="Dark"` to the `<Application>` element:

```xml
<Application
    x:Class="Center.App"
    RequestedTheme="Dark"
    ...>
```

**Step 2: Add MicaBackdrop to MainWindow**

In `Center/MainWindow.xaml`, add after the opening `<Window>` tag:

```xml
<Window.SystemBackdrop>
    <MicaBackdrop Kind="BaseAlt"/>
</Window.SystemBackdrop>
```

**Step 3: Final build**

```bash
dotnet build Center/Center.csproj
```
Expected: Build succeeded.

**Step 4: Final commit**

```bash
git add Center/App.xaml Center/MainWindow.xaml
git commit -m "feat(center): force dark theme and add Mica backdrop"
```

---

## Summary

| Task | What it builds |
|---|---|
| 1 | Dark theme colors + shared styles |
| 2 | NuGet packages (SignalR, WebView2, DotNetEnv) |
| 3 | Models (StationInfo, AlertEntry, LogEntry) |
| 4 | Services (CenterApiService, SignalRService, EnvironmentConfig) |
| 5 | All 6 ViewModels with filtering + observable properties |
| 6 | MainWindow shell — custom sidebar + header + Frame |
| 7 | Stub pages to make project compile |
| 8 | DashboardPage — stat cards + station table |
| 9 | StationsPage — full table with search/filter |
| 10 | AlertsPage — severity-filtered alert list |
| 11 | DataStreamsPage — live terminal feed |
| 12 | SystemLogsPage — log table + CSV export |
| 13 | Dark theme enforcement + Mica backdrop |
