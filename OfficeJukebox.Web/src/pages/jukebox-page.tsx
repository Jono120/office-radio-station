import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import * as signalR from '@microsoft/signalr'
import { AlertCircle, ListMusic, Music2, Search, Speaker } from 'lucide-react'
import {
  Alert,
  Badge,
  Button,
  Card,
  Link,
  Select,
  Text,
  TextField,
  View,
} from 'reshaped'
import { PlaybackQueueTable } from '@/components/playback-queue-table'
import { useProfile } from '@/hooks/use-profile'
import { apiFetch } from '@/lib/api'
import { formatMs } from '@/lib/format'
import { effectivePlaybackCount } from '@/lib/placeholder-tracks'
import type { NowPlaying, ProviderInfo, QueueItem, SearchResult } from '@/lib/types'
import {
  DEFAULT_SEARCH_PROVIDER,
  pickDefaultSearchProvider,
  providerIsSearchable,
  sortSearchProviders,
} from '@/lib/types'

export function JukeboxPage() {
  const { username } = useProfile()
  const [queue, setQueue] = useState<QueueItem[]>([])
  const [nowPlaying, setNowPlaying] = useState<NowPlaying | null>(null)
  const [providers, setProviders] = useState<ProviderInfo[]>([])
  const [selectedProvider, setSelectedProvider] = useState(DEFAULT_SEARCH_PROVIDER)
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<SearchResult[]>([])
  const [error, setError] = useState<string | null>(null)

  const searchableProviders = useMemo(
    () => sortSearchProviders(providers.filter(providerIsSearchable)),
    [providers],
  )
  const canSearch = searchableProviders.length > 0
  const playbackCount = effectivePlaybackCount(nowPlaying, queue)

  const connection = useMemo(
    () =>
      new signalR.HubConnectionBuilder()
        .withUrl('/hubs/queue')
        .withAutomaticReconnect()
        .build(),
    [],
  )

  const refreshQueue = useCallback(async () => {
    const response = await apiFetch('/api/queue')
    if (response.ok) {
      setQueue(await response.json())
    }
  }, [])

  const refreshNowPlaying = useCallback(async () => {
    const response = await apiFetch('/api/playback/now-playing')
    if (response.ok) {
      setNowPlaying(await response.json())
    }
  }, [])

  const refreshProviders = useCallback(async () => {
    const response = await apiFetch('/api/providers')
    if (response.ok) {
      const nextProviders: ProviderInfo[] = await response.json()
      setProviders(nextProviders)
      const available = nextProviders.filter(providerIsSearchable)
      if (available.length > 0 && !available.some((p) => p.id === selectedProvider)) {
        setSelectedProvider(pickDefaultSearchProvider(available))
      }
    }
  }, [selectedProvider])

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
    if (!canSearch || searchQuery.trim().length === 0) {
      return
    }

    setError(null)
    const response = await apiFetch(
      `/api/search?q=${encodeURIComponent(searchQuery)}&provider=${encodeURIComponent(selectedProvider)}`,
    )
    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      setError(body.error ?? 'Search failed for selected provider.')
      return
    }
    setSearchResults(await response.json())
  }

  const queueTrack = async (result: SearchResult) => {
    setError(null)
    const response = await apiFetch('/api/queue', {
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

  return (
    <View gap={10}>
      {error ? (
        <Alert color="critical" icon={AlertCircle} title="Something went wrong">
          {error}
        </Alert>
      ) : null}

      <Card padding={0}>
        <View
          attributes={{ style: { borderBottom: '1px solid var(--rs-color-border-neutral-faded)' } }}
          gap={2}
          padding={6}
        >
          <View align="center" direction="row" gap={2}>
            <Search size={20} />
            <Text variant="body-1" weight="medium">
              Search tracks
            </Text>
          </View>
          <Text color="neutral-faded" variant="body-3">
            Find music from a connected provider and add it to the office queue.
          </Text>
        </View>

        <View gap={6} padding={6}>
          <View align="center" direction={{ s: 'column', l: 'row' }} gap={4}>
            <View attributes={{ style: { minWidth: 192, width: '100%' } }}>
              <Select
                disabled={!canSearch}
                name="provider"
                onChange={({ value }) => setSelectedProvider(value)}
                placeholder="Provider"
                value={canSearch ? selectedProvider : ''}
              >
                {searchableProviders.map((provider) => (
                  <Select.Option key={provider.id} value={provider.id}>
                    {provider.displayName}
                  </Select.Option>
                ))}
              </Select>
            </View>

            <View.Item grow>
              <TextField
                disabled={!canSearch}
                icon={Search}
                inputAttributes={{
                  onKeyDown: (event: React.KeyboardEvent<HTMLInputElement>) => {
                    if (event.key === 'Enter') {
                      void runSearch()
                    }
                  },
                }}
                name="search"
                onChange={({ value }) => setSearchQuery(value)}
                placeholder="Search for artists, albums, or tracks…"
                value={searchQuery}
              />
            </View.Item>

            <Button disabled={!canSearch || searchQuery.trim().length === 0} onClick={() => void runSearch()}>
              Search
            </Button>
          </View>

          {!canSearch ? (
            <Text color="neutral-faded" variant="body-3">
              No music providers are connected yet. Ask an admin to connect Spotify, Apple Music, or YouTube in{' '}
              <RouterLink style={{ color: 'inherit' }} to="/settings/accounts">
                <Link color="inherit" variant="plain">
                  settings
                </Link>
              </RouterLink>
              .
            </Text>
          ) : searchResults.length > 0 ? (
            <Card padding={0}>
              {searchResults.map((result, index) => (
                <View
                  key={`${result.provider}:${result.externalId}`}
                  align="center"
                  attributes={{
                    style: {
                      borderBottom:
                        index < searchResults.length - 1
                          ? '1px solid var(--rs-color-border-neutral-faded)'
                          : undefined,
                    },
                  }}
                  direction="row"
                  gap={4}
                  justify="space-between"
                  padding={4}
                >
                  <View align="center" direction="row" gap={4}>
                    {result.artworkUrl ? (
                      <img
                        alt=""
                        src={result.artworkUrl}
                        style={{ borderRadius: 8, height: 48, objectFit: 'cover', width: 48 }}
                      />
                    ) : (
                      <View
                        align="center"
                        attributes={{
                          style: {
                            background: 'var(--rs-color-background-neutral-faded)',
                            borderRadius: 8,
                            height: 48,
                            width: 48,
                          },
                        }}
                        justify="center"
                      >
                        <Music2 size={20} />
                      </View>
                    )}
                    <View>
                      <Text maxLines={1} variant="body-3" weight="medium">
                        {result.name}
                      </Text>
                      <Text color="neutral-faded" maxLines={1} variant="caption-1">
                        {result.albumName ?? result.provider} · {formatMs(result.durationMs)}
                      </Text>
                    </View>
                  </View>
                  <Button onClick={() => void queueTrack(result)} size="small" variant="outline">
                    Queue
                  </Button>
                </View>
              ))}
            </Card>
          ) : (
            <Text color="neutral-faded" variant="body-3">
              Search results will appear here.
            </Text>
          )}
        </View>
      </Card>

      <Card padding={0}>
        <View
          attributes={{ style: { borderBottom: '1px solid var(--rs-color-border-neutral-faded)' } }}
          gap={2}
          padding={6}
        >
          <View align="center" direction="row" gap={2} justify="space-between">
            <View align="center" direction="row" gap={2}>
              <ListMusic size={20} />
              <Text variant="body-1" weight="medium">
                Now playing
              </Text>
            </View>
            {nowPlaying?.deviceName ? (
              <Badge color="neutral" icon={Speaker}>
                {nowPlaying.deviceName}
              </Badge>
            ) : null}
          </View>
          <Text color="neutral-faded" variant="body-3">
            {playbackCount === 0
              ? 'Nothing queued yet.'
              : `${playbackCount} track${playbackCount === 1 ? '' : 's'} in playback.`}
          </Text>
        </View>
        <View padding={6}>
          <PlaybackQueueTable nowPlaying={nowPlaying} queue={queue} />
        </View>
      </Card>
    </View>
  )
}
