namespace SensorDashboard.Models;

public class Statistics
{
    public double Min { get; set; }
    public double Max { get; set; }
    public double Average { get; set; }
    public double StdDev { get; set; }
    public int Count { get; set; }
    public DateTime LastUpdate { get; set; }
}
