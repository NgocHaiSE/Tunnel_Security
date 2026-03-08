using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using Station.Models;

namespace Station.Services
{
    // ═══════════════════════════════════════════════════════════════════
    //  Event args
    // ═══════════════════════════════════════════════════════════════════

    public class SensorTickEventArgs : EventArgs
    {
        public SimulatedSensor Sensor { get; init; } = null!;
        public double NewValue { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public bool IsAnomaly { get; init; }
    }

    public class AlertGeneratedEventArgs : EventArgs
    {
        public Alert Alert { get; init; } = null!;
        public string? TriggeredByCameraId { get; init; }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SimulatedSensor
    // ═══════════════════════════════════════════════════════════════════

    public class SimulatedSensor
    {
        public string SensorId { get; set; } = string.Empty;
        public string SensorName { get; set; } = string.Empty;
        public AlertCategory Category { get; set; }
        public string Location { get; set; } = string.Empty;       // e.g. "Hầm A1 - Cửa van số 3"
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        // Simulation parameters
        public double CurrentValue { get; set; }
        public double NominalValue { get; set; }    // Centre of normal range
        public double DriftSpeed { get; set; }      // Max change per tick
        public double MinNormal { get; set; }
        public double MaxNormal { get; set; }
        public double WarnThreshold { get; set; }
        public double CriticalThreshold { get; set; }
        public double AbsoluteMin { get; set; }
        public double AbsoluteMax { get; set; }

        // Fault injection state (set externally to test)
        public bool IsInFaultMode { get; set; } = false;
        public bool IsOnline { get; set; } = true;

        // Derived
        public SensorAlertLevel CurrentLevel =>
            !IsOnline ? SensorAlertLevel.Offline :
            CurrentValue >= CriticalThreshold ? SensorAlertLevel.Critical :
            CurrentValue >= WarnThreshold ? SensorAlertLevel.Warning :
            SensorAlertLevel.Normal;

        public AlertSeverity CurrentAlertSeverity =>
            CurrentValue >= CriticalThreshold ? AlertSeverity.Critical :
            CurrentValue >= WarnThreshold ? AlertSeverity.High :
            AlertSeverity.Low;

        public string StatusText => CurrentLevel switch
        {
            SensorAlertLevel.Critical => "Khẩn cấp",
            SensorAlertLevel.Warning => "Cảnh báo",
            SensorAlertLevel.Offline => "Mất kết nối",
            _ => "Bình thường"
        };
    }

    public enum SensorAlertLevel { Normal, Warning, Critical, Offline }

    // ═══════════════════════════════════════════════════════════════════
    //  Camera simulation entry
    // ═══════════════════════════════════════════════════════════════════

    public class SimulatedCamera
    {
        public string CameraId { get; set; } = string.Empty;
        public string CameraName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public bool IsOnline { get; set; } = true;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MockDataService  (Singleton)
    // ═══════════════════════════════════════════════════════════════════

    public sealed class MockDataService
    {
        // ── Singleton ──────────────────────────────────────────────────
        private static readonly Lazy<MockDataService> _instance =
            new(() => new MockDataService());
        public static MockDataService Instance => _instance.Value;

        // ── Events ─────────────────────────────────────────────────────
        /// Fired every tick for every sensor with its new reading
        public event EventHandler<SensorTickEventArgs>? SensorTick;
        /// Fired when an alert condition is detected
        public event EventHandler<AlertGeneratedEventArgs>? AlertGenerated;

        // ── Public state ───────────────────────────────────────────────
        public IReadOnlyList<SimulatedSensor> Sensors { get; }
        public IReadOnlyList<SimulatedCamera> Cameras { get; }
        public ObservableCollection<Alert> ActiveAlerts { get; } = new();
        public ObservableCollection<Alert> AlertHistory { get; } = new();

        // ── Private ────────────────────────────────────────────────────
        private readonly Random _rng = new();
        private readonly Timer _sensorTimer;
        private readonly Timer _alertTimer;

