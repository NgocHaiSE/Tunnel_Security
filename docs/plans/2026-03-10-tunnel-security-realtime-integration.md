# Tunnel Security Real-Time Integration Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement real-time sensor data flow from API to frontend, unify node definitions, and remove automatic alert generation from API.

**Architecture:**
- Backend will generate real-time mock sensor data using background timers
- SignalR will push sensor updates to all connected clients
- Frontend will connect to SignalR hub and display real-time data
- Automatic alert generation removed from API - alerts generated only on frontend based on threshold crossings

**Tech Stack:**
- ASP.NET Core 8.0 Backend
- SignalR for real-time communication
- WinUI 3.0 Frontend (Station)
- LiveChartsCore for charts

---

## Task 1: Add Real-Time Mock Data Generation to Backend

**Files:**
- Modify: `Backend/Hubs/SensorHub.cs`
- Create: `Backend/Services/BackgroundSensorSimulation.cs`
- Modify: `Backend/Program.cs`

**Step 1: Create background sensor simulation service**

```csharp
// Backend/Services/BackgroundSensorSimulation.cs
using Microsoft.AspNetCore.SignalR;
using Backend.Hubs;
using Backend.Models;

namespace Backend.Services;

public class BackgroundSensorSimulation
{
    private readonly IHubContext<SensorHub> _hub;
    private readonly Timer _timer;
    private readonly Random _random = new();

    public BackgroundSensorSimulation(IHubContext<SensorHub> hub)
    {
        _hub = hub;
        _timer = new Timer(SimulationTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        _timer.Change(0, 1500); // Every 1.5 seconds
    }

    public void Stop()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private async void SimulationTick(object? state)
    {
        foreach (var station in Mock.MockData.Stations)
        {
            foreach (var line in station.Lines)
            {
                foreach (var node in line.Nodes)
                {
                    foreach (var sensor in node.Sensors)
                    {
                        // Generate new value with random walk
                        double newValue = GenerateRandomValue(sensor);
                        sensor.CurrentValue = newValue;
                        sensor.LastReading = DateTime.UtcNow;

                        // Push to all clients via SignalR
                        await _hub.Clients.All.SendAsync("SensorUpdated", new
                        {
                            sensor.Id,
                            sensor.NodeId,
                            Type = sensor.Type.ToString(),
                            sensor.Name,
                            sensor.CurrentValue,
                            sensor.Unit,
                            sensor.LastReading,
                            NodeStatus = node.Status.ToString(),
                            NodeId = node.Id,
                            NodeName = node.Name,
                            LineId = line.Id,
                            LineName = line.Name
                        });
                    }
                }
            }
        }
    }

    private double GenerateRandomValue(Sensor sensor)
    {
        double current = sensor.CurrentValue ?? 0;
        double nominal = (sensor.WarningThreshold ?? 30) * 0.5;
        double drift = (sensor.CriticalThreshold ?? 50) * 0.1;

        // Revert to mean (5% per tick)
        double reversion = (nominal - current) * 0.05;

        // Random noise
        double noise = (_random.NextDouble() * 2 - 1) * drift;

        // Rare spike (1.5% chance)
        if (_random.NextDouble() < 0.015)
            noise += (_random.NextDouble() > 0.5 ? 1 : -1) * drift * 6;

        double next = current + reversion + noise;

        // Clamp to reasonable range
        double min = 0;
        double max = sensor.CriticalThreshold.HasValue ? sensor.CriticalThreshold.Value * 1.5 : 100;
        return Math.Clamp(next, min, max);
    }
}
```

**Step 2: Register service in Program.cs**

```csharp
// Add after builder.Build()
var app = builder.Build();

// Register background simulation
var sensorSim = app.Services.GetRequiredService<Services.BackgroundSensorSimulation>();
sensorSim.Start();
```

**Step 3: Run and verify**

Run: `dotnet run --project Backend`
Expected: API starts, simulation runs, SignalR hub ready

