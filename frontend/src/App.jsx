import { useState, useEffect, useRef } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { Line } from 'react-chartjs-2'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  TimeScale
} from 'chart.js'
import 'chartjs-adapter-date-fns'
import './App.css'

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  TimeScale
)

function App() {
  const [connection, setConnection] = useState(null)
  const [connected, setConnected] = useState(false)
  const [readings, setReadings] = useState([])
  const [stats, setStats] = useState(null)
  const [alerts, setAlerts] = useState([])
  const maxDataPoints = 1000

  useEffect(() => {
    let apiUrl
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
      apiUrl = 'http://localhost:8000'
    } else {
      const parts = window.location.hostname.split('.')
      parts[0] = parts[0] + '-8000'
      apiUrl = `https://${parts.join('.')}`
    }
    
    console.log('API URL:', apiUrl)

    const newConnection = new HubConnectionBuilder()
      .withUrl(`${apiUrl}/sensorhub`)
      .withAutomaticReconnect()
      .build()

    setConnection(newConnection)
  }, [])

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('Connected to SignalR hub')
          setConnected(true)

          connection.on('ReceiveInitialData', (initialData) => {
            console.log('Received initial data:', initialData.length, 'readings')
            setReadings(initialData.slice(-maxDataPoints))
          })

          connection.on('ReceiveBatchReadings', (newReadings) => {
            setReadings(prev => {
              const combined = [...prev, ...newReadings]
              return combined.slice(-maxDataPoints)
            })
          })

          connection.on('ReceiveStatistics', (newStats) => {
            setStats(newStats)
          })

          connection.on('ReceiveAnomaly', (alert) => {
            setAlerts(prev => [alert, ...prev].slice(0, 10))
          })
        })
        .catch(err => console.error('Connection failed:', err))

      return () => {
        connection.stop()
      }
    }
  }, [connection])

  const chartData = {
    labels: readings.map(r => new Date(r.timestamp)),
    datasets: [
      {
        label: 'Sensor Value',
        data: readings.map(r => r.value),
        borderColor: 'rgb(59, 130, 246)',
        backgroundColor: 'rgba(59, 130, 246, 0.1)',
        borderWidth: 1.5,
        pointRadius: 0,
        tension: 0.1
      }
    ]
  }

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    plugins: {
      legend: {
        display: false
      },
      title: {
        display: false
      }
    },
    scales: {
      x: {
        type: 'time',
        time: {
          unit: 'second',
          displayFormats: {
            second: 'HH:mm:ss'
          }
        },
        ticks: {
          maxTicksLimit: 10
        }
      },
      y: {
        beginAtZero: false
      }
    }
  }

  return (
    <div className="app">
      <header className="header">
        <h1>Real-Time Sensor Analytics Dashboard</h1>
        <div className="status">
          <span className={`status-dot ${connected ? 'connected' : 'disconnected'}`}></span>
          <span>{connected ? 'Connected' : 'Disconnected'}</span>
        </div>
      </header>

      <div className="dashboard">
        <div className="stats-grid">
          <div className="stat-card">
            <div className="stat-label">Min Value</div>
            <div className="stat-value">{stats?.min?.toFixed(2) || '--'}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Max Value</div>
            <div className="stat-value">{stats?.max?.toFixed(2) || '--'}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Average</div>
            <div className="stat-value">{stats?.average?.toFixed(2) || '--'}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Std Dev</div>
            <div className="stat-value">{stats?.stdDev?.toFixed(2) || '--'}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Data Points</div>
            <div className="stat-value">{stats?.count?.toLocaleString() || '--'}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Current Value</div>
            <div className="stat-value">{readings[readings.length - 1]?.value?.toFixed(2) || '--'}</div>
          </div>
        </div>

        <div className="chart-container">
          <h2>Live Sensor Readings</h2>
          <div className="chart">
            <Line data={chartData} options={chartOptions} />
          </div>
        </div>

        <div className="alerts-container">
          <h2>Anomaly Alerts</h2>
          <div className="alerts-list">
            {alerts.length === 0 ? (
              <div className="no-alerts">No anomalies detected</div>
            ) : (
              alerts.map((alert, idx) => (
                <div key={idx} className={`alert-item ${alert.severity.toLowerCase()}`}>
                  <div className="alert-time">
                    {new Date(alert.timestamp).toLocaleTimeString()}
                  </div>
                  <div className="alert-message">{alert.message}</div>
                  <div className="alert-badge">{alert.severity}</div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

export default App
