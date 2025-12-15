using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Backend.Hubs;
using Backend.Mock;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly IHubContext<SensorHub> _hub;

    public SensorsController(IHubContext<SensorHub> hub)
    {
        _hub = hub;
    }

    public class MeasurementRequest
    {
        public double Value { get; set; }
    }

    // POST /api/sensors/{id}/measurements
    [HttpPost("{id}/measurements")]
    public async Task<IActionResult> UpdateSensor(
        string id, [FromBody] MeasurementRequest req)
    {
        // Tìm sensor trong tất cả nodes
        Sensor? sensor = null;
        Node? parentNode = null;

        foreach (var station in MockData.Stations)
        {
            foreach (var line in station.Lines)
            {
                foreach (var node in line.Nodes)
                {
                    sensor = node.Sensors.FirstOrDefault(s => s.Id == id);
                    if (sensor != null)
                    {
                        parentNode = node;
                        break;
                    }
                }
                if (sensor != null) break;
            }
            if (sensor != null) break;
        }

        if (sensor == null) return NotFound("Sensor not found");

        // Cập nhật giá trị
        sensor.CurrentValue = req.Value;
        sensor.LastReading = DateTime.UtcNow;

        // Cập nhật trạng thái node dựa vào sensor values
        if (parentNode != null)
        {
            UpdateNodeStatus(parentNode);
        }

        // Gửi realtime cho client
        await _hub.Clients.All.SendAsync("SensorUpdated", new
        {
            sensor.Id,
            sensor.NodeId,
            sensor.Type,
            sensor.Name,
            sensor.CurrentValue,
            sensor.Unit,
            sensor.LastReading,
            NodeStatus = parentNode?.Status
        });

        return Ok(new { ok = true, sensor });
    }

    // GET /api/sensors/{id}
    [HttpGet("{id}")]
    public IActionResult GetSensor(string id)
    {
        foreach (var station in MockData.Stations)
        {
            foreach (var line in station.Lines)
            {
                foreach (var node in line.Nodes)
                {
                    var sensor = node.Sensors.FirstOrDefault(s => s.Id == id);
                    if (sensor != null)
                    {
                        return Ok(sensor);
                    }
                }
            }
        }

        return NotFound("Sensor not found");
    }

    private void UpdateNodeStatus(Node node)
    {
        // Kiểm tra nếu có sensor critical
        var hasCritical = node.Sensors.Any(s => 
            s.CurrentValue.HasValue && 
            s.CriticalThreshold.HasValue && 
            s.CurrentValue.Value >= s.CriticalThreshold.Value);

        if (hasCritical)
        {
            node.Status = NodeStatus.Critical;
            return;
        }

        // Kiểm tra nếu có sensor warning
        var hasWarning = node.Sensors.Any(s => 
            s.CurrentValue.HasValue && 
            s.WarningThreshold.HasValue && 
            s.CurrentValue.Value >= s.WarningThreshold.Value);

        if (hasWarning)
        {
            node.Status = NodeStatus.Warning;
            return;
        }

        // Bình thường
        node.Status = NodeStatus.Online;
    }
}