        // Per-sensor cooldown so we don't spam alerts
        private readonly Dictionary<string, DateTimeOffset> _alertCooldowns = new();
        private static readonly TimeSpan AlertCooldown = TimeSpan.FromSeconds(30);

        // Camera intrusion cooldown
        private readonly Dictionary<string, DateTimeOffset> _cameraAlertCooldowns = new();
        private static readonly TimeSpan CameraAlertCooldown = TimeSpan.FromSeconds(45);

        // ── Constructor ────────────────────────────────────────────────
        private MockDataService()
        {
            Sensors = BuildSensors();
            Cameras = BuildCameras();

            // Sensor tick: every 1.5s — updates readings & fires SensorTick
            _sensorTimer = new Timer(1500);
            _sensorTimer.Elapsed += OnSensorTick;
            _sensorTimer.AutoReset = true;

            // Alert evaluation: every 4s — checks thresholds & fires random camera alerts
            _alertTimer = new Timer(4000);
            _alertTimer.Elapsed += OnAlertEvaluation;
            _alertTimer.AutoReset = true;
        }

        // ── Start / Stop ───────────────────────────────────────────────
        public void Start()
        {
            _sensorTimer.Start();
            _alertTimer.Start();
        }

        public void Stop()
        {
            _sensorTimer.Stop();
            _alertTimer.Stop();
        }

        // ══════════════════════════════════════════════════════════════
        //  Sensor tick — random walk all sensor values
        // ══════════════════════════════════════════════════════════════

        private void OnSensorTick(object? sender, ElapsedEventArgs e)
        {
            foreach (var sensor in Sensors)
            {
                if (!sensor.IsOnline) continue;

                double newValue = sensor.IsInFaultMode
                    ? SimulateFaultSpike(sensor)
                    : RandomWalk(sensor);

                sensor.CurrentValue = newValue;

                SensorTick?.Invoke(this, new SensorTickEventArgs
                {
                    Sensor = sensor,
                    NewValue = newValue,
                    Timestamp = DateTimeOffset.Now,
                    IsAnomaly = sensor.CurrentLevel >= SensorAlertLevel.Warning
                });
            }
        }

        /// Gaussian-biased random walk — values drift toward nominal, occasionally spike
        private double RandomWalk(SimulatedSensor s)
        {
            // Revert-to-mean force (5% per tick)
            double reversion = (s.NominalValue - s.CurrentValue) * 0.05;

            // Random noise proportional to drift speed
            double noise = (_rng.NextDouble() * 2 - 1) * s.DriftSpeed;

            // Rare spike (1.5% chance — push toward or past threshold)
            if (_rng.NextDouble() < 0.015)
                noise += (_rng.NextDouble() > 0.5 ? 1 : -1) * s.DriftSpeed * 6;

            double next = s.CurrentValue + reversion + noise;
            return Math.Clamp(next, s.AbsoluteMin, s.AbsoluteMax);
        }

        private double SimulateFaultSpike(SimulatedSensor s)
        {
            // In fault mode, value ramps toward critical
            double target = s.AbsoluteMax;
            double step = s.DriftSpeed * 3;
            return Math.Min(s.CurrentValue + step, target);
        }

        // ══════════════════════════════════════════════════════════════
        //  Alert evaluation — threshold crossing + camera events
        // ══════════════════════════════════════════════════════════════

        private void OnAlertEvaluation(object? sender, ElapsedEventArgs e)
        {
            EvaluateSensorAlerts();
            MaybeFireCameraAlert();
            MaybeFireRandomSensorAlert();
        }

        private void EvaluateSensorAlerts()
        {
            foreach (var sensor in Sensors)
            {
                if (!sensor.IsOnline) continue;
                if (sensor.CurrentLevel == SensorAlertLevel.Normal) continue;

                // Check cooldown
                string key = $"sensor:{sensor.SensorId}";
                if (_alertCooldowns.TryGetValue(key, out var last) &&
                    DateTimeOffset.Now - last < AlertCooldown) continue;

                var alert = BuildSensorAlert(sensor);
                RegisterAlert(alert);
                _alertCooldowns[key] = DateTimeOffset.Now;
                AlertGenerated?.Invoke(this, new AlertGeneratedEventArgs { Alert = alert });
            }
        }

