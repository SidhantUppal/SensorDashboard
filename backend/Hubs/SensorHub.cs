using Microsoft.AspNetCore.SignalR;
using SensorDashboard.Services;

namespace SensorDashboard.Hubs;

public class SensorHub : Hub
{
    private readonly SensorDataService _dataService;
    private readonly ILogger<SensorHub> _logger;

    public SensorHub(SensorDataService dataService, ILogger<SensorHub> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        
        var recentReadings = _dataService.GetRecentReadings(1000);
        await Clients.Caller.SendAsync("ReceiveInitialData", recentReadings);
        
        var stats = _dataService.GetStatistics();
        await Clients.Caller.SendAsync("ReceiveStatistics", stats);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}
