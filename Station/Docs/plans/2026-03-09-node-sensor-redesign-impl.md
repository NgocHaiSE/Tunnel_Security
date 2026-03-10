# Tunnel Node/Sensor Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Restructure the app so each node (nút) holds exactly 7 devices (radar, camera, hồng ngoại, nhiệt độ, độ ẩm, ánh sáng, gia tốc) across 3 lines × 4 nodes = 12 nodes, all wired through MockDataService.

**Architecture:** Add `TunnelLine`/`TunnelNode` models in `Station.Models`. `MockDataService` builds 72 `SimulatedSensor` + 12 `SimulatedCamera` grouped under those models, and exposes a `Lines` property. All ViewModels consume from `MockDataService.Lines` instead of hardcoded data.

**Tech Stack:** C# 12, WinUI 3 (.NET 8), CommunityToolkit.Mvvm, LiveChartsCore

---

### Task 1: Update AlertCategory enum

**Files:**
- Modify: `Station/Models/Alert.cs:25-33`

**Step 1: Edit enum**

Replace the `AlertCategory` enum body:

```csharp
public enum AlertCategory
{
    Temperature,     // Nhiệt độ
    Humidity,        // Độ ẩm
    Radar,           // Radar phát hiện người
    Infrared,        // Cảm biến hồng ngoại (PIR)
    Light,           // Cảm biến ánh sáng
    Accelerometer,   // Cảm biến gia tốc
    Intrusion,       // Xâm nhập (camera AI)
    Equipment,       // Thiết bị
    Connection,      // Kết nối
    Other            // Khác
}
```

**Step 2: Verify build — expect errors in files that used Gas/WaterLevel/Motion**

```
dotnet build "Station/Station.csproj"
```

Expected: compile errors in `MockDataService.cs`, `AlertsViewModel.cs`, `SecurityMapNode.cs`. That is correct — those are handled in later tasks.

**Step 3: Commit**

```bash
git add "Station/Models/Alert.cs"
git commit -m "refactor(models): replace Gas/WaterLevel/Motion categories with Radar/Infrared/Light/Accelerometer"
```

---

### Task 2: Create TunnelLine model

**Files:**
- Create: `Station/Models/TunnelLine.cs`

**Step 1: Create file**

```csharp
using System.Collections.Generic;

namespace Station.Models
{
    public class TunnelLine
    {
        public string LineId { get; set; } = string.Empty;
        public string LineName { get; set; } = string.Empty;
        public List<TunnelNode> Nodes { get; set; } = new();
    }
}
```

**Step 2: Build (partial — errors still exist in other files)**

```
dotnet build "Station/Station.csproj" 2>&1 | findstr /i "TunnelLine"
```

Expected: no errors mentioning TunnelLine.

**Step 3: Commit**

```bash
git add "Station/Models/TunnelLine.cs"
git commit -m "feat(models): add TunnelLine model"
```

---

### Task 3: Create TunnelNode model

**Files:**
- Create: `Station/Models/TunnelNode.cs`

**Step 1: Create file**

```csharp
namespace Station.Models
{
    public class TunnelNode
    {
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string LineId { get; set; } = string.Empty;
        public string LineName { get; set; } = string.Empty;
    }
}
```

**Step 2: Commit**

```bash
git add "Station/Models/TunnelNode.cs"
git commit -m "feat(models): add TunnelNode model"
```

---

### Task 4: Update SecurityMapNode sensor fields

**Files:**
- Modify: `Station/Models/SecurityMapNode.cs:31-38`

**Step 1: Replace old sensor reading fields**

Remove:
```csharp
public double? RadarValue { get; set; }         // mm/m
public double? VibrationValue { get; set; }     // mm/s
public double? SmokeFireValue { get; set; }     // L or %
```

Add in their place:
```csharp
public bool?   RadarDetected { get; set; }          // phát hiện người
public bool?   InfraredDetected { get; set; }       // PIR kích hoạt
public double? LightValue { get; set; }             // lux
public double? AccelerometerValue { get; set; }     // m/s²
```

**Step 2: Update UpdateStatus() threshold logic (line 76-83)**

Replace:
```csharp
if (RadarValue > 3.0 || VibrationValue > 4.0 || SmokeFireValue > 50 || TemperatureValue > 45)
{
    Status = NodeStatus.Critical;
    return;
}

if (RadarValue > 2.0 || VibrationValue > 3.0 || SmokeFireValue > 30 || TemperatureValue > 35)
{
    Status = NodeStatus.Warning;
    return;
}
```

