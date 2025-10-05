using SensorDashboard.Hubs;
using SensorDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddSingleton<SensorDataService>();
builder.Services.AddHostedService<SensorSimulatorService>();
builder.Services.AddHostedService<DataPurgeService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.MapHub<SensorHub>("/sensorhub");

app.MapGet("/", () => "Sensor Dashboard API is running");

app.MapGet("/api/stats", (SensorDataService dataService) =>
{
    return dataService.GetStatistics();
});

app.MapGet("/api/recent", (SensorDataService dataService) =>
{
    return dataService.GetRecentReadings(1000);
});

app.Run("http://0.0.0.0:8000");