---

## Task 2: Remove Automatic Alert Generation from API

**Files:**
- Modify: `Backend/Controllers/SensorsController.cs:102-130`

**Step 1: Comment out or remove UpdateNodeStatus logic**

The current `UpdateNodeStatus` method automatically changes node status based on sensor values. This should be removed so alerts are only generated on the frontend.

```csharp
// Comment out or simplify - remove automatic status update
// private void UpdateNodeStatus(Node node)
// {
//     // This logic should be handled by frontend
// }
```

**Step 2: Run and verify**

Run: `dotnet run --project Backend`
Expected: API no longer automatically updates node status

---

## Task 3: Create API Client Service in Frontend

**Files:**
- Create: `Station/Services/ApiSensorClient.cs`
- Modify: `Station/Services/ServiceRegistration.cs` (if exists)

**Step 1: Create SignalR client service**

```csharp
// Station/Services/ApiSensorClient.cs
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Station.Services;

public class ApiSensorClient : IDisposable
{
    private HubConnection? _connection;
    private readonly string _apiBaseUrl;

    public event EventHandler<ApiSensorUpdate>? SensorUpdated;

    public ApiSensorClient(string apiBaseUrl = "http://localhost:5000")
    {
        _apiBaseUrl = apiBaseUrl;
    }

    public async Task ConnectAsync()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{_apiBaseUrl}/hubs/sensors")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ApiSensorUpdate>("SensorUpdated", (update) =>
        {
            SensorUpdated?.Invoke(this, update);
        });

        _connection.Closed += async (error) =>
        {
            await Task.Delay(1000);
            await ConnectAsync(); // Reconnect
        };

        await _connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }

    public void Dispose()
    {
        _connection?.DisposeAsync().AsTask().Wait();
    }
}

public class ApiSensorUpdate
{
    public string Id { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime LastReading { get; set; }
    public string NodeStatus { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string LineId { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
}
```

**Step 2: Test compilation**

Run: `dotnet build Station`
Expected: Build succeeds

---

## Task 4: Update AlertsViewModel to Use API Data

**Files:**
- Modify: `Station/ViewModels/AlertsViewModel.cs`

**Step 1: Add API client reference and connect to SignalR**

Add at the top:
```csharp
using Station.Services;
```

In the class, add:
```csharp
private readonly ApiSensorClient? _apiClient;
private readonly string _useApiMode; // "api" or "local"
```

Update constructor to accept API mode:
```csharp
public AlertsViewModel(bool useApi = false)
{
    _useApiMode = useApi ? "api" : "local";
    if (useApi)
    {
        _apiClient = new ApiSensorClient();
        _apiClient.SensorUpdated += OnApiSensorUpdated;
        _ = _apiClient.ConnectAsync();
    }
    else
    {
        // Original local mock
        MockDataService.Instance.AlertGenerated += OnLiveAlertGenerated;
    }
}
```

Add handler:
```csharp
private void OnApiSensorUpdated(object? sender, ApiSensorUpdate e)
{
    // Check thresholds and generate alert if needed
    // This logic should mirror MockDataService's alert generation
}
```

**Step 2: Run and verify**

Run: `dotnet build Station`
Expected: Build succeeds

---

## Task 5: Update DataPage Charts to Use API Data

**Files:**
- Modify: `Station/Views/DataPage.xaml.cs`

**Step 1: Add API client integration**

At the top add:
```csharp
using Station.Services;
```

Add instance variable:
```csharp
private ApiSensorClient? _apiClient;
private Dictionary<string, List<ApiSensorUpdate>> _apiSensorHistory = new();
```

Update DataPage_Loaded:
```csharp
private async void DataPage_Loaded(object sender, RoutedEventArgs e)
{
    // Check if API is available, fallback to local
    try
    {
        _apiClient = new ApiSensorClient();
        _apiClient.SensorUpdated += OnApiSensorUpdated;
        await _apiClient.ConnectAsync();
        _useApi = true;
    }
    catch
    {
        _useApi = false;
        // Fall back to local mock
    }

    BuildNodeFilterComboBox();
    LoadChartsForAllNodes();
}
```

