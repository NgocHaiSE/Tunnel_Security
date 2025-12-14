using Microsoft.AspNetCore.Mvc;
using Backend.Mock;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationsController : ControllerBase
{
    // GET /api/stations
    [HttpGet]
    public IActionResult GetStations()
        => Ok(MockData.Stations);

    // GET /api/stations/{id}
    [HttpGet("{id}")]
    public IActionResult GetStation(string id)
    {
        var s = MockData.Stations.FirstOrDefault(x => x.Id == id);
        return s == null ? NotFound() : Ok(s);
    }

    // GET /api/stations/{id}/lines
    [HttpGet("{id}/lines")]
    public IActionResult GetLines(string id)
    {
        var station = MockData.Stations.FirstOrDefault(x => x.Id == id);
        if (station == null) return NotFound();
        
        return Ok(station.Lines);
    }

    // GET /api/stations/{id}/nodes (GeoJSON) - Tất cả nodes trong trạm
    [HttpGet("{id}/nodes")]
    public IActionResult GetNodes(string id)
    {
        var station = MockData.Stations.FirstOrDefault(x => x.Id == id);
        if (station == null) return NotFound();

        var allNodes = station.Lines.SelectMany(l => l.Nodes).ToList();

        var geo = new
        {
            type = "FeatureCollection",
            features = allNodes.Select(node => new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Point",
                    coordinates = new[] { node.Lng, node.Lat }
                },
                properties = new
                {
                    node.Id,
                    node.Code,
                    node.Name,
                    node.LineId,
                    Line = station.Lines.FirstOrDefault(l => l.Id == node.LineId)?.Name,
                    node.Status,
                    node.IsHub,
                    node.BatteryLevel,
                    node.RSSI,
                    node.LastOnline,
                    SensorCount = node.Sensors.Count,
                    node.CameraId
                }
            })
        };

        return Ok(geo);
    }

    // GET /api/stations/{id}/lines-geojson (GeoJSON) - Các tuyến dạng LineString
    [HttpGet("{id}/lines-geojson")]
    public IActionResult GetLinesGeoJson(string id)
    {
        var station = MockData.Stations.FirstOrDefault(x => x.Id == id);
        if (station == null) return NotFound();

        var geo = new
        {
            type = "FeatureCollection",
            features = station.Lines.Select(line => new
            {
                type = "Feature",
                geometry = new
                {
                    type = "LineString",
                    coordinates = new[] {
                        new[] { line.StartLng, line.StartLat },
                        new[] { line.EndLng, line.EndLat }
                    }
                },
                properties = new
                {
                    line.Id,
                    line.Code,
                    line.Name,
                    line.Description,
                    line.Status,
                    line.Length,
                    NodeCount = line.Nodes.Count
                }
            })
        };

        return Ok(geo);
    }

    // GET /api/stations/{stationId}/lines/{lineId}/nodes
    [HttpGet("{stationId}/lines/{lineId}/nodes")]
    public IActionResult GetLineNodes(string stationId, string lineId)
    {
        var station = MockData.Stations.FirstOrDefault(x => x.Id == stationId);
        if (station == null) return NotFound("Station not found");

        var line = station.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line == null) return NotFound("Line not found");

        return Ok(line.Nodes);
    }

    // GET /api/stations/{stationId}/nodes/{nodeId}
    [HttpGet("{stationId}/nodes/{nodeId}")]
    public IActionResult GetNodeDetail(string stationId, string nodeId)
    {
        var station = MockData.Stations.FirstOrDefault(x => x.Id == stationId);
        if (station == null) return NotFound("Station not found");

        var node = station.Lines
            .SelectMany(l => l.Nodes)
            .FirstOrDefault(n => n.Id == nodeId);
            
        if (node == null) return NotFound("Node not found");

        return Ok(node);
    }

    // GET /api/stations/{stationId}/nodes/{nodeId}/sensors
    [HttpGet("{stationId}/nodes/{nodeId}/sensors")]
    public IActionResult GetNodeSensors(string stationId, string nodeId)
    {
        var station = MockData.Stations.FirstOrDefault(x => x.Id == stationId);
        if (station == null) return NotFound("Station not found");

        var node = station.Lines
            .SelectMany(l => l.Nodes)
            .FirstOrDefault(n => n.Id == nodeId);
            
        if (node == null) return NotFound("Node not found");

        return Ok(node.Sensors);
    }
}