        private void MaybeFireCameraAlert()
        {
            // ~8% chance per 4-second evaluation to generate a camera alert
            if (_rng.NextDouble() > 0.08) return;

            var onlineCams = Cameras.Where(c => c.IsOnline).ToList();
            if (onlineCams.Count == 0) return;

            var cam = onlineCams[_rng.Next(onlineCams.Count)];
            string key = $"cam:{cam.CameraId}";
            if (_alertCooldowns.TryGetValue(key, out var last) &&
                DateTimeOffset.Now - last < CameraAlertCooldown) return;

            var alert = BuildCameraAlert(cam);
            RegisterAlert(alert);
            _alertCooldowns[key] = DateTimeOffset.Now;
            AlertGenerated?.Invoke(this, new AlertGeneratedEventArgs
            {
                Alert = alert,
                TriggeredByCameraId = cam.CameraId
            });
        }

        private void MaybeFireRandomSensorAlert()
        {
            // ~3% chance — random sensor anomaly (brief spike already logged by walk, this fires an alert)
            if (_rng.NextDouble() > 0.03) return;

            // Pick a sensor that is currently just below warning — give it a push
            var candidate = Sensors
                .Where(s => s.IsOnline && !s.IsInFaultMode &&
                            s.CurrentValue >= s.MaxNormal * 0.85 &&
                            s.CurrentValue < s.WarnThreshold)
                .OrderByDescending(s => s.CurrentValue / s.WarnThreshold)
                .FirstOrDefault();

            if (candidate == null) return;

            // Force it over the warning threshold
            candidate.CurrentValue = candidate.WarnThreshold * (1.0 + _rng.NextDouble() * 0.15);
        }

        // ══════════════════════════════════════════════════════════════
        //  Alert builders
        // ══════════════════════════════════════════════════════════════

        private Alert BuildSensorAlert(SimulatedSensor sensor)
        {
            var (title, description) = GetSensorAlertText(sensor);
            return new Alert
            {
                Title = title,
                Description = description,
                Category = sensor.Category,
                Severity = sensor.CurrentAlertSeverity,
                State = AlertState.Unprocessed,
                LineId = "LINE-01",
                LineName = "Tuyến hầm chính",
                NodeId = sensor.NodeId,
                NodeName = sensor.NodeName,
                SensorId = sensor.SensorId,
                SensorName = sensor.SensorName,
                SensorType = sensor.Category.ToString(),
                SensorValue = Math.Round(sensor.CurrentValue, 2),
                SensorUnit = sensor.Unit,
                Threshold = sensor.CurrentAlertSeverity == AlertSeverity.Critical
                    ? sensor.CriticalThreshold : sensor.WarnThreshold,
                CreatedAt = DateTimeOffset.Now
            };
        }

        private Alert BuildCameraAlert(SimulatedCamera cam)
        {
            var scenarios = _cameraAlertScenarios;
            var scenario = scenarios[_rng.Next(scenarios.Length)];
            return new Alert
            {
                Title = scenario.Title,
                Description = scenario.Description,
                Category = AlertCategory.Intrusion,
                Severity = scenario.Severity,
                State = AlertState.Unprocessed,
                LineId = "LINE-01",
                LineName = "Tuyến hầm chính",
                NodeId = cam.NodeId,
                NodeName = cam.NodeName,
                CameraId = cam.CameraId,
                CreatedAt = DateTimeOffset.Now
            };
        }

        private void RegisterAlert(Alert alert)
        {
            ActiveAlerts.Insert(0, alert);
            AlertHistory.Insert(0, alert);

            // Keep history capped at 200
            while (AlertHistory.Count > 200)
                AlertHistory.RemoveAt(AlertHistory.Count - 1);
        }

        // ══════════════════════════════════════════════════════════════
        //  Public helpers
        // ══════════════════════════════════════════════════════════════

