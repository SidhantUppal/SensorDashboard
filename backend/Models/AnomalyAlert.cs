namespace SensorDashboard.Models;

public class AnomalyAlert
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
}
