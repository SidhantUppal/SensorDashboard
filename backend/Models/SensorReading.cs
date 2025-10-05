namespace SensorDashboard.Models;

public class SensorReading
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string SensorId { get; set; } = "SENSOR-001";
}