        public void AcknowledgeAlert(Alert alert, string by = "Operator")
        {
            alert.State = AlertState.Acknowledged;
            alert.AcknowledgedAt = DateTimeOffset.Now;
            alert.AcknowledgedBy = by;
            ActiveAlerts.Remove(alert);
        }

        public void ResolveAlert(Alert alert, string by = "Operator")
        {
            alert.State = AlertState.Resolved;
            alert.ResolvedAt = DateTimeOffset.Now;
            alert.ResolvedBy = by;
            ActiveAlerts.Remove(alert);
        }

        /// Inject a fault into a sensor to force a critical alert (for demo/testing)
        public void InjectSensorFault(string sensorId)
        {
            var sensor = Sensors.FirstOrDefault(s => s.SensorId == sensorId);
            if (sensor != null) sensor.IsInFaultMode = true;
        }

        public void ClearSensorFault(string sensorId)
        {
            var sensor = Sensors.FirstOrDefault(s => s.SensorId == sensorId);
            if (sensor != null) sensor.IsInFaultMode = false;
        }

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
            if (nominalValue.HasValue)      sensor.NominalValue      = nominalValue.Value;
            if (driftSpeed.HasValue)        sensor.DriftSpeed        = driftSpeed.Value;
            if (warnThreshold.HasValue)     sensor.WarnThreshold     = warnThreshold.Value;
            if (criticalThreshold.HasValue) sensor.CriticalThreshold = criticalThreshold.Value;
        }

        // ══════════════════════════════════════════════════════════════
        //  Sensor definitions (16 sensors across 4 tunnel segments)
        // ══════════════════════════════════════════════════════════════

