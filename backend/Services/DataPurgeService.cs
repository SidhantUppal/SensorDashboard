namespace SensorDashboard.Services;

public class DataPurgeService : BackgroundService
{
    private readonly SensorDataService _dataService;
    private readonly ILogger<DataPurgeService> _logger;
    private const int PurgeIntervalMinutes = 5;

    public DataPurgeService(
        SensorDataService dataService,
        ILogger<DataPurgeService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Purge Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(PurgeIntervalMinutes), stoppingToken);
            
            _dataService.PurgeOldData(TimeSpan.FromHours(24));
            
            _logger.LogInformation("Purged data older than 24 hours");
        }
    }
}
