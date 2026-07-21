import { useCallback, useEffect, useMemo, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import './App.css'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

type QueueItem = {
  id: string
  user: string
  provider: string
  externalId: string
  trackName: string
  albumName?: string
  externalLink?: string
  reason?: string
  status: string
}

type NowPlaying = {
  id?: string
  user?: string
  provider?: string
  externalId?: string
  trackName?: string
  albumName?: string
  artworkUrl?: string
  progressMs?: number
  durationMs?: number
  isPlaying: boolean
  deviceName?: string
}

type SearchResult = {
  provider: string
  externalId: string
  name: string
  albumName?: string
  artworkUrl?: string
  durationMs: number
  externalLink?: string
}

type ProviderInfo = {
  id: string
  displayName: string
  enabled: boolean
  isAuthenticated: boolean
  capabilities: string[]
}

function App() {
  const [username, setUsername] = useState('office-user')
  const [queue, setQueue] = useState<QueueItem[]>([])
  const [nowPlaying, setNowPlaying] = useState<NowPlaying | null>(null)
  const [providers, setProviders] = useState<ProviderInfo[]>([])
  const [selectedProvider, setSelectedProvider] = useState('spotify')
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<SearchResult[]>([])
  const [devices, setDevices] = useState<{ id: string; name: string; provider: string; isActive: boolean }[]>([])
  const [error, setError] = useState<string | null>(null)

  const connection = useMemo(
    () =>
      new signalR.HubConnectionBuilder()
        .withUrl(`${API_BASE}/hubs/queue`)
        .withAutomaticReconnect()
        .build(),
    [],
  )

  const refreshQueue = useCallback(async () => {
    const response = await fetch(`${API_BASE}/api/queue`)
    if (response.ok) {
      setQueue(await response.json())
    }
  }, [])

  const refreshNowPlaying = useCallback(async () => {
    const response = await fetch(`${API_BASE}/api/playback/now-playing`)
    if (response.ok) {
      setNowPlaying(await response.json())
    }
  }, [])

  const refreshProviders = useCallback(async () => {
    const response = await fetch(`${API_BASE}/api/providers`)
    if (response.ok) {
      setProviders(await response.json())
    }
  }, [])

  useEffect(() => {
    void refreshQueue()
    void refreshNowPlaying()
    void refreshProviders()
  }, [refreshQueue, refreshNowPlaying, refreshProviders])

  useEffect(() => {
    connection.on('QueueChanged', () => void refreshQueue())
    connection.on('NowPlayingChanged', () => void refreshNowPlaying())
    connection.on('PlaybackProgress', (payload: { progressMs: number; durationMs: number; isPlaying: boolean }) => {
      setNowPlaying((current) =>
        current
          ? { ...current, progressMs: payload.progressMs, durationMs: payload.durationMs, isPlaying: payload.isPlaying }
          : current,
      )
    })
    void connection.start()
    return () => {
      void connection.stop()
    }
  }, [connection, refreshQueue, refreshNowPlaying])

  const runSearch = async () => {
    setError(null)
    const response = await fetch(
      `${API_BASE}/api/search?q=${encodeURIComponent(searchQuery)}&provider=${encodeURIComponent(selectedProvider)}`,
    )
    if (!response.ok) {
      setError('Search failed for selected provider.')
      return
    }
    setSearchResults(await response.json())
  }

  const queueTrack = async (result: SearchResult) => {
    setError(null)
    const response = await fetch(`${API_BASE}/api/queue`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        user: username,
        provider: result.provider,
        externalId: result.externalId,
        trackName: result.name,
        albumName: result.albumName,
        externalLink: result.externalLink,
      }),
    })
    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      setError(body.errors?.join(', ') ?? body.error ?? 'Failed to queue track.')
      return
    }
    await refreshQueue()
  }

  const loadDevices = async (provider: string) => {
    const response = await fetch(`${API_BASE}/api/playback/devices?provider=${encodeURIComponent(provider)}`)
    if (response.ok) {
      setDevices(await response.json())
    }
  }

  const setDevice = async (provider: string, deviceId: string) => {
    await fetch(`${API_BASE}/api/playback/device`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ provider, deviceId }),
    })
    await loadDevices(provider)
  }

  return (
    <main className="app">
      <header>
        <h1>OfficeJukebox</h1>
        <p>Multi-provider office music queue</p>
      </header>

      <section className="panel">
        <label>
          Username
          <input value={username} onChange={(e) => setUsername(e.target.value)} />
        </label>
      </section>

      <section className="panel now-playing">
        <h2>Now Playing</h2>
        {nowPlaying?.trackName ? (
          <div className="now-playing-card">
            {nowPlaying.artworkUrl ? <img src={nowPlaying.artworkUrl} alt="" /> : null}
            <div>
              <strong>{nowPlaying.trackName}</strong>
              <div>{nowPlaying.albumName}</div>
              <div>{nowPlaying.deviceName}</div>
              <div>{nowPlaying.isPlaying ? 'Playing' : 'Paused'}</div>
            </div>
          </div>
        ) : (
          <p>Nothing playing</p>
        )}
      </section>

      <section className="panel">
        <h2>Search</h2>
        <div className="row">
          <select value={selectedProvider} onChange={(e) => setSelectedProvider(e.target.value)}>
            {providers.map((p) => (
              <option key={p.id} value={p.id}>
                {p.displayName}
              </option>
            ))}
          </select>
          <input value={searchQuery} onChange={(e) => setSearchQuery(e.target.value)} placeholder="Search tracks" />
          <button type="button" onClick={() => void runSearch()}>
            Search
          </button>
        </div>
        <ul className="results">
          {searchResults.map((result) => (
            <li key={`${result.provider}:${result.externalId}`}>
              <span>
                {result.name} — {result.albumName}
              </span>
              <button type="button" onClick={() => void queueTrack(result)}>
                Queue
              </button>
            </li>
          ))}
        </ul>
      </section>

      <section className="panel">
        <h2>Queue</h2>
        <ul className="queue">
          {queue.map((item) => (
            <li key={item.id}>
              <span>
                {item.trackName} ({item.provider}) — {item.user}
              </span>
              <span className="status">{item.status}</span>
            </li>
          ))}
        </ul>
      </section>

      <section className="panel">
        <h2>Providers</h2>
        <ul>
          {providers.map((provider) => (
            <li key={provider.id} className="provider-row">
              <span>
                {provider.displayName} — {provider.isAuthenticated ? 'connected' : 'not connected'}
              </span>
              <button type="button" onClick={() => window.open(`${API_BASE}/api/providers/${provider.id}/auth`, '_blank')}>
                Connect
              </button>
              <button type="button" onClick={() => void loadDevices(provider.id)}>
                Devices
              </button>
            </li>
          ))}
        </ul>
        <ul>
          {devices.map((device) => (
            <li key={device.id}>
              {device.name} {device.isActive ? '(active)' : ''}
              <button type="button" onClick={() => void setDevice(device.provider, device.id)}>
                Use
              </button>
            </li>
          ))}
        </ul>
      </section>

      {error ? <p className="error">{error}</p> : null}
    </main>
  )
}

export default App
