# Simulation Web Dashboard Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Expose MockDataService via an embedded HTTP server (HttpListener + built-in WebSocket) and a single-file `simulation.html` dashboard for controlling and observing the station simulation in real time.

**Architecture:** `SimulationApiServer` (new `Services/` class) runs `HttpListener` on `http://localhost:5050/`, serving REST endpoints and upgrading WebSocket connections. It bridges directly to `MockDataService.Instance`, subscribing to `SensorTick`/`AlertGenerated` events and broadcasting JSON to all WebSocket clients. No extra NuGet packages required — `System.Net.HttpListener` and `System.Net.WebSockets` are built into .NET 8.

**Tech Stack:** .NET 8, System.Net.HttpListener, System.Net.WebSockets, System.Text.Json, WinUI 3 (no changes to UI layer), vanilla HTML/JS/CSS (Tailwind CDN)

---

### Task 1: Add FireManualAlert + UpdateSensorParams to MockDataService

**Files:**
- Modify: `Station/Services/MockDataService.cs` (append to "Public helpers" region)

**Step 1: Add the two public methods after `ClearSensorFault`**

```csharp
/// Fire a manually-crafted alert (from web dashboard or tests)
public void FireManualAlert(
    string title,
    string description,
    AlertSeverity severity,
    AlertCategory category,
    string nodeId = "NODE-A1",
    string nodeName = "Nút A1")
{
    var alert = new Alert
    {
        Title = title,
        Description = description,
        Category = category,
        Severity = severity,
        State = AlertState.Unprocessed,
        LineId = "LINE-01",
        LineName = "Tuyến hầm chính",
        NodeId = nodeId,
        NodeName = nodeName,
        CreatedAt = DateTimeOffset.Now
    };
    RegisterAlert(alert);
    AlertGenerated?.Invoke(this, new AlertGeneratedEventArgs { Alert = alert });
}

/// Update simulation parameters for a sensor at runtime
public void UpdateSensorParams(string sensorId, double? nominalValue, double? driftSpeed,
    double? warnThreshold, double? criticalThreshold)
{
    var sensor = Sensors.FirstOrDefault(s => s.SensorId == sensorId);
    if (sensor == null) return;
    if (nominalValue.HasValue)    sensor.NominalValue    = nominalValue.Value;
    if (driftSpeed.HasValue)      sensor.DriftSpeed      = driftSpeed.Value;
    if (warnThreshold.HasValue)   sensor.WarnThreshold   = warnThreshold.Value;
    if (criticalThreshold.HasValue) sensor.CriticalThreshold = criticalThreshold.Value;
}
```

**Step 2: Verify it compiles**
Build the project (Ctrl+Shift+B). Expected: 0 errors.

**Step 3: Commit**
```bash
git add Station/Services/MockDataService.cs
git commit -m "feat(mock): add FireManualAlert and UpdateSensorParams helpers"
```

---

### Task 2: Create SimulationApiServer.cs

**Files:**
- Create: `Station/Services/SimulationApiServer.cs`

**Step 1: Create the file with full content**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Station.Models;