With:
```csharp
if ((RadarDetected == true) || (InfraredDetected == true) ||
    AccelerometerValue > 5.0 || TemperatureValue > 45)
{
    Status = NodeStatus.Critical;
    return;
}

if (AccelerometerValue > 3.0 || TemperatureValue > 35 || LightValue > 900)
{
    Status = NodeStatus.Warning;
    return;
}
```

**Step 3: Commit**

```bash
git add "Station/Models/SecurityMapNode.cs"
git commit -m "refactor(models): update SecurityMapNode sensor fields for new sensor types"
```

---

### Task 5: Restructure MockDataService

**Files:**
- Modify: `Station/Services/MockDataService.cs`

This is the largest task. Work section by section.

#### 5a — Add LineId/LineName to SimulatedSensor and SimulatedCamera

In `SimulatedSensor` class (around line 34), add two properties after `NodeName`:
```csharp
public string LineId { get; set; } = string.Empty;
public string LineName { get; set; } = string.Empty;
```

In `SimulatedCamera` class (around line 86), add after `NodeName`:
```csharp
public string LineId { get; set; } = string.Empty;
public string LineName { get; set; } = string.Empty;
```

#### 5b — Add Lines property and BuildLines to MockDataService

After `public IReadOnlyList<SimulatedCamera> Cameras { get; }` (line 113), add:
```csharp
public IReadOnlyList<TunnelLine> Lines { get; }
```

In the constructor, after `Cameras = BuildCameras();`, add:
```csharp
Lines = BuildLines();
```

Add the `BuildLines` method before `BuildSensors`:
```csharp
private IReadOnlyList<TunnelLine> BuildLines()
{
    var lineIds = Sensors.Select(s => s.LineId).Distinct().ToList();
    return lineIds.Select(lid =>
    {
        var firstSensor = Sensors.First(s => s.LineId == lid);
        var nodeIds = Sensors
            .Where(s => s.LineId == lid)
            .Select(s => s.NodeId)
            .Distinct()
            .ToList();
        return new TunnelLine
        {
            LineId = lid,
            LineName = firstSensor.LineName,
            Nodes = nodeIds.Select(nid =>
            {
                var ns = Sensors.First(s => s.NodeId == nid);
                return new TunnelNode
                {
                    NodeId = nid,
                    NodeName = ns.NodeName,
                    LineId = lid,
                    LineName = firstSensor.LineName
                };
            }).ToList()
        };
    }).ToList();
}
```

Add using at the top of the file if not present:
```csharp
using Station.Models;
```

#### 5c — Replace BuildSensors with new 3×4×6 structure

Replace the entire `BuildSensors()` method body (lines 417–618) with:

```csharp
private List<SimulatedSensor> BuildSensors()
{
    var sensors = new List<SimulatedSensor>();
    var lines = new[]
    {
        ("LINE-01", "Tuyến Bắc"),
        ("LINE-02", "Tuyến Trung"),
        ("LINE-03", "Tuyến Nam"),
    };

    for (int li = 0; li < lines.Length; li++)
    {
        var (lineId, lineName) = lines[li];
        for (int ni = 1; ni <= 4; ni++)
        {
            string nodeId   = $"NODE-L{li + 1}-{ni:D2}";
            string nodeName = $"Nút {li + 1}-{ni:D2}";
            string loc      = $"{lineName} - Nút {ni:D2}";

            sensors.Add(new SimulatedSensor
            {
                SensorId = $"RAD-L{li+1}-N{ni:D2}", SensorName = $"Radar phát hiện người {nodeName}",
                Category = AlertCategory.Radar,
                LineId = lineId, LineName = lineName, NodeId = nodeId, NodeName = nodeName,
                Location = loc, Unit = "%",
                NominalValue = 5, CurrentValue = 5,
                MinNormal = 0, MaxNormal = 50,
                WarnThreshold = 60, CriticalThreshold = 85,
                AbsoluteMin = 0, AbsoluteMax = 100, DriftSpeed = 2.0
            });
            sensors.Add(new SimulatedSensor
            {
                SensorId = $"PIR-L{li+1}-N{ni:D2}", SensorName = $"Cảm biến hồng ngoại {nodeName}",
                Category = AlertCategory.Infrared,
                LineId = lineId, LineName = lineName, NodeId = nodeId, NodeName = nodeName,
                Location = loc, Unit = "%",
                NominalValue = 5, CurrentValue = 5,
                MinNormal = 0, MaxNormal = 50,
                WarnThreshold = 60, CriticalThreshold = 85,
                AbsoluteMin = 0, AbsoluteMax = 100, DriftSpeed = 3.0
            });
            sensors.Add(new SimulatedSensor
            {
                SensorId = $"TMP-L{li+1}-N{ni:D2}", SensorName = $"Cảm biến nhiệt độ {nodeName}",
                Category = AlertCategory.Temperature,
                LineId = lineId, LineName = lineName, NodeId = nodeId, NodeName = nodeName,
                Location = loc, Unit = "°C",
                NominalValue = 24 + li, CurrentValue = 24 + li,
                MinNormal = 18, MaxNormal = 32,
                WarnThreshold = 38, CriticalThreshold = 50,
                AbsoluteMin = -5, AbsoluteMax = 80, DriftSpeed = 0.4
            });
            sensors.Add(new SimulatedSensor
            {
                SensorId = $"HUM-L{li+1}-N{ni:D2}", SensorName = $"Cảm biến độ ẩm {nodeName}",
                Category = AlertCategory.Humidity,
                LineId = lineId, LineName = lineName, NodeId = nodeId, NodeName = nodeName,
                Location = loc, Unit = "%RH",
                NominalValue = 55, CurrentValue = 55,
                MinNormal = 40, MaxNormal = 70,
                WarnThreshold = 80, CriticalThreshold = 90,
                AbsoluteMin = 10, AbsoluteMax = 99, DriftSpeed = 0.8
            });
            sensors.Add(new SimulatedSensor
            {
                SensorId = $"LUX-L{li+1}-N{ni:D2}", SensorName = $"Cảm biến ánh sáng {nodeName}",
                Category = AlertCategory.Light,
                LineId = lineId, LineName = lineName, NodeId = nodeId, NodeName = nodeName,
                Location = loc, Unit = "lux",
                NominalValue = 150, CurrentValue = 150,
                MinNormal = 50, MaxNormal = 500,
                WarnThreshold = 600, CriticalThreshold = 900,
                AbsoluteMin = 0, AbsoluteMax = 1200, DriftSpeed = 10.0
            });
            sensors.Add(new SimulatedSensor
            {
                SensorId = $"ACC-L{li+1}-N{ni:D2}", SensorName = $"Cảm biến gia tốc {nodeName}",
                Category = AlertCategory.Accelerometer,
                LineId = lineId, LineName = lineName, NodeId = nodeId, NodeName = nodeName,
                Location = loc, Unit = "m/s²",
                NominalValue = 0.5, CurrentValue = 0.5,
                MinNormal = 0, MaxNormal = 2.0,
                WarnThreshold = 3.0, CriticalThreshold = 5.0,
                AbsoluteMin = 0, AbsoluteMax = 20, DriftSpeed = 0.1
            });
        }
    }
    return sensors;
}
```

#### 5d — Replace BuildCameras

Replace entire `BuildCameras()` method with:

```csharp
private List<SimulatedCamera> BuildCameras()
{
    var cams = new List<SimulatedCamera>();
    var lines = new[]
    {
        ("LINE-01", "Tuyến Bắc"),
        ("LINE-02", "Tuyến Trung"),
        ("LINE-03", "Tuyến Nam"),
    };

    for (int li = 0; li < lines.Length; li++)
    {
        var (lineId, lineName) = lines[li];
        for (int ni = 1; ni <= 4; ni++)
        {
            cams.Add(new SimulatedCamera
            {
                CameraId   = $"CAM-L{li+1}-N{ni:D2}",
                CameraName = $"Camera Nút {li+1}-{ni:D2}",
                Location   = $"{lineName} - Nút {ni:D2}",
                LineId     = lineId,
                LineName   = lineName,
                NodeId     = $"NODE-L{li + 1}-{ni:D2}",
                NodeName   = $"Nút {li + 1}-{ni:D2}",
                IsOnline   = true
            });
        }
    }
    return cams;
}
```

#### 5e — Update alert text templates

Replace the `GetSensorAlertText` switch body with:

