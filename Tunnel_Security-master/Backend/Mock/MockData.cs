using Backend.Models;

namespace Backend.Mock;

public static class MockData
{
    /// <summary>
    /// Tính tọa độ nút dọc theo đường thẳng giữa 2 điểm
    /// </summary>
    private static (double lng, double lat) InterpolatePoint(
        double startLng, double startLat,
        double endLng, double endLat,
        double ratio)
    {
        return (
            startLng + (endLng - startLng) * ratio,
            startLat + (endLat - startLat) * ratio
        );
    }

    public static List<Station> GetStations()
    {
        // Tọa độ trung tâm - HUB chính tại ngã tư Cầu Giấy (giao Xuân Thủy - Cầu Giấy)
        double hubLng = 105.7965;
        double hubLat = 21.0365;

        var station = new Station
        {
            Id = "ST01",
            Name = "Trạm Giám Sát Cống Quận Cầu Giấy",
            District = "Cầu Giấy, Hà Nội",
            CenterLng = hubLng,
            CenterLat = hubLat,
            MinLng = 105.78,
            MinLat = 21.02,
            MaxLng = 105.82,
            MaxLat = 21.06,
            CreatedAt = DateTime.UtcNow
        };

        // ========== HUB TRUNG TÂM ==========
        var hubLine = new Line
        {
            Id = "L0",
            StationId = "ST01",
            Code = "HUB",
            Name = "Trung tâm điều khiển",
            Description = "Hub trung tâm tại ngã tư Cầu Giấy",
            StartLng = hubLng,
            StartLat = hubLat,
            EndLng = hubLng,
            EndLat = hubLat,
            Length = 0,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        var hubNode = new Node
        {
            Id = "HUB-01",
            LineId = "L0",
            Code = "HUB",
            Name = "Trung tâm điều khiển",
            Description = "Hub trung tâm - Ngã tư Cầu Giấy",
            Lng = hubLng,
            Lat = hubLat,
            Status = NodeStatus.Online,
            LastOnline = DateTime.UtcNow,
            HardwareId = "HW-HUB-01",
            FirmwareVersion = "v3.0.0",
            IsHub = true,
            BatteryLevel = 100,
            RSSI = -30,
            CameraId = "CAM-HUB-01",
            CreatedAt = DateTime.UtcNow
        };
        hubNode.Sensors = CreateSensorsForNode(hubNode.Id, 0);
        hubLine.Nodes.Add(hubNode);

        // ========== TUYẾN 1: Đường Xuân Thủy (Đông - về phía Cầu Giấy) ==========
        // Cống chính dọc đường Xuân Thủy hướng về ĐH Quốc Gia
        var line1 = new Line
        {
            Id = "L1",
            StationId = "ST01",
            Code = "XT",
            Name = "Cống Xuân Thủy",
            Description = "Tuyến cống chính dọc đường Xuân Thủy",
            StartLng = hubLng,
            StartLat = hubLat,
            EndLng = 105.8085,  // Hướng về ĐH Quốc Gia
            EndLat = 21.0385,
            Length = 1350,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        for (int i = 1; i <= 3; i++)
        {
            double ratio = i / 3.0;
            var (lng, lat) = InterpolatePoint(line1.StartLng, line1.StartLat, line1.EndLng, line1.EndLat, ratio);

            var node = new Node
            {
                Id = $"XT-{i}",
                LineId = "L1",
                Code = $"XT{i}",
                Name = $"Cống Xuân Thủy {i}",
                Description = $"Hố ga số {i} - Đường Xuân Thủy",
                Lng = lng,
                Lat = lat,
                Status = i == 2 ? NodeStatus.Warning : NodeStatus.Online,
                LastOnline = DateTime.UtcNow.AddMinutes(-i * 5),
                HardwareId = $"HW-XT-{i}",
                FirmwareVersion = "v2.1.3",
                IsHub = false,
                BatteryLevel = 92 - i * 8,
                RSSI = -45 - i * 5,
                CameraId = i == 3 ? "CAM-XT-3" : null,
                CreatedAt = DateTime.UtcNow
            };
            node.Sensors = CreateSensorsForNode(node.Id, i);
            line1.Nodes.Add(node);
        }

        // ========== TUYẾN 2: Đường Cầu Giấy (Bắc - về phía Hoàng Quốc Việt) ==========
        var line2 = new Line
        {
            Id = "L2",
            StationId = "ST01",
            Code = "CG",
            Name = "Cống Cầu Giấy",
            Description = "Tuyến cống dọc đường Cầu Giấy",
            StartLng = hubLng,
            StartLat = hubLat,
            EndLng = 105.7985,
            EndLat = 21.0485,  // Hướng Hoàng Quốc Việt
            Length = 1400,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        for (int i = 1; i <= 3; i++)
        {
            double ratio = i / 3.0;
            var (lng, lat) = InterpolatePoint(line2.StartLng, line2.StartLat, line2.EndLng, line2.EndLat, ratio);

            var node = new Node
            {
                Id = $"CG-{i}",
                LineId = "L2",
                Code = $"CG{i}",
                Name = $"Cống Cầu Giấy {i}",
                Description = $"Hố ga số {i} - Đường Cầu Giấy",
                Lng = lng,
                Lat = lat,
                Status = NodeStatus.Online,
                LastOnline = DateTime.UtcNow.AddMinutes(-i * 3),
                HardwareId = $"HW-CG-{i}",
                FirmwareVersion = "v2.1.3",
                IsHub = false,
                BatteryLevel = 88 - i * 7,
                RSSI = -48 - i * 4,
                CameraId = i == 2 ? "CAM-CG-2" : null,
                CreatedAt = DateTime.UtcNow
            };
            node.Sensors = CreateSensorsForNode(node.Id, i);
            line2.Nodes.Add(node);
        }

        // ========== TUYẾN 3: Đường Trần Thái Tông (Nam) ==========
        var line3 = new Line
        {
            Id = "L3",
            StationId = "ST01",
            Code = "TTT",
            Name = "Cống Trần Thái Tông",
            Description = "Tuyến cống dọc đường Trần Thái Tông",
            StartLng = hubLng,
            StartLat = hubLat,
            EndLng = 105.7945,
            EndLat = 21.0255,  // Hướng về Trung Kính
            Length = 1250,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        for (int i = 1; i <= 3; i++)
        {
            double ratio = i / 3.0;
            var (lng, lat) = InterpolatePoint(line3.StartLng, line3.StartLat, line3.EndLng, line3.EndLat, ratio);

            var node = new Node
            {
                Id = $"TTT-{i}",
                LineId = "L3",
                Code = $"TTT{i}",
                Name = $"Cống Trần Thái Tông {i}",
                Description = $"Hố ga số {i} - Đường Trần Thái Tông",
                Lng = lng,
                Lat = lat,
                Status = i == 3 ? NodeStatus.Critical : NodeStatus.Online,
                LastOnline = DateTime.UtcNow.AddMinutes(-i * 4),
                HardwareId = $"HW-TTT-{i}",
                FirmwareVersion = "v2.1.2",
                IsHub = false,
                BatteryLevel = 85 - i * 10,
                RSSI = -50 - i * 5,
                CameraId = i == 1 ? "CAM-TTT-1" : null,
                CreatedAt = DateTime.UtcNow
            };
            node.Sensors = CreateSensorsForNode(node.Id, i);
            line3.Nodes.Add(node);
        }

        // ========== TUYẾN 4: Đường Duy Tân (Tây) ==========
        var line4 = new Line
        {
            Id = "L4",
            StationId = "ST01",
            Code = "DT",
            Name = "Cống Duy Tân",
            Description = "Tuyến cống dọc đường Duy Tân",
            StartLng = hubLng,
            StartLat = hubLat,
            EndLng = 105.7855,
            EndLat = 21.0335,  // Hướng về Big C Thăng Long
            Length = 1200,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        for (int i = 1; i <= 3; i++)
        {
            double ratio = i / 3.0;
            var (lng, lat) = InterpolatePoint(line4.StartLng, line4.StartLat, line4.EndLng, line4.EndLat, ratio);

            var node = new Node
            {
                Id = $"DT-{i}",
                LineId = "L4",
                Code = $"DT{i}",
                Name = $"Cống Duy Tân {i}",
                Description = $"Hố ga số {i} - Đường Duy Tân",
                Lng = lng,
                Lat = lat,
                Status = i == 1 ? NodeStatus.Offline : NodeStatus.Online,
                LastOnline = i == 1 ? DateTime.UtcNow.AddHours(-3) : DateTime.UtcNow.AddMinutes(-i * 2),
                HardwareId = $"HW-DT-{i}",
                FirmwareVersion = "v2.0.9",
                IsHub = false,
                BatteryLevel = 78 - i * 12,
                RSSI = -55 - i * 6,
                CreatedAt = DateTime.UtcNow
            };
            node.Sensors = CreateSensorsForNode(node.Id, i);
            line4.Nodes.Add(node);
        }

        // ========== TUYẾN 5: Đường Phạm Văn Đồng (Đông Bắc - tuyến lớn) ==========
        var line5 = new Line
        {
            Id = "L5",
            StationId = "ST01",
            Code = "PVD",
            Name = "Cống Phạm Văn Đồng",
            Description = "Tuyến cống chính dọc đường Phạm Văn Đồng",
            StartLng = hubLng,
            StartLat = hubLat,
            EndLng = 105.8095,
            EndLat = 21.0525,  // Hướng về Cổ Nhuế
            Length = 2100,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        for (int i = 1; i <= 4; i++)
        {
            double ratio = i / 4.0;
            var (lng, lat) = InterpolatePoint(line5.StartLng, line5.StartLat, line5.EndLng, line5.EndLat, ratio);

            var node = new Node
            {
                Id = $"PVD-{i}",
                LineId = "L5",
                Code = $"PVD{i}",
                Name = $"Cống Phạm Văn Đồng {i}",
                Description = $"Hố ga số {i} - Đường Phạm Văn Đồng",
                Lng = lng,
                Lat = lat,
                Status = NodeStatus.Online,
                LastOnline = DateTime.UtcNow.AddMinutes(-i * 6),
                HardwareId = $"HW-PVD-{i}",
                FirmwareVersion = "v2.1.3",
                IsHub = false,
                BatteryLevel = 90 - i * 8,
                RSSI = -42 - i * 4,
                CameraId = (i == 2 || i == 4) ? $"CAM-PVD-{i}" : null,
                CreatedAt = DateTime.UtcNow
            };
            node.Sensors = CreateSensorsForNode(node.Id, i);
            line5.Nodes.Add(node);
        }

        // ========== TUYẾN 6: Đường Nguyễn Phong Sắc (Tây Nam) ==========
        var line6 = new Line
        {
            Id = "L6",
            StationId = "ST01",
            Code = "NPS",
            Name = "Cống Nguyễn Phong Sắc",
            Description = "Tuyến cống dọc đường Nguyễn Phong Sắc",
            StartLng = hubLng,
            StartLat = hubLat,
            EndLng = 105.7875,
            EndLat = 21.0285,
            Length = 1100,
            Status = "maintenance",
            CreatedAt = DateTime.UtcNow
        };

        for (int i = 1; i <= 2; i++)
        {
            double ratio = i / 2.0;
            var (lng, lat) = InterpolatePoint(line6.StartLng, line6.StartLat, line6.EndLng, line6.EndLat, ratio);

            var node = new Node
            {
                Id = $"NPS-{i}",
                LineId = "L6",
                Code = $"NPS{i}",
                Name = $"Cống Nguyễn Phong Sắc {i}",
                Description = $"Hố ga số {i} - Đường Nguyễn Phong Sắc",
                Lng = lng,
                Lat = lat,
                Status = NodeStatus.Maintenance,
                LastOnline = DateTime.UtcNow.AddHours(-1),
                HardwareId = $"HW-NPS-{i}",
                FirmwareVersion = "v2.0.5",
                IsHub = false,
                BatteryLevel = 65 - i * 15,
                RSSI = -60 - i * 5,
                CreatedAt = DateTime.UtcNow
            };
            node.Sensors = CreateSensorsForNode(node.Id, i);
            line6.Nodes.Add(node);
        }

        station.Lines = new List<Line> { hubLine, line1, line2, line3, line4, line5, line6 };
        
        return new List<Station> { station };
    }

    private static List<Sensor> CreateSensorsForNode(string nodeId, int nodeIndex)
    {
        var sensors = new List<Sensor>();
        var random = new Random(nodeId.GetHashCode());

        sensors.Add(new Sensor
        {
            Id = $"{nodeId}-RADAR",
            NodeId = nodeId,
            Type = SensorType.Radar,
            Name = "Radar biến dạng",
            Unit = "mm",
            WarningThreshold = 2.0,
            CriticalThreshold = 3.0,
            CurrentValue = 0.5 + random.NextDouble() * 2.5,
            LastReading = DateTime.UtcNow.AddSeconds(-random.Next(10, 60)),
            IsEnabled = true,
            SamplingRate = 1
        });

        sensors.Add(new Sensor
        {
            Id = $"{nodeId}-VIB",
            NodeId = nodeId,
            Type = SensorType.Vibration,
            Name = "Cảm biến rung",
            Unit = "mm/s",
            WarningThreshold = 3.0,
            CriticalThreshold = 4.0,
            CurrentValue = 0.3 + random.NextDouble() * 3.5,
            LastReading = DateTime.UtcNow.AddSeconds(-random.Next(10, 60)),
            IsEnabled = true,
            SamplingRate = 10
        });

        sensors.Add(new Sensor
        {
            Id = $"{nodeId}-TEMP",
            NodeId = nodeId,
            Type = SensorType.Temperature,
            Name = "Nhiệt độ",
            Unit = "°C",
            WarningThreshold = 35.0,
            CriticalThreshold = 45.0,
            CurrentValue = 22 + random.NextDouble() * 15,
            LastReading = DateTime.UtcNow.AddSeconds(-random.Next(10, 60)),
            IsEnabled = true,
            SamplingRate = 1
        });

        sensors.Add(new Sensor
        {
            Id = $"{nodeId}-HUM",
            NodeId = nodeId,
            Type = SensorType.Humidity,
            Name = "Độ ẩm",
            Unit = "%",
            WarningThreshold = 80.0,
            CriticalThreshold = 90.0,
            CurrentValue = 50 + random.NextDouble() * 35,
            LastReading = DateTime.UtcNow.AddSeconds(-random.Next(10, 60)),
            IsEnabled = true,
            SamplingRate = 1
        });

        if (nodeIndex % 2 == 0)
        {
            sensors.Add(new Sensor
            {
                Id = $"{nodeId}-SMOKE",
                NodeId = nodeId,
                Type = SensorType.SmokeFire,
                Name = "Cảm biến khói/lửa",
                Unit = "%",
                WarningThreshold = 30.0,
                CriticalThreshold = 50.0,
                CurrentValue = random.NextDouble() * 25,
                LastReading = DateTime.UtcNow.AddSeconds(-random.Next(10, 60)),
                IsEnabled = true,
                SamplingRate = 1
            });
        }

        sensors.Add(new Sensor
        {
            Id = $"{nodeId}-WATER",
            NodeId = nodeId,
            Type = SensorType.WaterLevel,
            Name = "Mực nước",
            Unit = "cm",
            WarningThreshold = 150.0,
            CriticalThreshold = 200.0,
            CurrentValue = 50 + random.NextDouble() * 120,
            LastReading = DateTime.UtcNow.AddSeconds(-random.Next(10, 60)),
            IsEnabled = true,
            SamplingRate = 1
        });

        if (nodeIndex % 3 == 0)
        {
            sensors.Add(new Sensor
            {
                Id = $"{nodeId}-GAS",
                NodeId = nodeId,
                Type = SensorType.Gas,
                Name = "Cảm biến khí gas",
                Unit = "ppm",
                WarningThreshold = 50.0,
                CriticalThreshold = 100.0,
                CurrentValue = random.NextDouble() * 40,
                LastReading = DateTime.UtcNow.AddSeconds(-random.Next(10, 60)),
                IsEnabled = true,
                SamplingRate = 1
            });
        }

        return sensors;
    }

    public static List<Station> Stations => GetStations();
}
