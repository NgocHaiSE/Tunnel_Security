using Microsoft.AspNetCore.SignalR;
using Backend.Hubs;
using Backend.Mock;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Background service that simulates real-time sensor data updates.
/// Runs on a timer and pushes sensor values to all connected SignalR clients.
/// </summary>
public class BackgroundSensorSimulation : IDisposable
{
    private readonly IHubContext<SensorHub> _hub;
    private readonly Timer _timer;
    private readonly Random _random = new();
    private bool _isRunning;

    public BackgroundSensorSimulation(IHubContext<SensorHub> hub)
    {
        _hub = hub;
        _timer = new Timer(SimulationTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Starts the sensor simulation.
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _timer.Change(0, 1500); // Start immediately, then every 1.5 seconds
        Console.WriteLine("[BackgroundSensorSimulation] Started - pushing updates every 1.5s");
    }

    /// <summary>
    /// Stops the sensor simulation.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        Console.WriteLine("[BackgroundSensorSimulation] Stopped");
    }

    private async void SimulationTick(object? state)
    {
        if (!_isRunning) return;

        try
        {
            foreach (var station in MockData.Stations)
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
                                SensorNodeId = sensor.NodeId,
                                Type = sensor.Type.ToString(),
                                sensor.Name,
                                sensor.CurrentValue,
                                sensor.Unit,
                                sensor.LastReading,
                                NodeStatus = node.Status.ToString(),
                                NodeId = node.Id,
                                NodeName = node.Name,
                                LineId = line.Id,
                                LineName = line.Name,
                                StationId = station.Id,
                                StationName = station.Name
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BackgroundSensorSimulation] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a new sensor value using a random walk algorithm with mean reversion.
    /// </summary>
    private double GenerateRandomValue(Sensor sensor)
    {
        double current = sensor.CurrentValue ?? 0;

        // Calculate nominal value as 50% of warning threshold
        double nominal = (sensor.WarningThreshold ?? 30) * 0.5;

        // Calculate drift as 10% of critical threshold
        double drift = (sensor.CriticalThreshold ?? 50) * 0.1;

        // Revert to mean (5% per tick)
        double reversion = (nominal - current) * 0.05;

        // Random noise proportional to drift
        double noise = (_random.NextDouble() * 2 - 1) * drift;

        // Rare spike (1.5% chance) - push toward or past threshold
        if (_random.NextDouble() < 0.015)
        {
            noise += (_random.NextDouble() > 0.5 ? 1 : -1) * drift * 6;
        }

        double next = current + reversion + noise;

        // Clamp to reasonable range
        double min = 0;
        double max = sensor.CriticalThreshold.HasValue ? sensor.CriticalThreshold.Value * 1.5 : 100;
        return Math.Clamp(next, min, max);
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }
}