```csharp
return s.Category switch
{
    AlertCategory.Temperature => (
        $"Nhiệt độ {level} tại {s.NodeName}",
        $"Cảm biến {s.SensorName} ghi nhận {s.CurrentValue:F1}{s.Unit} — vượt ngưỡng {(s.CurrentAlertSeverity == AlertSeverity.Critical ? s.CriticalThreshold : s.WarnThreshold)}{s.Unit}. Kiểm tra thông gió ngay."),
    AlertCategory.Humidity => (
        $"Độ ẩm {level} tại {s.NodeName}",
        $"Cảm biến {s.SensorName} ghi nhận {s.CurrentValue:F1}{s.Unit}. Nguy cơ ăn mòn thiết bị điện. Kích hoạt hệ thống hút ẩm."),
    AlertCategory.Radar => (
        $"Radar phát hiện người tại {s.NodeName}",
        $"Radar {s.SensorName} ghi nhận xác suất hiện diện {s.CurrentValue:F0}{s.Unit}. {(s.CurrentAlertSeverity == AlertSeverity.Critical ? "KHẨN CẤP: Xác nhận xâm nhập trái phép, điều phối bảo vệ ngay!" : "Cần xác minh hiện diện bất thường trong khu vực.")}"),
    AlertCategory.Infrared => (
        $"Cảm biến hồng ngoại kích hoạt tại {s.NodeName}",
        $"Cảm biến PIR {s.SensorName} phát hiện chuyển động nhiệt. Kết hợp với camera để xác nhận."),
    AlertCategory.Light => (
        $"Ánh sáng bất thường tại {s.NodeName}",
        $"Cảm biến ánh sáng ghi nhận {s.CurrentValue:F0}{s.Unit}. Kiểm tra nguồn sáng lạ hoặc hệ thống chiếu sáng."),
    AlertCategory.Accelerometer => (
        $"Rung động {level} tại {s.NodeName}",
        $"Gia tốc kế {s.SensorName} ghi nhận {s.CurrentValue:F2}{s.Unit}. {(s.CurrentAlertSeverity == AlertSeverity.Critical ? "NGUY HIỂM: Kiểm tra kết cấu cống ngay lập tức!" : "Theo dõi kết cấu cống và thiết bị cơ khí.")}"),
    _ => (
        $"Cảnh báo cảm biến tại {s.NodeName}",
        $"Cảm biến {s.SensorName} ghi nhận giá trị bất thường: {s.CurrentValue:F2}{s.Unit}.")
};
```

Also update `BuildSensorAlert` — replace hardcoded `LineId`/`LineName` with sensor's own:
```csharp
LineId   = sensor.LineId,
LineName = sensor.LineName,
```

And `BuildCameraAlert`:
```csharp
LineId   = cam.LineId,
LineName = cam.LineName,
```

**Step: Build to verify**
```
dotnet build "Station/Station.csproj"
```
Expected: only errors remain in AlertsViewModel.cs and MonitoringDashboardViewModel.cs (handled next).

**Step: Commit**
```bash
git add "Station/Services/MockDataService.cs"
git commit -m "feat(mock): restructure to 3 lines x 4 nodes x 6 sensors + 1 camera per node"
```

---

### Task 6: Update DevicesViewModel

**Files:**
- Modify: `Station/ViewModels/DevicesViewModel.cs`

**Step 1: Replace `LoadMockData()` to pull from MockDataService**

Replace the entire `LoadMockData()` method with:

```csharp
private void LoadMockData()
{
    StatusFilters.Add("Tất cả trạng thái");
    StatusFilters.Add("Hoạt động");
    StatusFilters.Add("Ngoại tuyến");
    StatusFilters.Add("Lỗi");
    StatusFilters.Add("Tắt");

    LineFilters.Add("Tất cả tuyến");
    var mock = Station.Services.MockDataService.Instance;
    foreach (var line in mock.Lines)
        LineFilters.Add(line.LineName);

    AllDevices.Clear();

    // Add cameras
    foreach (var cam in mock.Cameras)
    {
        AllDevices.Add(new DeviceItemViewModel
        {
            Name            = cam.CameraName,
            DeviceId        = cam.CameraId,
            Type            = "Camera",
            TypeDisplay     = "Camera giám sát",
            Location        = $"{cam.LineName} / {cam.NodeName}",
            IpAddress       = string.Empty,
            Status          = cam.IsOnline ? DeviceStatus.Online : DeviceStatus.Offline,
            LastOnline      = DateTimeOffset.Now.AddMinutes(-1),
            Manufacturer    = "Hikvision",
            FirmwareVersion = "V5.7.3",
            AlertCount      = 0
        });
    }

    // Add sensors
    foreach (var s in mock.Sensors)
    {
        AllDevices.Add(new DeviceItemViewModel
        {
            Name            = s.SensorName,
            DeviceId        = s.SensorId,
            Type            = "Sensor",
            TypeDisplay     = CategoryToDisplay(s.Category),
            Location        = $"{s.LineName} / {s.NodeName}",
            IpAddress       = string.Empty,
            Status          = s.IsOnline ? DeviceStatus.Online : DeviceStatus.Offline,
            LastOnline      = DateTimeOffset.Now.AddSeconds(-5),
            Manufacturer    = "Bosch",
            FirmwareVersion = "V3.1.0",
            AlertCount      = 0
        });
    }

    TotalDevices = AllDevices.Count;
}

private static string CategoryToDisplay(Station.Models.AlertCategory cat) => cat switch
{
    Station.Models.AlertCategory.Radar         => "Radar phát hiện người",
    Station.Models.AlertCategory.Infrared      => "Cảm biến hồng ngoại",
    Station.Models.AlertCategory.Temperature   => "Cảm biến nhiệt độ",
    Station.Models.AlertCategory.Humidity      => "Cảm biến độ ẩm",
    Station.Models.AlertCategory.Light         => "Cảm biến ánh sáng",
    Station.Models.AlertCategory.Accelerometer => "Cảm biến gia tốc",
    _                                          => "Cảm biến"
};
```

**Step 2: Update ApplyFilters — fix line filter and rebuild NodeItemViewModel sensors**

In `ApplyFilters`, replace the line-filter predicate:
```csharp
// Old:
filtered = filtered.Where(d => d.Location.StartsWith(SelectedLine.Replace("Tuyến ", "")));
// New:
filtered = filtered.Where(d => d.Location.StartsWith(SelectedLine));
```

Replace the node-building block (the large section that manually adds 7 SensorItemViewModels) with this simpler version that reads from MockDataService:

```csharp
var mock = Station.Services.MockDataService.Instance;

var nodeGroups = filtered.GroupBy(d =>
{
    var parts = d.Location.Split('/');
    return parts.Length >= 2
        ? $"{parts[0].Trim()} / {parts[1].Trim()}"
        : d.Location;
});

foreach (var group in nodeGroups)
{
    var items     = group.ToList();
    var first     = items.First();
    var locParts  = first.Location.Split('/');
    string lineName = locParts.Length >= 1 ? locParts[0].Trim() : "?";
    string nodeName = locParts.Length >= 2 ? locParts[1].Trim() : "?";

    var line = mock.Lines.FirstOrDefault(l => l.LineName == lineName);
    var node = line?.Nodes.FirstOrDefault(n => n.NodeName == nodeName);

    var nodeVm = new NodeItemViewModel
    {
        NodeName = nodeName,
        LineName = lineName,
        Location = $"{lineName} / {nodeName}",
        Status   = items.Any(d => d.Status == DeviceStatus.Fault)    ? DeviceStatus.Fault    :
                   items.Any(d => d.Status == DeviceStatus.Offline)  ? DeviceStatus.Offline  :
                   DeviceStatus.Online
    };

    if (node != null)
    {
        var nodeSensors = mock.Sensors.Where(s => s.NodeId == node.NodeId).ToList();
        var nodeCam     = mock.Cameras.FirstOrDefault(c => c.NodeId == node.NodeId);

        // Camera
        if (nodeCam != null)
            nodeVm.Sensors.Add(new SensorItemViewModel
            {
                SensorId       = nodeCam.CameraId,
                SensorName     = nodeCam.CameraName,
                SensorType     = "Camera",
                CurrentValue   = nodeCam.IsOnline ? "Online" : "Offline",
                Unit           = string.Empty,
                LastUpdateText = "Vừa xong",
                SensorStatus   = nodeCam.IsOnline ? DeviceStatus.Online : DeviceStatus.Offline,
                TypeIcon       = "\uE714",
                LineName       = lineName,
                NodeName       = nodeName,
                Location       = $"{lineName} / {nodeName}"
            });

        // Sensors
        foreach (var s in nodeSensors)
        {
            nodeVm.Sensors.Add(new SensorItemViewModel
            {
                SensorId       = s.SensorId,
                SensorName     = s.SensorName,
                SensorType     = s.Category.ToString(),
                CurrentValue   = FormatSensorValue(s),
                Unit           = s.Unit,
                LastUpdateText = "Vừa xong",
                SensorStatus   = s.IsOnline ? DeviceStatus.Online : DeviceStatus.Offline,
                TypeIcon       = CategoryIcon(s.Category),
                LineName       = lineName,
                NodeName       = nodeName,
                Location       = $"{lineName} / {nodeName}"
            });
        }
    }

    FilteredNodes.Add(nodeVm);
}
```

