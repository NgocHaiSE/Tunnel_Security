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
            // GET /api/sensors với optional filters
            if (method == "GET" && path == "/api/sensors")
            {
                var query = ctx.Request.QueryString;
                var lineId = query["lineId"];
                var nodeId = query["nodeId"];
                var type = query["type"];

                var list = new List<object>();
                foreach (var s in _mock.Sensors)
                {
                    // Apply filters
                    if (!string.IsNullOrEmpty(lineId) && s.LineId != lineId) continue;
                    if (!string.IsNullOrEmpty(nodeId) && s.NodeId != nodeId) continue;
                    if (!string.IsNullOrEmpty(type) && s.Category.ToString().ToLower() != type.ToLower()) continue;

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
                }
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
                    if (dto == null) { await WriteJsonAsync(ctx.Response, 400, new { error = "invalid body" }); return; }
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
            res.StatusCode      = status;
            res.ContentType     = "application/json; charset=utf-8";
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