Add handler:
```csharp
private void OnApiSensorUpdated(object? sender, ApiSensorUpdate e)
{
    DispatcherQueue.TryEnqueue(() =>
    {
        // Update chart with new value
        UpdateChartWithApiData(e);
    });
}

private void UpdateChartWithApiData(ApiSensorUpdate update)
{
    // Add to historical data
    if (!_apiSensorHistory.ContainsKey(update.Id))
        _apiSensorHistory[update.Id] = new List<ApiSensorUpdate>();

    _apiSensorHistory[update.Id].Add(update);

    // Keep only last 50 readings
    if (_apiSensorHistory[update.Id].Count > 50)
        _apiSensorHistory[update.Id].RemoveAt(0);

    // Update chart series
    // (similar to existing UpdateChartData logic)
}
```

**Step 2: Run and verify**

Run: `dotnet build Station`
Expected: Build succeeds

---

## Task 6: Sync Node Definitions Between API and App

**Files:**
- Review: `Backend/Mock/MockData.cs` vs `Station/Services/MockDataService.cs`

**Step 1: Compare node structures**

The API uses:
- Station: "ST01" - "Trạm Giám Sát Cống Quận Cầu Giấy"
- Lines: "L1" (Xuân Thủy), "L2" (Cầu Giấy), "L3" (Trần Thái Tông), etc.
- Nodes: "XT-1", "CG-1", "TTT-1", etc.

The App uses:
- Lines: "LINE-01", "LINE-02", "LINE-03"
- Nodes: "NODE-L1-01", "NODE-L1-02", etc.

**Step 2: Update frontend to match API structure**

Modify `Station/Services/MockDataService.cs` - BuildSensors() method to use API's node structure:

```csharp
// Update to match API structure
private List<SimulatedSensor> BuildSensors()
{
    // Use same lines as API: L0 (HUB), L1 (XT), L2 (CG), L3 (TTT), L4 (DT), L5 (PVD), L6 (NPS)
    var lines = new[]
    {
        ("L0", "Trung tâm điều khiển"),
        ("L1", "Cống Xuân Thủy"),
        ("L2", "Cống Cầu Giấy"),
        ("L3", "Cống Trần Thái Tông"),
        ("L4", "Cống Duy Tân"),
        ("L5", "Cống Phạm Văn Đồng"),
        ("L6", "Cống Nguyễn Phong Sắc"),
    };

    // Build sensors for each line and node matching API
    // ... (implement to match API node counts)
}
```

**Step 3: Test the sync**

Verify both API and App show same node IDs

---

## Task 7: Integration Testing

**Step 1: Start Backend**

Run: `dotnet run --project Backend`
Expected: Backend starts on port 5000, simulation running

**Step 2: Start Frontend**

Run: `dotnet run --project Station`
Expected: Station app launches

**Step 3: Verify real-time data flow**

1. Open Data page in Station app
2. Charts should update every 1.5 seconds with new values from API
3. Open Alerts page
4. Alerts should generate based on threshold crossings

**Step 4: Verify API endpoints**

- GET http://localhost:5000/api/stations - returns station data
- GET http://localhost:5000/api/stations/ST01/nodes - returns nodes
- GET http://localhost:5000/hubs/sensors - SignalR hub accessible

---

## Task 8: Commit Changes

```bash
git add Backend/ Station/
git commit -m "feat: add real-time sensor data via SignalR

- Add BackgroundSensorSimulation service for API-side mock data
- Remove automatic alert generation from API
- Add ApiSensorClient for frontend SignalR connection
- Update AlertsViewModel to support API mode
- Update DataPage charts to display API real-time data
- Sync node definitions between API and App"
```