Add two helper methods after `CategoryToDisplay`:

```csharp
private static string FormatSensorValue(Station.Services.SimulatedSensor s) =>
    s.Category switch
    {
        Station.Models.AlertCategory.Radar         => $"{s.CurrentValue:F0}%",
        Station.Models.AlertCategory.Infrared      => $"{s.CurrentValue:F0}%",
        Station.Models.AlertCategory.Temperature   => $"{s.CurrentValue:F1}°C",
        Station.Models.AlertCategory.Humidity      => $"{s.CurrentValue:F1}%RH",
        Station.Models.AlertCategory.Light         => $"{s.CurrentValue:F0} lux",
        Station.Models.AlertCategory.Accelerometer => $"{s.CurrentValue:F2} m/s²",
        _ => $"{s.CurrentValue:F2}"
    };

private static string CategoryIcon(Station.Models.AlertCategory cat) => cat switch
{
    Station.Models.AlertCategory.Radar         => "\uE701",
    Station.Models.AlertCategory.Infrared      => "\uE7C1",
    Station.Models.AlertCategory.Temperature   => "\uE9CA",
    Station.Models.AlertCategory.Humidity      => "\uE81E",
    Station.Models.AlertCategory.Light         => "\uE706",
    Station.Models.AlertCategory.Accelerometer => "\uEDA4",
    _                                          => "\uE957"
};
```

**Step 3: Build**

```
dotnet build "Station/Station.csproj"
```

Expected: DevicesViewModel compiles cleanly.

**Step 4: Commit**

```bash
git add "Station/ViewModels/DevicesViewModel.cs"
git commit -m "feat(devices): load nodes/sensors from MockDataService.Lines"
```

---

### Task 7: Update MonitoringDashboardViewModel

**Files:**
- Modify: `Station/ViewModels/MonitoringDashboardViewModel.cs`

**Step 1: Update LoadFromMockData — derive nodes from Lines**

Replace the node-counting block (lines 176–179):
```csharp
// Old:
var nodeIds = sensors.Select(s => s.NodeId).Distinct().ToList();
TotalNodes  = nodeIds.Count;
ActiveNodes = nodeIds.Count(nid => sensors.Any(s => s.NodeId == nid && s.IsOnline));
OfflineNodes = nodeIds.Count(nid => sensors.All(s => s.NodeId == nid && !s.IsOnline));

// New:
var allNodes = _mock.Lines.SelectMany(l => l.Nodes).ToList();
TotalNodes   = allNodes.Count;
ActiveNodes  = allNodes.Count(n => sensors.Any(s => s.NodeId == n.NodeId && s.IsOnline));
OfflineNodes = allNodes.Count(n => sensors.All(s => s.NodeId == n.NodeId && !s.IsOnline));
```

**Step 2: Update average sensor stats labels**

Replace gas/water stat variables with new category queries.

In `LoadFromMockData`, replace the vibration/gas blocks:
```csharp
// Remove AverageVibration, AverageGasLevel blocks entirely.
// Add:
var radSensors = sensors.Where(s => s.Category == AlertCategory.Radar).ToList();
var accSensors = sensors.Where(s => s.Category == AlertCategory.Accelerometer).ToList();
```

Update stat text properties (rename gas → radar, vibration → accelerometer) keeping the same observable property names to avoid XAML binding changes, just updating the values and labels:
```csharp
AverageVibration = accSensors.Count > 0
    ? Math.Round(accSensors.Average(s => s.CurrentValue), 2) : 0;
AverageVibrationText = $"{AverageVibration:F2} m/s²";

AverageGasLevel = radSensors.Count > 0
    ? Math.Round(radSensors.Average(s => s.CurrentValue), 1) : 0;
AverageGasLevelText = $"{AverageGasLevel:F0}%";
```

**Step 3: Update OnSensorTick switch**

