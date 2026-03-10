# Design: Tunnel Node & Sensor Redesign

Date: 2026-03-09
Approach: B — TunnelLine + TunnelNode model

## Goal

Restructure the data model so that:
- A **line** (tuyến cống) contains multiple **nodes** (nút)
- Each **node** has exactly 7 devices: radar, camera, infrared (PIR), temperature, humidity, light, accelerometer
- Mock data: 3 lines × 4 nodes = 12 nodes, 72 sensors, 12 cameras
- All ViewModels load from MockDataService (no hardcoded data)

## Data Model

### New Models

**TunnelLine** (`Models/TunnelLine.cs`)
```
LineId     string   "LINE-01"
LineName   string   "Tuyến Bắc"
Nodes      List<TunnelNode>
```

**TunnelNode** (`Models/TunnelNode.cs`)
```
NodeId     string   "NODE-L1-01"
NodeName   string   "Nút L1-01"
LineId     string
LineName   string
Sensors    List<SimulatedSensor>   // 6 sensors
Camera     SimulatedCamera?        // 1 camera
```

### Modified: AlertCategory (Alert.cs)

Add: `Radar`, `Infrared`, `Light`, `Accelerometer`
Remove: `Gas`, `WaterLevel`, `Motion`
Keep: `Temperature`, `Humidity`, `Intrusion`, `Equipment`, `Connection`, `Other`

### Modified: SecurityMapNode.cs

Replace fields:
- Remove: `RadarValue`, `VibrationValue`, `SmokeFireValue`
- Add: `RadarDetected` (bool?), `InfraredDetected` (bool?), `LightValue` (lux), `AccelerometerValue` (m/s²)
- Keep: `TemperatureValue`, `HumidityValue`, `CameraId`

## MockDataService

### Lines layout

| Line | LineName | Nodes |
|---|---|---|
| LINE-01 | Tuyến Bắc | NODE-L1-01 … NODE-L1-04 |
| LINE-02 | Tuyến Trung | NODE-L2-01 … NODE-L2-04 |
| LINE-03 | Tuyến Nam | NODE-L3-01 … NODE-L3-04 |

### Sensors per node (6 SimulatedSensor)

| SensorId pattern | Category | Unit | Nominal | Warn | Critical |
|---|---|---|---|---|---|
| RAD-Lx-Ny-001 | Radar | % confidence | 5 | 60 | 85 |
| PIR-Lx-Ny-001 | Infrared | % | 5 | 60 | 85 |
| TMP-Lx-Ny-001 | Temperature | °C | 24 | 38 | 50 |
| HUM-Lx-Ny-001 | Humidity | %RH | 55 | 80 | 90 |
| LUX-Lx-Ny-001 | Light | lux | 150 | 600 | 900 |
| ACC-Lx-Ny-001 | Accelerometer | m/s² | 0.5 | 3.0 | 5.0 |

### Camera per node (1 SimulatedCamera)

Pattern: `CAM-Lx-Ny-001`, linked by `NodeId`.

### New public property

```csharp
public IReadOnlyList<TunnelLine> Lines { get; }
```

Sensors and Cameras remain as flat lists (backward compat for existing alert evaluation logic).

## ViewModels

### DevicesViewModel

- `LoadMockData()`: iterate `MockDataService.Lines` → populate `LineFilters`, `AllDevices` (camera as device + 6 sensors as device rows), `FilteredNodes`
- Remove hardcoded mock device array
- Line filter matches `LineName`

### MonitoringDashboardViewModel

- `LoadFromMockData()`: derive TotalNodes, ActiveNodes from `Lines`
- `OnSensorTick()`: add handlers for `Radar`, `Infrared`, `Light`, `Accelerometer` categories
- Update mini chart labels: replace "Gas"→"Radar", "WaterLevel"→"Gia tốc"
- Stats text updated (remove gas/water references)

### AlertsViewModel

- Update category filter display names to match new enum values

## Files Changed

| File | Action |
|---|---|
| `Models/Alert.cs` | Edit — AlertCategory enum |
| `Models/TunnelLine.cs` | Create |
| `Models/TunnelNode.cs` | Create |
| `Models/SecurityMapNode.cs` | Edit — sensor fields |
| `Services/MockDataService.cs` | Edit — BuildSensors, BuildCameras, expose Lines, alert text |
| `ViewModels/DevicesViewModel.cs` | Edit — LoadMockData |
| `ViewModels/MonitoringDashboardViewModel.cs` | Edit — stats, OnSensorTick |
| `ViewModels/AlertsViewModel.cs` | Edit — category labels |
