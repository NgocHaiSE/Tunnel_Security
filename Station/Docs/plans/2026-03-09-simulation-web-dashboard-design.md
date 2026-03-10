# Simulation Web Dashboard Design
Date: 2026-03-09

## Summary
Expose MockDataService via an embedded Kestrel HTTP server (port 5050) with REST API + WebSocket, and a single-file `simulation.html` dashboard for controlling and observing the station simulation.

## Architecture

```
WinUI App (Station.exe)
  └── SimulationApiServer.cs   [Services/]
        ├── Kestrel  http://localhost:5050
        ├── REST API  (control endpoints)
        └── WebSocket  ws://localhost:5050/ws  (real-time push)
              ↕ JSON
        simulation.html  (opened in browser)
```

## C# Component: SimulationApiServer

**File:** `Station/Services/SimulationApiServer.cs`

Embedded Kestrel server started from `App.xaml.cs` on app launch.
Bridges REST/WebSocket calls to `MockDataService.Instance`.

### REST Endpoints

| Method | Path | Action |
|--------|------|--------|
| GET | `/api/sensors` | All 16 sensors + current values/levels |
| GET | `/api/alerts` | Alert history (up to 200) |
| POST | `/api/simulation/start` | Start MockDataService timers |
| POST | `/api/simulation/stop` | Stop MockDataService timers |
| POST | `/api/alerts/fire` | Manually fire a fake alert |
| POST | `/api/sensors/{id}/fault` | Inject fault into sensor |
| DELETE | `/api/sensors/{id}/fault` | Clear sensor fault |
| PUT | `/api/sensors/{id}/params` | Update nominalValue, driftSpeed, thresholds |

### WebSocket: `ws://localhost:5050/ws`

Push-only from server. Two message types:
```json
{ "type": "sensorTick", "sensorId": "SNS-A1-T01", "value": 27.3, "level": "Normal", "unit": "°C", "ts": "2026-03-09T..." }
{ "type": "alertGenerated", "id": "...", "title": "...", "severity": "Critical", "category": "Temperature", "nodeId": "NODE-A1", "ts": "..." }
```

## Web Component: simulation.html

Single standalone HTML file. No external build tools.
Connects to `http://localhost:5050` (REST) and `ws://localhost:5050/ws` (WebSocket).

### Layout (4 panels)
1. **Live Sensors Grid** — 16 cards, color-coded Normal(green)/Warning(amber)/Critical(red), value updates via WebSocket
2. **Alert Feed** — scrollable stream of incoming alerts, color by severity
3. **Control Panel** — Start/Stop buttons + manual alert form (title, category, severity dropdown)
4. **Sensor Inspector** — click sensor card → drawer shows full params, Inject/Clear Fault buttons, editable fields for nominalValue/driftSpeed/thresholds

## Startup Integration

In `App.xaml.cs`:
```csharp
private SimulationApiServer? _simServer;

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    _simServer = new SimulationApiServer();
    _ = _simServer.StartAsync();
    MockDataService.Instance.Start();
    // ... existing window setup
}
```

## Dependencies

Add to `Station.csproj`:
```xml
<PackageReference Include="Microsoft.AspNetCore.App" />
```
Or target `net8.0-windows10...` with `Microsoft.AspNetCore` already available.