Replace `case AlertCategory.Motion:`, `case AlertCategory.WaterLevel:`, `case AlertCategory.Gas:` branches with new ones:

```csharp
case AlertCategory.Accelerometer:
    UpdateMiniChart(VibrationSeries, e.NewValue);
    var accSensors = _mock.Sensors.Where(s => s.Category == AlertCategory.Accelerometer).ToList();
    if (accSensors.Count > 0)
    {
        AverageVibration     = Math.Round(accSensors.Average(s => s.CurrentValue), 2);
        AverageVibrationText = $"{AverageVibration:F2} m/s²";
    }
    break;

case AlertCategory.Light:
    UpdateMiniChart(LightSeries, e.NewValue);
    break;

case AlertCategory.Radar:
case AlertCategory.Infrared:
    UpdateMiniChart(WaterLevelSeries, e.NewValue);
    var radSensors2 = _mock.Sensors.Where(s => s.Category == AlertCategory.Radar).ToList();
    if (radSensors2.Count > 0)
    {
        AverageGasLevel     = Math.Round(radSensors2.Average(s => s.CurrentValue), 1);
        AverageGasLevelText = $"{AverageGasLevel:F0}%";
    }
    break;
```

Remove old `case AlertCategory.Motion:`, `case AlertCategory.WaterLevel:`, `case AlertCategory.Gas:`.

**Step 4: Build**

```
dotnet build "Station/Station.csproj"
```

Expected: MonitoringDashboardViewModel compiles cleanly.

**Step 5: Commit**

```bash
git add "Station/ViewModels/MonitoringDashboardViewModel.cs"
git commit -m "feat(dashboard): update stats and sensor tick handlers for new sensor types"
```

---

### Task 8: Update AlertsViewModel

**Files:**
- Modify: `Station/ViewModels/AlertsViewModel.cs`

**Step 1: Update Categories list (around line 218)**

Replace:
```csharp
Categories.Add("Mực nước");
Categories.Add("Khí gas");
// ...
Categories.Add("Chuyển động");
```

With:
```csharp
Categories.Add("Radar");
Categories.Add("Hồng ngoại");
// keep: Nhiệt độ, Độ ẩm
Categories.Add("Ánh sáng");
Categories.Add("Gia tốc");
```

Full new Categories block (replace whole section):
```csharp
Categories.Add("Tất cả loại");
Categories.Add("Radar");
Categories.Add("Hồng ngoại");
Categories.Add("Nhiệt độ");
Categories.Add("Độ ẩm");
Categories.Add("Ánh sáng");
Categories.Add("Gia tốc");
Categories.Add("Xâm nhập");
Categories.Add("Thiết bị");
Categories.Add("Kết nối");
```

**Step 2: Update filter switch (around line 528)**

Replace old category filter cases:
```csharp
"Mực nước"    => filtered.Where(a => a.Category == AlertCategory.WaterLevel),
"Khí gas"     => filtered.Where(a => a.Category == AlertCategory.Gas),
// ...
"Chuyển động" => filtered.Where(a => a.Category == AlertCategory.Motion),
```

With:
```csharp
"Radar"       => filtered.Where(a => a.Category == AlertCategory.Radar),
"Hồng ngoại"  => filtered.Where(a => a.Category == AlertCategory.Infrared),
"Nhiệt độ"    => filtered.Where(a => a.Category == AlertCategory.Temperature),
"Độ ẩm"       => filtered.Where(a => a.Category == AlertCategory.Humidity),
"Ánh sáng"    => filtered.Where(a => a.Category == AlertCategory.Light),
"Gia tốc"     => filtered.Where(a => a.Category == AlertCategory.Accelerometer),
"Xâm nhập"    => filtered.Where(a => a.Category == AlertCategory.Intrusion),
"Thiết bị"    => filtered.Where(a => a.Category == AlertCategory.Equipment),
"Kết nối"     => filtered.Where(a => a.Category == AlertCategory.Connection),
```

**Step 3: Update CategoryIcon helper (around line 954)**

Replace:
```csharp
AlertCategory.WaterLevel => "\uE81E",
AlertCategory.Gas        => "\uE9CA",
AlertCategory.Motion     => "\uE805",
```

With:
```csharp
AlertCategory.Radar         => "\uE701",
AlertCategory.Infrared      => "\uE7C1",
AlertCategory.Light         => "\uE706",
AlertCategory.Accelerometer => "\uEDA4",
```

**Step 4: Update CategoryName helper (around line 967)**

