import { useCallback, useEffect, useMemo, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import {
  AlertCircle,
  ListMusic,
  Music2,
  Pause,
  Play,
  Search,
  Speaker,
} from 'lucide-react'
import { ImageZoom } from '@/components/kibo-ui/image-zoom'
import { ListItems } from '@/components/kibo-ui/list'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Progress, ProgressValue } from '@/components/ui/progress'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { apiFetch } from '@/lib/api'
import { formatMs, queueStatusVariant } from '@/lib/format'
import type { NowPlaying, ProviderInfo, QueueItem, SearchResult } from '@/lib/types'
import { providerIsSearchable } from '@/lib/types'
import { useProfile } from '@/hooks/use-profile'

export function JukeboxPage() {
  const { username } = useProfile()
  const [queue, setQueue] = useState<QueueItem[]>([])
  const [nowPlaying, setNowPlaying] = useState<NowPlaying | null>(null)
  const [providers, setProviders] = useState<ProviderInfo[]>([])
  const [selectedProvider, setSelectedProvider] = useState('spotify')
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<SearchResult[]>([])
  const [error, setError] = useState<string | null>(null)

  const searchableProviders = useMemo(
    () => providers.filter(providerIsSearchable),
    [providers],
  )

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
        setSelectedProvider(available[0].id)
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

  const progressPercent =
    nowPlaying?.durationMs && nowPlaying.progressMs !== undefined
      ? Math.min(100, (nowPlaying.progressMs / nowPlaying.durationMs) * 100)
      : 0

  return (
    <>
      {error ? (
        <Alert variant="destructive">
          <AlertCircle />
          <AlertTitle>Something went wrong</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Music2 className="size-4" />
            Now playing
          </CardTitle>
          <CardDescription>Live playback from the office output device.</CardDescription>
          {nowPlaying?.deviceName ? (
            <CardAction>
              <Badge variant="outline" className="gap-1">
                <Speaker className="size-3" />
                {nowPlaying.deviceName}
              </Badge>
            </CardAction>
          ) : null}
        </CardHeader>
        <CardContent>
          {nowPlaying?.trackName ? (
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
              {nowPlaying.artworkUrl ? (
                <ImageZoom className="shrink-0 overflow-hidden rounded-xl">
                  <img
                    src={nowPlaying.artworkUrl}
                    alt={nowPlaying.albumName ?? nowPlaying.trackName}
                    className="size-28 rounded-xl object-cover"
                  />
                </ImageZoom>
              ) : (
                <div className="flex size-28 shrink-0 items-center justify-center rounded-xl bg-muted">
                  <Music2 className="size-10 text-muted-foreground" />
                </div>
              )}
              <div className="min-w-0 flex-1 space-y-3">
                <div>
                  <p className="truncate text-lg font-semibold">{nowPlaying.trackName}</p>
                  {nowPlaying.albumName ? (
                    <p className="truncate text-sm text-muted-foreground">{nowPlaying.albumName}</p>
                  ) : null}
                </div>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  {nowPlaying.isPlaying ? (
                    <>
                      <Play className="size-3.5" />
                      Playing
                    </>
                  ) : (
                    <>
                      <Pause className="size-3.5" />
                      Paused
                    </>
                  )}
                </div>
                <div className="space-y-1">
                  <Progress value={progressPercent}>
                    <ProgressValue>
                      {() => `${formatMs(nowPlaying.progressMs)} / ${formatMs(nowPlaying.durationMs)}`}
                    </ProgressValue>
                  </Progress>
                </div>
              </div>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">Nothing playing yet. Queue a track to get started.</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Search className="size-4" />
            Search tracks
          </CardTitle>
          <CardDescription>Find music from a connected provider and add it to the queue.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {searchableProviders.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No music providers are connected yet. Ask an admin to connect Spotify, Apple Music, or YouTube in settings.
            </p>
          ) : (
            <>
              <div className="flex flex-col gap-3 sm:flex-row">
                <Select
                  value={selectedProvider}
                  onValueChange={(value) => {
                    if (value) setSelectedProvider(value)
                  }}
                >
                  <SelectTrigger className="w-full sm:w-44">
                    <SelectValue placeholder="Provider" />
                  </SelectTrigger>
                  <SelectContent>
                    {searchableProviders.map((provider) => (
                      <SelectItem key={provider.id} value={provider.id}>
                        {provider.displayName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Input
                  className="flex-1"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      void runSearch()
                    }
                  }}
                  placeholder="Search tracks"
                />
                <Button type="button" onClick={() => void runSearch()}>
                  Search
                </Button>
              </div>

              {searchResults.length > 0 ? (
                <ListItems className="rounded-xl border bg-muted/30 p-0">
                  {searchResults.map((result) => (
                    <div
                      key={`${result.provider}:${result.externalId}`}
                      className="flex items-center justify-between gap-3 border-b px-4 py-3 last:border-b-0"
                    >
                      <div className="flex min-w-0 items-center gap-3">
                        {result.artworkUrl ? (
                          <img
                            src={result.artworkUrl}
                            alt=""
                            className="size-10 shrink-0 rounded-md object-cover"
                          />
                        ) : (
                          <div className="flex size-10 shrink-0 items-center justify-center rounded-md bg-muted">
                            <Music2 className="size-4 text-muted-foreground" />
                          </div>
                        )}
                        <div className="min-w-0 text-left">
                          <p className="truncate font-medium">{result.name}</p>
                          <p className="truncate text-sm text-muted-foreground">
                            {result.albumName ?? result.provider} · {formatMs(result.durationMs)}
                          </p>
                        </div>
                      </div>
                      <Button type="button" size="sm" variant="secondary" onClick={() => void queueTrack(result)}>
                        Queue
                      </Button>
                    </div>
                  ))}
                </ListItems>
              ) : (
                <p className="text-sm text-muted-foreground">Search results will appear here.</p>
              )}
            </>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ListMusic className="size-4" />
            Queue
          </CardTitle>
          <CardDescription>
            {queue.length === 0 ? 'No tracks queued.' : `${queue.length} track${queue.length === 1 ? '' : 's'} waiting.`}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {queue.length > 0 ? (
            <ScrollArea className="h-72 rounded-xl border">
              <ListItems className="p-0">
                {queue.map((item) => (
                  <div
                    key={item.id}
                    className="flex items-center justify-between gap-3 border-b px-4 py-3 last:border-b-0"
                  >
                    <div className="min-w-0 text-left">
                      <p className="truncate font-medium">{item.trackName}</p>
                      <p className="truncate text-sm text-muted-foreground">
                        {item.albumName ? `${item.albumName} · ` : ''}
                        {item.provider} · {item.user}
                      </p>
                    </div>
                    <Badge variant={queueStatusVariant(item.status)}>{item.status}</Badge>
                  </div>
                ))}
              </ListItems>
            </ScrollArea>
          ) : (
            <p className="text-sm text-muted-foreground">The queue is empty.</p>
          )}
        </CardContent>
      </Card>
    </>
  )
}
