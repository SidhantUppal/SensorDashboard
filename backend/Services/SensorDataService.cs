using SensorDashboard.Models;

namespace SensorDashboard.Services;

public class SensorDataService
{
    private readonly SensorReading[] _circularBuffer;
    private readonly object _statsLock = new();
    private int _writeIndex = 0;
    private int _count = 0;
    private const int MaxDataPoints = 100000;
    private const double AnomalyThresholdStdDev = 3.0;
    
    private double _sum = 0;
    private double _sumOfSquares = 0;
    private double _min = double.MaxValue;
    private double _max = double.MinValue;
    
    private Statistics _currentStats = new()
    {
        Min = double.MaxValue,
        Max = double.MinValue,
        Average = 0,
        StdDev = 0,
        Count = 0,
        LastUpdate = DateTime.UtcNow
    };

    public SensorDataService()
    {
        _circularBuffer = new SensorReading[MaxDataPoints];
    }

    public void AddReading(SensorReading reading)
    {
        lock (_statsLock)
        {
            SensorReading? oldReading = null;
            
            if (_count >= MaxDataPoints)
            {
                oldReading = _circularBuffer[_writeIndex];
            }
            
            _circularBuffer[_writeIndex] = reading;
            _writeIndex = (_writeIndex + 1) % MaxDataPoints;
            
            if (_count < MaxDataPoints)
            {
                _count++;
            }
            
            UpdateStatisticsIncremental(reading, oldReading);
        }
    }

    public IEnumerable<SensorReading> GetRecentReadings(int count = 1000)
    {
        lock (_statsLock)
        {
            if (_count == 0) return Enumerable.Empty<SensorReading>();
            
            var actualCount = Math.Min(count, _count);
            var result = new SensorReading[actualCount];
            
            var startIndex = (_writeIndex - actualCount + MaxDataPoints) % MaxDataPoints;
            
            for (int i = 0; i < actualCount; i++)
            {
                var index = (startIndex + i) % MaxDataPoints;
                result[i] = _circularBuffer[index];
            }
            
            return result;
        }
    }

    public Statistics GetStatistics()
    {
        lock (_statsLock)
        {
            return _currentStats;
        }
    }

    public AnomalyAlert? CheckForAnomaly(SensorReading reading)
    {
        lock (_statsLock)
        {
            if (_currentStats.Count < 100 || _currentStats.StdDev == 0) return null;
            
            var deviation = Math.Abs(reading.Value - _currentStats.Average);
            
            if (deviation > AnomalyThresholdStdDev * _currentStats.StdDev)
            {
                return new AnomalyAlert
                {
                    Timestamp = reading.Timestamp,
                    Value = reading.Value,
                    Message = $"Value {reading.Value:F2} deviates {deviation / _currentStats.StdDev:F2} standard deviations from mean",
                    Severity = deviation > 5 * _currentStats.StdDev ? "Critical" : "Warning"
                };
            }
            
            return null;
        }
    }

    public void PurgeOldData(TimeSpan maxAge)
    {
        lock (_statsLock)
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            var purgedCount = 0;
            
            while (_count > 0)
            {
                var oldestIndex = (_writeIndex - _count + MaxDataPoints) % MaxDataPoints;
                var oldest = _circularBuffer[oldestIndex];
                
                if (oldest.Timestamp >= cutoffTime) break;
                
                _sum -= oldest.Value;
                _sumOfSquares -= oldest.Value * oldest.Value;
                _count--;
                purgedCount++;
            }
            
            if (purgedCount > 0)
            {
                RecalculateMinMax();
                UpdateCurrentStats();
            }
        }
    }

    private void UpdateStatisticsIncremental(SensorReading newReading, SensorReading? oldReading)
    {
        if (oldReading != null)
        {
            _sum -= oldReading.Value;
            _sumOfSquares -= oldReading.Value * oldReading.Value;
        }
        
        _sum += newReading.Value;
        _sumOfSquares += newReading.Value * newReading.Value;
        
        if (newReading.Value < _min) _min = newReading.Value;
        if (newReading.Value > _max) _max = newReading.Value;
        
        if (oldReading != null && (oldReading.Value == _min || oldReading.Value == _max))
        {
            RecalculateMinMax();
        }
        
        UpdateCurrentStats();
    }

    private void RecalculateMinMax()
    {
        if (_count == 0)
        {
            _min = double.MaxValue;
            _max = double.MinValue;
            return;
        }
        
        _min = double.MaxValue;
        _max = double.MinValue;
        
        for (int i = 0; i < _count; i++)
        {
            var index = (_writeIndex - _count + i + MaxDataPoints) % MaxDataPoints;
            var value = _circularBuffer[index].Value;
            if (value < _min) _min = value;
            if (value > _max) _max = value;
        }
    }

    private void UpdateCurrentStats()
    {
        _currentStats.Count = _count;
        _currentStats.LastUpdate = DateTime.UtcNow;
        
        if (_count == 0)
        {
            _currentStats.Min = 0;
            _currentStats.Max = 0;
            _currentStats.Average = 0;
            _currentStats.StdDev = 0;
            return;
        }
        
        _currentStats.Min = _min;
        _currentStats.Max = _max;
        _currentStats.Average = _sum / _count;
        
        if (_count > 1)
        {
            var variance = (_sumOfSquares / _count) - (_currentStats.Average * _currentStats.Average);
            _currentStats.StdDev = Math.Sqrt(Math.Max(0, variance));
        }
        else
        {
            _currentStats.StdDev = 0;
        }
    }
}