        private List<SimulatedSensor> BuildSensors() => new()
        {
            // ─── Segment A1: Cửa vào hầm ─────────────────────────────
            new SimulatedSensor
            {
                SensorId = "SNS-A1-T01", SensorName = "Cảm biến nhiệt độ A1",
                Category = AlertCategory.Temperature,
                Location = "Hầm A1 - Cửa vào", NodeId = "NODE-A1", NodeName = "Nút A1",
                Unit = "°C",
                NominalValue = 24, CurrentValue = 24,
                MinNormal = 18, MaxNormal = 30,
                WarnThreshold = 38, CriticalThreshold = 50,
                AbsoluteMin = -5, AbsoluteMax = 80,
                DriftSpeed = 0.4
            },
            new SimulatedSensor
            {
                SensorId = "SNS-A1-H01", SensorName = "Cảm biến độ ẩm A1",
                Category = AlertCategory.Humidity,
                Location = "Hầm A1 - Cửa vào", NodeId = "NODE-A1", NodeName = "Nút A1",
                Unit = "%RH",
                NominalValue = 55, CurrentValue = 55,
                MinNormal = 40, MaxNormal = 70,
                WarnThreshold = 80, CriticalThreshold = 90,
                AbsoluteMin = 10, AbsoluteMax = 99,
                DriftSpeed = 0.8
            },
            new SimulatedSensor
            {
                SensorId = "SNS-A1-G01", SensorName = "Cảm biến CO A1",
                Category = AlertCategory.Gas,
                Location = "Hầm A1 - Cửa vào", NodeId = "NODE-A1", NodeName = "Nút A1",
                Unit = "ppm",
                NominalValue = 15, CurrentValue = 15,
                MinNormal = 0, MaxNormal = 50,
                WarnThreshold = 80, CriticalThreshold = 150,
                AbsoluteMin = 0, AbsoluteMax = 500,
                DriftSpeed = 1.5
            },
            new SimulatedSensor
            {
                SensorId = "SNS-A1-W01", SensorName = "Cảm biến mực nước A1",
                Category = AlertCategory.WaterLevel,
                Location = "Hầm A1 - Rãnh thoát nước", NodeId = "NODE-A1", NodeName = "Nút A1",
                Unit = "cm",
                NominalValue = 8, CurrentValue = 8,
                MinNormal = 0, MaxNormal = 25,
                WarnThreshold = 40, CriticalThreshold = 60,
                AbsoluteMin = 0, AbsoluteMax = 120,
                DriftSpeed = 0.6
            },

            // ─── Segment B2: Giữa hầm ─────────────────────────────────
            new SimulatedSensor
            {
                SensorId = "SNS-B2-T01", SensorName = "Cảm biến nhiệt độ B2",
                Category = AlertCategory.Temperature,
                Location = "Hầm B2 - Đường ống chính", NodeId = "NODE-B2", NodeName = "Nút B2",
                Unit = "°C",
                NominalValue = 26, CurrentValue = 26,
                MinNormal = 18, MaxNormal = 32,
                WarnThreshold = 38, CriticalThreshold = 52,
                AbsoluteMin = -5, AbsoluteMax = 80,
                DriftSpeed = 0.5
            },
            new SimulatedSensor
            {
                SensorId = "SNS-B2-G01", SensorName = "Cảm biến CO₂ B2",
                Category = AlertCategory.Gas,
                Location = "Hầm B2 - Khu vực trung tâm", NodeId = "NODE-B2", NodeName = "Nút B2",
                Unit = "ppm",
                NominalValue = 600, CurrentValue = 600,
                MinNormal = 400, MaxNormal = 800,
                WarnThreshold = 1200, CriticalThreshold = 2000,
                AbsoluteMin = 300, AbsoluteMax = 5000,
                DriftSpeed = 20
            },
            new SimulatedSensor
            {
                SensorId = "SNS-B2-M01", SensorName = "Cảm biến rung B2",
                Category = AlertCategory.Motion,
                Location = "Hầm B2 - Kết cấu hầm", NodeId = "NODE-B2", NodeName = "Nút B2",
                Unit = "mm/s",
                NominalValue = 5, CurrentValue = 5,
                MinNormal = 0, MaxNormal = 15,
                WarnThreshold = 25, CriticalThreshold = 40,
                AbsoluteMin = 0, AbsoluteMax = 100,
                DriftSpeed = 0.8
            },
            new SimulatedSensor
            {
                SensorId = "SNS-B2-H01", SensorName = "Cảm biến độ ẩm B2",
                Category = AlertCategory.Humidity,
                Location = "Hầm B2 - Đường ống chính", NodeId = "NODE-B2", NodeName = "Nút B2",
                Unit = "%RH",
                NominalValue = 60, CurrentValue = 60,
                MinNormal = 45, MaxNormal = 72,
                WarnThreshold = 82, CriticalThreshold = 92,
                AbsoluteMin = 10, AbsoluteMax = 99,
                DriftSpeed = 1.0
            },

            // ─── Segment C3: Phòng điều khiển ────────────────────────
            new SimulatedSensor
            {
                SensorId = "SNS-C3-T01", SensorName = "Cảm biến nhiệt độ tủ điện C3",
                Category = AlertCategory.Temperature,
                Location = "Phòng điều khiển C3 - Tủ điện", NodeId = "NODE-C3", NodeName = "Nút C3",
                Unit = "°C",
                NominalValue = 22, CurrentValue = 22,
                MinNormal = 16, MaxNormal = 28,
                WarnThreshold = 35, CriticalThreshold = 45,
                AbsoluteMin = 5, AbsoluteMax = 80,
                DriftSpeed = 0.3
            },
            new SimulatedSensor
            {
                SensorId = "SNS-C3-A01", SensorName = "Cảm biến âm thanh C3",
                Category = AlertCategory.Other,
                Location = "Phòng điều khiển C3", NodeId = "NODE-C3", NodeName = "Nút C3",
                Unit = "dB",
                NominalValue = 45, CurrentValue = 45,
                MinNormal = 30, MaxNormal = 65,
                WarnThreshold = 85, CriticalThreshold = 95,
                AbsoluteMin = 20, AbsoluteMax = 120,
                DriftSpeed = 2.0
            },
            new SimulatedSensor
            {
                SensorId = "SNS-C3-W01", SensorName = "Cảm biến mực nước C3",
                Category = AlertCategory.WaterLevel,
                Location = "Phòng điều khiển C3 - Hố thu nước", NodeId = "NODE-C3", NodeName = "Nút C3",
                Unit = "cm",
                NominalValue = 5, CurrentValue = 5,
                MinNormal = 0, MaxNormal = 20,
                WarnThreshold = 35, CriticalThreshold = 55,
                AbsoluteMin = 0, AbsoluteMax = 100,
                DriftSpeed = 0.4
            },

            // ─── Segment D4: Cửa ra hầm ──────────────────────────────
            new SimulatedSensor
            {
                SensorId = "SNS-D4-T01", SensorName = "Cảm biến nhiệt độ D4",
                Category = AlertCategory.Temperature,
                Location = "Hầm D4 - Cửa ra", NodeId = "NODE-D4", NodeName = "Nút D4",
                Unit = "°C",
                NominalValue = 23, CurrentValue = 23,
                MinNormal = 17, MaxNormal = 31,
                WarnThreshold = 37, CriticalThreshold = 50,
                AbsoluteMin = -5, AbsoluteMax = 80,
                DriftSpeed = 0.4
            },
            new SimulatedSensor
            {
                SensorId = "SNS-D4-G01", SensorName = "Cảm biến CO D4",
                Category = AlertCategory.Gas,
                Location = "Hầm D4 - Cửa ra", NodeId = "NODE-D4", NodeName = "Nút D4",
                Unit = "ppm",
                NominalValue = 12, CurrentValue = 12,
                MinNormal = 0, MaxNormal = 50,
                WarnThreshold = 80, CriticalThreshold = 150,
                AbsoluteMin = 0, AbsoluteMax = 500,
                DriftSpeed = 1.2
            },
            new SimulatedSensor
            {
                SensorId = "SNS-D4-M01", SensorName = "Cảm biến chuyển động D4",
                Category = AlertCategory.Motion,
                Location = "Hầm D4 - Khu vực cấm", NodeId = "NODE-D4", NodeName = "Nút D4",
                Unit = "%",
                NominalValue = 5, CurrentValue = 5,
                MinNormal = 0, MaxNormal = 30,
                WarnThreshold = 60, CriticalThreshold = 85,
                AbsoluteMin = 0, AbsoluteMax = 100,
                DriftSpeed = 2.5
            },
            new SimulatedSensor
            {
                SensorId = "SNS-D4-H01", SensorName = "Cảm biến độ ẩm D4",
                Category = AlertCategory.Humidity,
                Location = "Hầm D4 - Cửa ra", NodeId = "NODE-D4", NodeName = "Nút D4",
                Unit = "%RH",
                NominalValue = 58, CurrentValue = 58,
                MinNormal = 42, MaxNormal = 72,
                WarnThreshold = 82, CriticalThreshold = 92,
                AbsoluteMin = 10, AbsoluteMax = 99,
                DriftSpeed = 0.9
            },
            new SimulatedSensor
            {
                SensorId = "SNS-D4-W01", SensorName = "Cảm biến mực nước D4",
                Category = AlertCategory.WaterLevel,
                Location = "Hầm D4 - Rãnh thoát nước", NodeId = "NODE-D4", NodeName = "Nút D4",
                Unit = "cm",
                NominalValue = 10, CurrentValue = 10,
                MinNormal = 0, MaxNormal = 25,
                WarnThreshold = 42, CriticalThreshold = 62,
                AbsoluteMin = 0, AbsoluteMax = 120,
                DriftSpeed = 0.7
            }
        };