Replace:
```csharp
AlertCategory.WaterLevel => "Mực nước",
AlertCategory.Gas        => "Khí gas",
AlertCategory.Motion     => "Chuyển động",
```

With:
```csharp
AlertCategory.Radar         => "Radar",
AlertCategory.Infrared      => "Hồng ngoại",
AlertCategory.Light         => "Ánh sáng",
AlertCategory.Accelerometer => "Gia tốc",
```

**Step 5: Replace hardcoded mock alerts**

Find the mock alert array (starting around line 240) and replace the Alert objects that use `AlertCategory.Gas`, `AlertCategory.WaterLevel`, `AlertCategory.Motion` with alerts using the new categories. Example replacements:

```csharp
// Replace Gas alert:
new AlertItemViewModel
{
    Title       = "Radar phát hiện người tại Nút L1-01",
    Description = "Radar ghi nhận xác suất hiện diện 78%. Cần xác minh.",
    Category    = AlertCategory.Radar,
    Severity    = AlertSeverity.High,
    State       = AlertState.Unprocessed,
    LineId      = "LINE-01", LineName = "Tuyến Bắc",
    NodeId      = "NODE-L1-01", NodeName = "Nút 1-01",
    SensorId    = "RAD-L1-N01", SensorName = "Radar Nút 1-01",
    SensorType  = "Radar", SensorValue = 78, SensorUnit = "%", Threshold = 60,
    CreatedAt   = DateTimeOffset.Now.AddMinutes(-5)
},
// Replace WaterLevel alert:
new AlertItemViewModel
{
    Title       = "Rung động bất thường tại Nút L2-03",
    Description = "Gia tốc kế ghi nhận 3.8 m/s².",
    Category    = AlertCategory.Accelerometer,
    Severity    = AlertSeverity.High,
    State       = AlertState.Unprocessed,
    LineId      = "LINE-02", LineName = "Tuyến Trung",
    NodeId      = "NODE-L2-03", NodeName = "Nút 2-03",
    SensorId    = "ACC-L2-N03", SensorName = "Gia tốc kế Nút 2-03",
    SensorType  = "Accelerometer", SensorValue = 3.8, SensorUnit = "m/s²", Threshold = 3.0,
    CreatedAt   = DateTimeOffset.Now.AddMinutes(-12)
},
// Replace Motion alert:
new AlertItemViewModel
{
    Title       = "Cảm biến hồng ngoại kích hoạt tại Nút L3-02",
    Description = "PIR phát hiện chuyển động nhiệt trong khu vực hạn chế.",
    Category    = AlertCategory.Infrared,
    Severity    = AlertSeverity.Medium,
    State       = AlertState.Acknowledged,
    LineId      = "LINE-03", LineName = "Tuyến Nam",
    NodeId      = "NODE-L3-02", NodeName = "Nút 3-02",
    SensorId    = "PIR-L3-N02", SensorName = "Hồng ngoại Nút 3-02",
    SensorType  = "Infrared", SensorValue = 65, SensorUnit = "%", Threshold = 60,
    CreatedAt   = DateTimeOffset.Now.AddMinutes(-28)
},
```

**Step 6: Final build — expect zero errors**

```
dotnet build "Station/Station.csproj"
```

Expected: Build succeeded, 0 Error(s).

**Step 7: Final commit**

```bash
git add "Station/ViewModels/AlertsViewModel.cs"
git commit -m "feat(alerts): update categories and mock data for new sensor types"
```

---

### Task 9: Smoke test in app

**Step 1: Run the app**

Build and launch in Visual Studio or:
```
dotnet run --project "Station/Station.csproj"
```

**Verify checklist:**
- [ ] Trang Quản lý Thiết bị: hiện 3 tuyến trong dropdown filter
- [ ] Mỗi nút expand ra 7 thiết bị (1 camera + 6 sensor)
- [ ] Tên sensor đúng: Radar, Hồng ngoại, Nhiệt độ, Độ ẩm, Ánh sáng, Gia tốc
- [ ] Trang Monitoring Dashboard: TotalNodes = 12
- [ ] Trang Cảnh báo: dropdown loại hiện Radar/Hồng ngoại/Ánh sáng/Gia tốc
- [ ] Không có crash khi filter theo tuyến

**Step 2: Final commit (if any last-minute fixes)**

```bash
git add -p
git commit -m "fix: post-smoke-test corrections"
```
