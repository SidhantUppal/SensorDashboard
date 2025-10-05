using Microsoft.AspNetCore.SignalR;
using SensorDashboard.Hubs;
using SensorDashboard.Models;

namespace SensorDashboard.Services;

public class SensorSimulatorService : BackgroundService
{
    private readonly IHubContext<SensorHub> _hubContext;
    private readonly SensorDataService _dataService;
    private readonly ILogger<SensorSimulatorService> _logger;
    private readonly Random _random = new();
    private double _baseValue = 50.0;
    private const int ReadingsPerSecond = 1000;

    public SensorSimulatorService(
        IHubContext<SensorHub> hubContext,
        SensorDataService dataService,
        ILogger<SensorSimulatorService> logger)
    {
        _hubContext = hubContext;
        _dataService = dataService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sensor Simulator Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var startTime = DateTime.UtcNow;
            var readings = new List<SensorReading>();
            
            for (int i = 0; i < ReadingsPerSecond; i++)
            {
                var reading = GenerateReading();
                _dataService.AddReading(reading);
                readings.Add(reading);
                
                var anomaly = _dataService.CheckForAnomaly(reading);
                if (anomaly != null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveAnomaly", anomaly, stoppingToken);
                }
            }
            
            await _hubContext.Clients.All.SendAsync("ReceiveBatchReadings", readings, stoppingToken);
            
            var stats = _dataService.GetStatistics();
            await _hubContext.Clients.All.SendAsync("ReceiveStatistics", stats, stoppingToken);
            
            var elapsed = DateTime.UtcNow - startTime;
            var delay = TimeSpan.FromSeconds(1) - elapsed;
            
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private SensorReading GenerateReading()
    {
        _baseValue += (_random.NextDouble() - 0.5) * 2;
        _baseValue = Math.Clamp(_baseValue, 30, 70);
        
        var noise = _random.NextDouble() * 4 - 2;
        
        if (_random.NextDouble() < 0.001)
        {
            noise += (_random.NextDouble() - 0.5) * 40;
        }
        
        return new SensorReading
        {
            Timestamp = DateTime.UtcNow,
            Value = Math.Round(_baseValue + noise, 2),
            SensorId = "SENSOR-001"
        };
    }
}