        // ══════════════════════════════════════════════════════════════
        //  Camera definitions (16 cameras matching LiveVideoViewModel)
        // ══════════════════════════════════════════════════════════════

        private List<SimulatedCamera> BuildCameras() => new()
        {
            new SimulatedCamera { CameraId = "CAM-01", CameraName = "Camera #1", Location = "Hầm A1 - Cửa vào",     NodeId = "NODE-A1", NodeName = "Nút A1" },
            new SimulatedCamera { CameraId = "CAM-02", CameraName = "Camera #2", Location = "Hầm A1 - Hành lang",   NodeId = "NODE-A1", NodeName = "Nút A1" },
            new SimulatedCamera { CameraId = "CAM-03", CameraName = "Camera #3", Location = "Hầm A1 - Cửa van số 3",NodeId = "NODE-A1", NodeName = "Nút A1" },
            new SimulatedCamera { CameraId = "CAM-04", CameraName = "Camera #4", Location = "Hầm A2 - Kho thiết bị",NodeId = "NODE-A1", NodeName = "Nút A1" },
            new SimulatedCamera { CameraId = "CAM-05", CameraName = "Camera #5", Location = "Hầm B1 - Đường ống",   NodeId = "NODE-B2", NodeName = "Nút B2" },
            new SimulatedCamera { CameraId = "CAM-06", CameraName = "Camera #6", Location = "Hầm B1 - Trung tâm",   NodeId = "NODE-B2", NodeName = "Nút B2" },
            new SimulatedCamera { CameraId = "CAM-07", CameraName = "Camera #7", Location = "Hầm B2 - Đường ống chính", NodeId = "NODE-B2", NodeName = "Nút B2" },
            new SimulatedCamera { CameraId = "CAM-08", CameraName = "Camera #8", Location = "Hầm B2 - Cửa phụ",     NodeId = "NODE-B2", NodeName = "Nút B2" },
            new SimulatedCamera { CameraId = "CAM-09", CameraName = "Camera #9", Location = "Phòng điều khiển C3",  NodeId = "NODE-C3", NodeName = "Nút C3" },
            new SimulatedCamera { CameraId = "CAM-10", CameraName = "Camera #10",Location = "Phòng điều khiển C3 - Tủ điện", NodeId = "NODE-C3", NodeName = "Nút C3" },
            new SimulatedCamera { CameraId = "CAM-11", CameraName = "Camera #11",Location = "Hầm C3 - Lối thoát hiểm",NodeId = "NODE-C3", NodeName = "Nút C3" },
            new SimulatedCamera { CameraId = "CAM-12", CameraName = "Camera #12",Location = "Hầm C3 - Cửa phụ",    NodeId = "NODE-C3", NodeName = "Nút C3" },
            new SimulatedCamera { CameraId = "CAM-13", CameraName = "Camera #13",Location = "Hầm D4 - Cửa ra",     NodeId = "NODE-D4", NodeName = "Nút D4", IsOnline = false },
            new SimulatedCamera { CameraId = "CAM-14", CameraName = "Camera #14",Location = "Hầm D4 - Khu vực cấm",NodeId = "NODE-D4", NodeName = "Nút D4", IsOnline = false },
            new SimulatedCamera { CameraId = "CAM-15", CameraName = "Camera #15",Location = "Hầm D4 - Hành lang",  NodeId = "NODE-D4", NodeName = "Nút D4", IsOnline = false },
            new SimulatedCamera { CameraId = "CAM-16", CameraName = "Camera #16",Location = "Hầm D4 - Đầu hầm",    NodeId = "NODE-D4", NodeName = "Nút D4", IsOnline = false },
        };