namespace Station.Services
{
    // DTO for POST /api/alerts/fire
    public class FireAlertDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string Category { get; set; } = "Other";
        public string NodeId { get; set; } = "NODE-A1";
        public string NodeName { get; set; } = "Nút A1";
    }

    // DTO for PUT /api/sensors/{id}/params
    public class SensorParamsDto
    {
        public double? NominalValue { get; set; }
        public double? DriftSpeed { get; set; }
        public double? WarnThreshold { get; set; }
        public double? CriticalThreshold { get; set; }
    }

    public sealed class SimulationApiServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly MockDataService _mock = MockDataService.Instance;
        private readonly List<WebSocket> _wsClients = new();
        private readonly SemaphoreSlim _wsLock = new(1, 1);
        private readonly CancellationTokenSource _cts = new();

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SimulationApiServer()
        {
            _listener.Prefixes.Add("http://localhost:5050/");
            _mock.SensorTick += OnSensorTick;
            _mock.AlertGenerated += OnAlertGenerated;
        }

        // ── Start / Stop ──────────────────────────────────────────────────
        public async Task StartAsync()
        {
            _listener.Start();
            System.Diagnostics.Debug.WriteLine("[SimAPI] Listening on http://localhost:5050/");
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    _ = HandleRequestAsync(ctx);
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _mock.SensorTick -= OnSensorTick;
            _mock.AlertGenerated -= OnAlertGenerated;
            try { _listener.Stop(); } catch { }
        }

        public void Dispose() => Stop();

        // ── Request dispatcher ────────────────────────────────────────────
        private async Task HandleRequestAsync(HttpListenerContext ctx)
        {
            ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
            ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");
            ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (ctx.Request.HttpMethod == "OPTIONS")
            {
                ctx.Response.StatusCode = 204;
                ctx.Response.Close();
                return;
            }

            if (ctx.Request.IsWebSocketRequest)
            {
                var wsCtx = await ctx.AcceptWebSocketAsync(null);
                await HandleWebSocketAsync(wsCtx.WebSocket);
                return;
            }

            var path   = ctx.Request.Url?.AbsolutePath.TrimEnd('/') ?? "/";
            var method = ctx.Request.HttpMethod;

            try { await RouteAsync(ctx, path, method); }
            catch (Exception ex)
            {
                try { await WriteJsonAsync(ctx.Response, 500, new { error = ex.Message }); }
                catch { }
            }
        }

        // ── REST router ───────────────────────────────────────────────────
        private async Task RouteAsync(HttpListenerContext ctx, string path, string method)
        {
            // GET /api/sensors
            if (method == "GET" && path == "/api/sensors")
            {
                var list = new List<object>();
                foreach (var s in _mock.Sensors)
                    list.Add(new {
                        s.SensorId, s.SensorName,
                        Category = s.Category.ToString(),
                        s.Location, s.NodeId, s.NodeName, s.Unit,
                        s.CurrentValue, s.NominalValue, s.DriftSpeed,
                        s.WarnThreshold, s.CriticalThreshold,
                        s.AbsoluteMin, s.AbsoluteMax,
                        s.IsOnline, s.IsInFaultMode,
                        Level = s.CurrentLevel.ToString(),
                        s.StatusText
                    });
                await WriteJsonAsync(ctx.Response, 200, list);
                return;
            }

            // GET /api/alerts
            if (method == "GET" && path == "/api/alerts")
            {
                var list = new List<object>();
                foreach (var a in _mock.AlertHistory)
                    list.Add(new {
                        a.Id, a.Title, a.Description,
                        Category = a.Category.ToString(),
                        Severity = a.Severity.ToString(),
                        State    = a.State.ToString(),
                        a.NodeId, a.NodeName,
                        a.SensorId, a.SensorName, a.SensorValue, a.SensorUnit,
                        a.CameraId,
                        CreatedAt = a.CreatedAt.ToString("o")
                    });
                await WriteJsonAsync(ctx.Response, 200, list);
                return;
            }

            // POST /api/simulation/start
            if (method == "POST" && path == "/api/simulation/start")
            {
                _mock.Start();
                await WriteJsonAsync(ctx.Response, 200, new { ok = true, status = "running" });
                return;
            }

            // POST /api/simulation/stop
            if (method == "POST" && path == "/api/simulation/stop")
            {
                _mock.Stop();
                await WriteJsonAsync(ctx.Response, 200, new { ok = true, status = "stopped" });
                return;
            }

            // POST /api/alerts/fire
            if (method == "POST" && path == "/api/alerts/fire")
            {
                var body = await ReadBodyAsync(ctx.Request);
                var dto  = JsonSerializer.Deserialize<FireAlertDto>(body, _jsonOpts);
                if (dto == null) { await WriteJsonAsync(ctx.Response, 400, new { error = "invalid body" }); return; }

                if (!Enum.TryParse<AlertSeverity>(dto.Severity, true, out var sev)) sev = AlertSeverity.Medium;
                if (!Enum.TryParse<AlertCategory>(dto.Category, true, out var cat)) cat = AlertCategory.Other;

                _mock.FireManualAlert(dto.Title, dto.Description, sev, cat, dto.NodeId, dto.NodeName);
                await WriteJsonAsync(ctx.Response, 200, new { ok = true });
                return;
            }

            // POST /api/sensors/{id}/fault
            if (method == "POST" && path.StartsWith("/api/sensors/") && path.EndsWith("/fault"))
            {
                var parts = path.Split('/');  // ["","api","sensors","{id}","fault"]
                if (parts.Length == 5)
                {
                    _mock.InjectSensorFault(parts[3]);
                    await WriteJsonAsync(ctx.Response, 200, new { ok = true });
                    return;
                }
            }

            // DELETE /api/sensors/{id}/fault
            if (method == "DELETE" && path.StartsWith("/api/sensors/") && path.EndsWith("/fault"))
            {
                var parts = path.Split('/');
                if (parts.Length == 5)
                {
                    _mock.ClearSensorFault(parts[3]);
                    await WriteJsonAsync(ctx.Response, 200, new { ok = true });
                    return;
                }
            }

            // PUT /api/sensors/{id}/params
            if (method == "PUT" && path.StartsWith("/api/sensors/") && path.EndsWith("/params"))
            {
                var parts = path.Split('/');
                if (parts.Length == 5)
                {
                    var body = await ReadBodyAsync(ctx.Request);
                    var dto  = JsonSerializer.Deserialize<SensorParamsDto>(body, _jsonOpts);
                    if (dto != null)
                        _mock.UpdateSensorParams(parts[3],
                            dto.NominalValue, dto.DriftSpeed,
                            dto.WarnThreshold, dto.CriticalThreshold);
                    await WriteJsonAsync(ctx.Response, 200, new { ok = true });
                    return;
                }
            }

            await WriteJsonAsync(ctx.Response, 404, new { error = "not found" });
        }

        // ── WebSocket ─────────────────────────────────────────────────────
        private async Task HandleWebSocketAsync(WebSocket ws)
        {
            await _wsLock.WaitAsync();
            _wsClients.Add(ws);
            _wsLock.Release();

            System.Diagnostics.Debug.WriteLine($"[SimAPI] WS client connected. Total: {_wsClients.Count}");

            // Keep alive — read (and discard) any pings until close
            var buf = new byte[256];
            try
            {
                while (ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
            }
            catch { }
            finally
            {
                await _wsLock.WaitAsync();
                _wsClients.Remove(ws);
                _wsLock.Release();
                ws.Dispose();
                System.Diagnostics.Debug.WriteLine($"[SimAPI] WS client disconnected. Total: {_wsClients.Count}");
            }
        }

        private async void OnSensorTick(object? sender, SensorTickEventArgs e)
        {
            var msg = JsonSerializer.Serialize(new
            {
                type     = "sensorTick",
                sensorId = e.Sensor.SensorId,
                name     = e.Sensor.SensorName,
                value    = Math.Round(e.NewValue, 2),
                unit     = e.Sensor.Unit,
                level    = e.Sensor.CurrentLevel.ToString(),
                nodeId   = e.Sensor.NodeId,
                nodeName = e.Sensor.NodeName,
                isAnomaly = e.IsAnomaly,
                ts       = e.Timestamp.ToString("o")
            }, _jsonOpts);
            await BroadcastAsync(msg);
        }

        private async void OnAlertGenerated(object? sender, AlertGeneratedEventArgs e)
        {
            var a = e.Alert;
            var msg = JsonSerializer.Serialize(new
            {
                type      = "alertGenerated",
                id        = a.Id,
                title     = a.Title,
                description = a.Description,
                category  = a.Category.ToString(),
                severity  = a.Severity.ToString(),
                nodeId    = a.NodeId,
                nodeName  = a.NodeName,
                sensorId  = a.SensorId,
                sensorValue = a.SensorValue,
                sensorUnit  = a.SensorUnit,
                cameraId  = a.CameraId,
                ts        = a.CreatedAt.ToString("o")
            }, _jsonOpts);
            await BroadcastAsync(msg);
        }

        private async Task BroadcastAsync(string json)
        {
            var bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
            await _wsLock.WaitAsync();
            var snapshot = new List<WebSocket>(_wsClients);
            _wsLock.Release();

            foreach (var ws in snapshot)
            {
                try
                {
                    if (ws.State == WebSocketState.Open)
                        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch { }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static async Task WriteJsonAsync(HttpListenerResponse res, int status, object payload)
        {
            var json  = JsonSerializer.Serialize(payload, _jsonOpts);
            var bytes = Encoding.UTF8.GetBytes(json);
            res.StatusCode   = status;
            res.ContentType  = "application/json; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
            res.OutputStream.Close();
        }

        private static async Task<string> ReadBodyAsync(HttpListenerRequest req)
        {
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}
```

**Step 2: Build — verify 0 errors**

**Step 3: Commit**
```bash
git add Station/Services/SimulationApiServer.cs
git commit -m "feat(sim-api): add SimulationApiServer with REST + WebSocket"
```

---

### Task 3: Wire SimulationApiServer into App.xaml.cs

**Files:**
- Modify: `Station/App.xaml.cs`

**Step 1: Replace App.xaml.cs with wired-up version**

```csharp
using Microsoft.UI.Xaml;
using Station.Services;
using System.Threading.Tasks;

namespace Station
{
    public partial class App : Application
    {
        public Window? m_window { get; private set; }
        private SimulationApiServer? _simServer;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Start mock data simulation
            MockDataService.Instance.Start();

            // Start web API server on background thread
            _simServer = new SimulationApiServer();
            Task.Run(() => _simServer.StartAsync());

            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}
```

**Step 2: Build — verify 0 errors**

**Step 3: Run app — check Debug Output for:**
```
[SimAPI] Listening on http://localhost:5050/
```

**Step 4: Quick smoke test in browser — open:**
```
http://localhost:5050/api/sensors
```
Expected: JSON array of 16 sensor objects.

**Step 5: Commit**
```bash
git add Station/App.xaml.cs
git commit -m "feat(app): start SimulationApiServer and MockDataService on launch"
```

---

### Task 4: Create simulation.html

**Files:**
- Create: `simulation.html` (at repo root, alongside `center.html`)

**Step 1: Create the file**

Full content — paste exactly as below:

```html
<!DOCTYPE html>
<html lang="vi">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0"/>
<title>Station Simulation Control</title>
<script src="https://cdn.tailwindcss.com"></script>
<style>
  body { background:#0f172a; color:#e2e8f0; font-family:'Segoe UI',sans-serif; }
  ::-webkit-scrollbar{width:6px} ::-webkit-scrollbar-track{background:#1e293b}
  ::-webkit-scrollbar-thumb{background:#475569;border-radius:3px}
  .sensor-card { transition: all .3s; }
  .sensor-card:hover { transform:translateY(-2px); box-shadow:0 8px 24px rgba(0,0,0,.4); }
  .level-Normal   { border-color:#22c55e; }
  .level-Warning  { border-color:#f59e0b; background:#1c1507; }
  .level-Critical { border-color:#ef4444; background:#1a0707; animation: pulse-red 1s infinite; }
  .level-Offline  { border-color:#6b7280; opacity:.6; }
  @keyframes pulse-red { 0%,100%{box-shadow:0 0 0 0 rgba(239,68,68,.4)} 50%{box-shadow:0 0 0 6px rgba(239,68,68,0)} }
  .alert-Critical { border-left-color:#ef4444; background:#1a0707; }
  .alert-High     { border-left-color:#f97316; background:#1c0e04; }
  .alert-Medium   { border-left-color:#f59e0b; background:#1c1507; }
  .alert-Low      { border-left-color:#22c55e; background:#071a0c; }
  .badge-Critical { background:#ef4444; }
  .badge-High     { background:#f97316; }
  .badge-Medium   { background:#f59e0b; }
  .badge-Low      { background:#22c55e; }
  .badge-Normal   { background:#22c55e; }
  .badge-Warning  { background:#f59e0b; }
  .badge-Offline  { background:#6b7280; }
  #inspector { transition: transform .3s; }
</style>
</head>
<body class="min-h-screen">

<!-- ═══ HEADER ══════════════════════════════════════════════════════════ -->
<header class="bg-slate-800 border-b border-slate-700 px-6 py-3 flex items-center justify-between sticky top-0 z-50">
  <div class="flex items-center gap-3">
    <div class="w-8 h-8 rounded-lg bg-blue-600 flex items-center justify-center text-white font-bold">S</div>
    <span class="font-bold text-lg">Station Simulation Control</span>
    <span class="text-slate-400 text-sm ml-2">localhost:5050</span>
  </div>
  <div class="flex items-center gap-4">
    <!-- WS status -->
    <div class="flex items-center gap-2 text-sm">
      <div id="wsIndicator" class="w-2.5 h-2.5 rounded-full bg-red-500"></div>
      <span id="wsStatus" class="text-slate-400">Disconnected</span>
    </div>
    <!-- Sim controls -->
    <button onclick="simControl('start')"
      class="px-4 py-1.5 rounded-lg bg-green-600 hover:bg-green-500 text-white text-sm font-medium transition">
      ▶ Start
    </button>
    <button onclick="simControl('stop')"
      class="px-4 py-1.5 rounded-lg bg-red-700 hover:bg-red-600 text-white text-sm font-medium transition">
      ■ Stop
    </button>
  </div>
</header>

<!-- ═══ MAIN LAYOUT ══════════════════════════════════════════════════════ -->
<div class="flex h-[calc(100vh-57px)]">

  <!-- LEFT: Sensor Grid + Manual Alert Form -->
  <div class="flex-1 overflow-y-auto p-5 space-y-5">

    <!-- Sensor Grid -->
    <section>
      <h2 class="text-sm font-semibold text-slate-400 uppercase tracking-wider mb-3">
        Live Sensors
        <span id="sensorOnlineCount" class="ml-2 text-slate-500"></span>
      </h2>
      <div id="sensorGrid" class="grid grid-cols-2 xl:grid-cols-4 gap-3">
        <div class="col-span-full text-slate-500 text-sm py-8 text-center">Đang kết nối...</div>
      </div>
    </section>

    <!-- Manual Alert Form -->
    <section class="bg-slate-800 border border-slate-700 rounded-xl p-5">
      <h2 class="text-sm font-semibold text-slate-400 uppercase tracking-wider mb-4">Phát Cảnh Báo Thủ Công</h2>
      <div class="grid grid-cols-2 gap-3">
        <div class="col-span-2">
          <label class="text-xs text-slate-400 mb-1 block">Tiêu đề *</label>
          <input id="alertTitle" type="text" placeholder="Ví dụ: Phát hiện khói bất thường tại cửa hầm"
            class="w-full bg-slate-900 border border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500"/>
        </div>
        <div class="col-span-2">
          <label class="text-xs text-slate-400 mb-1 block">Mô tả</label>
          <textarea id="alertDesc" rows="2" placeholder="Mô tả chi tiết..."
            class="w-full bg-slate-900 border border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500 resize-none"></textarea>
        </div>
        <div>
          <label class="text-xs text-slate-400 mb-1 block">Mức độ</label>
          <select id="alertSeverity" class="w-full bg-slate-900 border border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500">
            <option value="Low">Thấp (Low)</option>
            <option value="Medium" selected>Trung bình (Medium)</option>
            <option value="High">Cao (High)</option>
            <option value="Critical">Khẩn cấp (Critical)</option>
          </select>
        </div>
        <div>
          <label class="text-xs text-slate-400 mb-1 block">Loại</label>
          <select id="alertCategory" class="w-full bg-slate-900 border border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500">
            <option value="Temperature">Nhiệt độ</option>
            <option value="Humidity">Độ ẩm</option>
            <option value="Gas">Khí gas</option>
            <option value="WaterLevel">Mực nước</option>
            <option value="Motion">Chuyển động</option>
            <option value="Intrusion">Xâm nhập</option>
            <option value="Equipment">Thiết bị</option>
            <option value="Other" selected>Khác</option>
          </select>
        </div>
        <div>
          <label class="text-xs text-slate-400 mb-1 block">Node</label>
          <select id="alertNode" class="w-full bg-slate-900 border border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500">
            <option value="NODE-A1|Nút A1">Nút A1 (Cửa vào hầm)</option>
            <option value="NODE-B2|Nút B2">Nút B2 (Giữa hầm)</option>
            <option value="NODE-C3|Nút C3">Nút C3 (Phòng điều khiển)</option>
            <option value="NODE-D4|Nút D4">Nút D4 (Cửa ra hầm)</option>
          </select>
        </div>
        <div class="flex items-end">
          <button onclick="fireAlert()"
            class="w-full px-4 py-2 rounded-lg bg-orange-600 hover:bg-orange-500 text-white text-sm font-medium transition">
            🔔 Phát cảnh báo
          </button>
        </div>
      </div>
      <div id="alertFormMsg" class="mt-2 text-xs hidden"></div>
    </section>

  </div>

  <!-- RIGHT: Alert Feed -->
  <div class="w-96 border-l border-slate-700 flex flex-col bg-slate-900">
    <div class="px-4 py-3 border-b border-slate-700 flex items-center justify-between">
      <h2 class="text-sm font-semibold text-slate-400 uppercase tracking-wider">Alert Feed</h2>
      <div class="flex items-center gap-2">
        <span id="alertCount" class="text-xs bg-red-600 text-white px-2 py-0.5 rounded-full">0</span>
        <button onclick="clearAlertFeed()" class="text-xs text-slate-500 hover:text-slate-300">Xóa</button>
      </div>
    </div>
    <div id="alertFeed" class="flex-1 overflow-y-auto p-3 space-y-2">
      <div class="text-slate-600 text-xs text-center py-6">Chưa có cảnh báo</div>
    </div>
  </div>
</div>

<!-- ═══ SENSOR INSPECTOR DRAWER ═════════════════════════════════════════ -->
<div id="inspectorOverlay" class="fixed inset-0 bg-black/60 z-40 hidden" onclick="closeInspector()"></div>
<div id="inspector" class="fixed right-0 top-0 h-full w-[420px] bg-slate-900 border-l border-slate-700 z-50 transform translate-x-full overflow-y-auto">
  <div class="p-5">
    <div class="flex items-center justify-between mb-5">
      <h2 class="font-semibold text-lg" id="inspSensorName">Sensor</h2>
      <button onclick="closeInspector()" class="text-slate-400 hover:text-white text-xl">✕</button>
    </div>
    <!-- Status badge + value -->
    <div class="flex items-center gap-3 mb-5">
      <div class="text-3xl font-mono font-bold" id="inspValue">--</div>
      <div class="text-slate-400" id="inspUnit"></div>
      <span id="inspBadge" class="ml-auto px-3 py-1 rounded-full text-xs text-white font-medium"></span>
    </div>
    <!-- Info grid -->
    <div class="grid grid-cols-2 gap-2 text-sm mb-5">
      <div class="bg-slate-800 rounded-lg p-3">
        <div class="text-slate-500 text-xs mb-1">Node</div>
        <div id="inspNode" class="font-medium"></div>
      </div>
      <div class="bg-slate-800 rounded-lg p-3">
        <div class="text-slate-500 text-xs mb-1">Vị trí</div>
        <div id="inspLocation" class="font-medium text-xs"></div>
      </div>
      <div class="bg-slate-800 rounded-lg p-3">
        <div class="text-slate-500 text-xs mb-1">Ngưỡng cảnh báo</div>
        <div id="inspWarn" class="font-medium text-amber-400"></div>
      </div>
      <div class="bg-slate-800 rounded-lg p-3">
        <div class="text-slate-500 text-xs mb-1">Ngưỡng khẩn cấp</div>
        <div id="inspCrit" class="font-medium text-red-400"></div>
      </div>
    </div>
    <!-- Fault buttons -->
    <div class="flex gap-3 mb-5">
      <button id="btnInjectFault" onclick="injectFault()"
        class="flex-1 px-4 py-2 rounded-lg bg-red-700 hover:bg-red-600 text-white text-sm font-medium transition">
        ⚡ Inject Fault
      </button>
      <button id="btnClearFault" onclick="clearFault()"
        class="flex-1 px-4 py-2 rounded-lg bg-slate-700 hover:bg-slate-600 text-white text-sm font-medium transition">
        ✓ Clear Fault
      </button>
    </div>
    <!-- Param editor -->
    <div class="border-t border-slate-700 pt-5">
      <h3 class="text-sm font-semibold text-slate-400 mb-3">Chỉnh thông số giả lập</h3>
      <div class="space-y-3">
        <div>
          <label class="text-xs text-slate-500 mb-1 block">Nominal Value (giá trị nền)</label>
          <input id="pNominal" type="number" step="0.1"
            class="w-full bg-slate-800 border border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500"/>
        </div>
        <div>
          <label class="text-xs text-slate-500 mb-1 block">Drift Speed (tốc độ dao động)</label>
          <input id="pDrift" type="number" step="0.1" min="0.01"
            class="w-full bg-slate-800 border border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500"/>
        </div>
        <div>
          <label class="text-xs text-slate-500 mb-1 block">Warn Threshold (ngưỡng cảnh báo)</label>
          <input id="pWarn" type="number" step="0.5"
            class="w-full bg-slate-800 border border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500"/>
        </div>
        <div>
          <label class="text-xs text-slate-500 mb-1 block">Critical Threshold (ngưỡng khẩn cấp)</label>
          <input id="pCrit" type="number" step="0.5"
            class="w-full bg-slate-800 border border-slate-600 rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue-500"/>
        </div>
        <button onclick="saveParams()"
          class="w-full px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-sm font-medium transition">
          💾 Lưu thông số
        </button>
        <div id="paramsMsg" class="text-xs hidden"></div>
      </div>
    </div>
  </div>
</div>

<!-- ═══ SCRIPT ═══════════════════════════════════════════════════════════ -->
<script>
const BASE = 'http://localhost:5050';
let ws = null;
let sensors = {};          // sensorId → sensor object
let alertFeedCount = 0;
let inspectorSensorId = null;

// ── WebSocket connection ──────────────────────────────────────────────────
function connectWS() {
  ws = new WebSocket('ws://localhost:5050/ws');

  ws.onopen = () => {
    document.getElementById('wsIndicator').className = 'w-2.5 h-2.5 rounded-full bg-green-500';
    document.getElementById('wsStatus').textContent = 'Connected';
    loadSensors();
    loadAlerts();
  };

  ws.onclose = () => {
    document.getElementById('wsIndicator').className = 'w-2.5 h-2.5 rounded-full bg-red-500';
    document.getElementById('wsStatus').textContent = 'Disconnected — retrying…';
    setTimeout(connectWS, 3000);
  };

  ws.onerror = () => ws.close();

  ws.onmessage = e => {
    const msg = JSON.parse(e.data);
    if (msg.type === 'sensorTick') handleSensorTick(msg);
    else if (msg.type === 'alertGenerated') handleAlertGenerated(msg);
  };
}

// ── Load initial state ────────────────────────────────────────────────────
async function loadSensors() {
  try {
    const res = await fetch(`${BASE}/api/sensors`);
    const list = await res.json();
    list.forEach(s => sensors[s.sensorId] = s);
    renderSensorGrid();
    const online = list.filter(s => s.isOnline).length;
    document.getElementById('sensorOnlineCount').textContent = `${online}/${list.length} online`;
  } catch(e) { console.error('loadSensors', e); }
}

async function loadAlerts() {
  try {
    const res = await fetch(`${BASE}/api/alerts`);
    const list = await res.json();
    list.slice(0, 50).forEach(a => prependAlertCard(a, false));
    alertFeedCount = list.length;
    document.getElementById('alertCount').textContent = alertFeedCount;
  } catch(e) { console.error('loadAlerts', e); }
}

// ── Sensor grid rendering ────────────────────────────────────────────────
function renderSensorGrid() {
  const grid = document.getElementById('sensorGrid');
  grid.innerHTML = '';
  Object.values(sensors).forEach(s => {
    const card = makeSensorCard(s);
    grid.appendChild(card);
  });
}

function makeSensorCard(s) {
  const div = document.createElement('div');
  div.id = `card-${s.sensorId}`;
  div.className = `sensor-card bg-slate-800 border-2 rounded-xl p-3 cursor-pointer level-${s.level}`;
  div.onclick = () => openInspector(s.sensorId);
  div.innerHTML = sensorCardHTML(s);
  return div;
}

function sensorCardHTML(s) {
  const faultTag = s.isInFaultMode ? `<span class="text-xs bg-red-900 text-red-300 px-1.5 py-0.5 rounded">FAULT</span>` : '';
  const offlineTag = !s.isOnline ? `<span class="text-xs bg-gray-700 text-gray-400 px-1.5 py-0.5 rounded">OFFLINE</span>` : '';
  return `
    <div class="flex items-start justify-between mb-2">
      <div class="text-xs text-slate-400 leading-tight">${s.sensorName}</div>
      <span class="badge-${s.level} text-white text-[10px] px-1.5 py-0.5 rounded-full ml-1 shrink-0">${s.level}</span>
    </div>
    <div class="text-2xl font-mono font-bold mb-1">${fmt(s.currentValue)} <span class="text-sm font-normal text-slate-400">${s.unit}</span></div>
    <div class="text-xs text-slate-500">${s.nodeId} · ${s.category}</div>
    <div class="flex gap-1 mt-2">${faultTag}${offlineTag}</div>
  `;
}

function updateSensorCard(s) {
  sensors[s.sensorId] = { ...sensors[s.sensorId], ...s };
  const card = document.getElementById(`card-${s.sensorId}`);
  if (!card) return;
  const sensor = sensors[s.sensorId];
  card.className = `sensor-card bg-slate-800 border-2 rounded-xl p-3 cursor-pointer level-${sensor.level}`;
  card.innerHTML = sensorCardHTML(sensor);

  // Update inspector if open
  if (inspectorSensorId === s.sensorId) refreshInspectorValues(sensor);
}

function handleSensorTick(msg) {
  const s = sensors[msg.sensorId];
  if (!s) return;
  s.currentValue = msg.value;
  s.level = msg.level;
  updateSensorCard({ sensorId: msg.sensorId, currentValue: msg.value, level: msg.level });
}

function handleAlertGenerated(msg) {
  prependAlertCard(msg, true);
  alertFeedCount++;
  document.getElementById('alertCount').textContent = alertFeedCount;
}

// ── Alert feed ───────────────────────────────────────────────────────────
function prependAlertCard(a, animate) {
  const feed = document.getElementById('alertFeed');
  // Remove placeholder
  const placeholder = feed.querySelector('.text-slate-600');
  if (placeholder) placeholder.remove();

  const div = document.createElement('div');
  div.className = `alert-${a.severity} border-l-4 rounded-r-lg p-3 text-sm`;
  if (animate) div.style.animation = 'pulse 0.5s';

  const ts = new Date(a.ts || a.createdAt || Date.now());
  const timeStr = ts.toLocaleTimeString('vi-VN');
  div.innerHTML = `
    <div class="flex items-start justify-between gap-2">
      <span class="font-medium">${esc(a.title)}</span>
      <span class="badge-${a.severity} text-white text-[10px] px-1.5 py-0.5 rounded-full shrink-0">${a.severity}</span>
    </div>
    <div class="text-slate-400 text-xs mt-1 line-clamp-2">${esc(a.description || '')}</div>
    <div class="text-slate-500 text-xs mt-1">${esc(a.nodeName||'')} · ${timeStr}</div>
  `;
  feed.insertBefore(div, feed.firstChild);

  // Keep max 100 items
  while (feed.children.length > 100) feed.removeChild(feed.lastChild);
}

function clearAlertFeed() {
  document.getElementById('alertFeed').innerHTML =
    '<div class="text-slate-600 text-xs text-center py-6">Chưa có cảnh báo</div>';
  alertFeedCount = 0;
  document.getElementById('alertCount').textContent = '0';
}

// ── Simulation controls ──────────────────────────────────────────────────
async function simControl(action) {
  try {
    const res = await fetch(`${BASE}/api/simulation/${action}`, { method: 'POST' });
    const data = await res.json();
    showToast(action === 'start' ? '▶ Simulation started' : '■ Simulation stopped',
              action === 'start' ? 'green' : 'red');
  } catch(e) { showToast('Lỗi kết nối server', 'red'); }
}

// ── Fire manual alert ────────────────────────────────────────────────────
async function fireAlert() {
  const title = document.getElementById('alertTitle').value.trim();
  if (!title) { showFormMsg('alertFormMsg', 'Vui lòng nhập tiêu đề', 'red'); return; }

  const nodeVal = document.getElementById('alertNode').value.split('|');
  const body = {
    title,
    description: document.getElementById('alertDesc').value.trim(),
    severity:    document.getElementById('alertSeverity').value,
    category:    document.getElementById('alertCategory').value,
    nodeId:      nodeVal[0],
    nodeName:    nodeVal[1]
  };

  try {
    const res = await fetch(`${BASE}/api/alerts/fire`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    if (res.ok) {
      showFormMsg('alertFormMsg', '✓ Cảnh báo đã được phát', 'green');
      document.getElementById('alertTitle').value = '';
      document.getElementById('alertDesc').value = '';
    }
  } catch(e) { showFormMsg('alertFormMsg', 'Lỗi kết nối server', 'red'); }
}

// ── Sensor Inspector ─────────────────────────────────────────────────────
function openInspector(sensorId) {
  inspectorSensorId = sensorId;
  const s = sensors[sensorId];
  if (!s) return;

  document.getElementById('inspSensorName').textContent = s.sensorName;
  document.getElementById('inspUnit').textContent = s.unit;
  document.getElementById('inspNode').textContent = s.nodeName;
  document.getElementById('inspLocation').textContent = s.location || '';
  document.getElementById('inspWarn').textContent = `${s.warnThreshold} ${s.unit}`;
  document.getElementById('inspCrit').textContent = `${s.criticalThreshold} ${s.unit}`;
  document.getElementById('pNominal').value = s.nominalValue;
  document.getElementById('pDrift').value   = s.driftSpeed;
  document.getElementById('pWarn').value    = s.warnThreshold;
  document.getElementById('pCrit').value    = s.criticalThreshold;
  refreshInspectorValues(s);

  document.getElementById('inspectorOverlay').classList.remove('hidden');
  document.getElementById('inspector').style.transform = 'translateX(0)';
}

function refreshInspectorValues(s) {
  document.getElementById('inspValue').textContent = fmt(s.currentValue || s.CurrentValue);
  const badge = document.getElementById('inspBadge');
  badge.className = `ml-auto px-3 py-1 rounded-full text-xs text-white font-medium badge-${s.level}`;
  badge.textContent = s.level;
}

function closeInspector() {
  inspectorSensorId = null;
  document.getElementById('inspectorOverlay').classList.add('hidden');
  document.getElementById('inspector').style.transform = 'translateX(100%)';
}

async function injectFault() {
  if (!inspectorSensorId) return;
  try {
    await fetch(`${BASE}/api/sensors/${inspectorSensorId}/fault`, { method: 'POST' });
    showToast(`⚡ Fault injected: ${inspectorSensorId}`, 'red');
    if (sensors[inspectorSensorId]) sensors[inspectorSensorId].isInFaultMode = true;
  } catch(e) { showToast('Lỗi', 'red'); }
}

async function clearFault() {
  if (!inspectorSensorId) return;
  try {
    await fetch(`${BASE}/api/sensors/${inspectorSensorId}/fault`, { method: 'DELETE' });
    showToast(`✓ Fault cleared: ${inspectorSensorId}`, 'green');
    if (sensors[inspectorSensorId]) sensors[inspectorSensorId].isInFaultMode = false;
  } catch(e) { showToast('Lỗi', 'red'); }
}

async function saveParams() {
  if (!inspectorSensorId) return;
  const body = {
    nominalValue:      parseFloat(document.getElementById('pNominal').value) || null,
    driftSpeed:        parseFloat(document.getElementById('pDrift').value)   || null,
    warnThreshold:     parseFloat(document.getElementById('pWarn').value)    || null,
    criticalThreshold: parseFloat(document.getElementById('pCrit').value)    || null,
  };
  try {
    const res = await fetch(`${BASE}/api/sensors/${inspectorSensorId}/params`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    if (res.ok) showFormMsg('paramsMsg', '✓ Đã lưu thông số', 'green');
  } catch(e) { showFormMsg('paramsMsg', 'Lỗi', 'red'); }
}

// ── Toast notification ───────────────────────────────────────────────────
function showToast(msg, color) {
  const t = document.createElement('div');
  t.className = `fixed bottom-5 right-5 z-50 px-4 py-2 rounded-lg text-white text-sm font-medium shadow-lg ${color==='green'?'bg-green-600':'bg-red-600'}`;
  t.textContent = msg;
  document.body.appendChild(t);
  setTimeout(() => t.remove(), 2500);
}

function showFormMsg(id, msg, color) {
  const el = document.getElementById(id);
  el.className = `mt-2 text-xs ${color==='green'?'text-green-400':'text-red-400'}`;
  el.textContent = msg;
  el.classList.remove('hidden');
  setTimeout(() => el.classList.add('hidden'), 3000);
}

function fmt(v) {
  if (v === undefined || v === null) return '--';
  return Number(v).toFixed(1);
}

function esc(s) {
  return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
}

// ── Boot ─────────────────────────────────────────────────────────────────
connectWS();
</script>
</body>
</html>
```

**Step 2: Open `simulation.html` in browser**

Verify:
- 16 sensor cards render
- WS indicator turns green
- Alert feed loads history
- Click a sensor → inspector drawer slides in
- Click "▶ Start" → sensor values start updating live
- Inject Fault → card turns red and pulses

**Step 3: Commit**
```bash
git add simulation.html Station/Docs/plans/2026-03-09-simulation-web-dashboard.md
git commit -m "feat(web): add simulation control dashboard (simulation.html)"
```

---

## Summary

| File | Action |
|------|--------|
| `Station/Services/MockDataService.cs` | Add `FireManualAlert` + `UpdateSensorParams` |
| `Station/Services/SimulationApiServer.cs` | Create (REST + WebSocket server) |
| `Station/App.xaml.cs` | Wire up server + `MockDataService.Start()` |
| `simulation.html` | Create (single-file web dashboard) |

No new NuGet packages required. Open `simulation.html` in any browser while Station app is running.