        // ══════════════════════════════════════════════════════════════
        //  Alert text templates
        // ══════════════════════════════════════════════════════════════

        private static (string Title, string Description) GetSensorAlertText(SimulatedSensor s)
        {
            string level = s.CurrentAlertSeverity == AlertSeverity.Critical ? "nguy hiểm" : "cao";
            return s.Category switch
            {
                AlertCategory.Temperature => (
                    $"Nhiệt độ {level} tại {s.NodeName}",
                    $"Cảm biến {s.SensorName} ghi nhận nhiệt độ {s.CurrentValue:F1}{s.Unit} — vượt ngưỡng {(s.CurrentAlertSeverity == AlertSeverity.Critical ? s.CriticalThreshold : s.WarnThreshold)}{s.Unit}. Kiểm tra hệ thống thông gió và thiết bị làm mát ngay lập tức."),
                AlertCategory.Humidity => (
                    $"Độ ẩm {level} tại {s.NodeName}",
                    $"Cảm biến {s.SensorName} ghi nhận độ ẩm {s.CurrentValue:F1}{s.Unit}. Nguy cơ ăn mòn thiết bị điện và ảnh hưởng đến kết cấu hầm. Kích hoạt hệ thống hút ẩm."),
                AlertCategory.WaterLevel => (
                    $"Mực nước {level} tại {s.NodeName}",
                    $"Mực nước tại {s.Location} đạt {s.CurrentValue:F1}{s.Unit}. {(s.CurrentAlertSeverity == AlertSeverity.Critical ? "NGUY HIỂM NGHIÊM TRỌNG: Kích hoạt bơm thoát nước khẩn cấp và sơ tán khu vực!" : "Kích hoạt bơm thoát nước và theo dõi sát.")}"),
                AlertCategory.Gas => (
                    $"Nồng độ khí {level} tại {s.NodeName}",
                    $"Cảm biến {s.SensorName} phát hiện nồng độ khí {s.CurrentValue:F0}{s.Unit}. {(s.CurrentAlertSeverity == AlertSeverity.Critical ? "KHẨN CẤP: Kích hoạt hệ thống thông gió, cấm người vào khu vực!" : "Tăng cường thông gió và giám sát liên tục.")}"),
                AlertCategory.Motion => (
                    $"Rung chấn {level} tại {s.NodeName}",
                    $"Cảm biến {s.SensorName} ghi nhận rung động {s.CurrentValue:F1}{s.Unit}. Kiểm tra kết cấu hầm và các thiết bị cơ khí trong khu vực."),
                _ => (
                    $"Cảnh báo cảm biến tại {s.NodeName}",
                    $"Cảm biến {s.SensorName} ghi nhận giá trị bất thường: {s.CurrentValue:F2}{s.Unit}. Cần kiểm tra và đánh giá ngay.")
            };
        }

        private readonly (string Title, string Description, AlertSeverity Severity)[] _cameraAlertScenarios =
        {
            (
                "Phát hiện xâm nhập trái phép",
                "Hệ thống AI phát hiện người xâm nhập vào khu vực hầm cấm. Đối tượng đang di chuyển với tốc độ bất thường. Điều phối lực lượng bảo vệ ngay.",
                AlertSeverity.Critical
            ),
            (
                "Chuyển động bất thường trong hầm",
                "Camera phát hiện chuyển động bất thường tại khu vực giám sát. Đối tượng không rõ danh tính đang di chuyển trong khu vực hạn chế.",
                AlertSeverity.High
            ),
            (
                "Phát hiện phương tiện không được phép",
                "Hệ thống nhận diện biển số phát hiện phương tiện lạ không có trong danh sách được phép. Phương tiện đang đỗ tại vị trí không quy định.",
                AlertSeverity.High
            ),
            (
                "Phát hiện vật thể bỏ lại",
                "Camera AI phát hiện vật thể bỏ lại trong khu vực hầm hơn 5 phút. Kiểm tra ngay để đảm bảo an toàn theo quy trình.",
                AlertSeverity.Medium
            ),
            (
                "Lảng vảng khu vực hạn chế",
                "Cảnh báo: đối tượng di chuyển quanh khu vực hạn chế trong thời gian dài bất thường. Hành vi đáng ngờ — cần xác minh ngay.",
                AlertSeverity.Medium
            ),
            (
                "Phát hiện nhóm người bất thường",
                "Camera phát hiện tập hợp nhóm người tại khu vực không được phép. Số lượng vượt giới hạn cho phép theo quy định an toàn.",
                AlertSeverity.High
            ),
            (
                "Cảnh báo vượt ranh giới ảo",
                "Hệ thống AI xác nhận có đối tượng vượt qua đường ranh giới giám sát tại vị trí cửa hầm phụ. Kích hoạt quy trình kiểm soát.",
                AlertSeverity.Critical
            ),
            (
                "Mất tín hiệu camera tạm thời",
                "Camera bị mất tín hiệu trong 3 giây. Tín hiệu đã khôi phục nhưng cần kiểm tra thiết bị và đường truyền để xác nhận nguyên nhân.",
                AlertSeverity.Low
            ),
        };
    }
}
