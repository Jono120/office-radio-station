import type { NowPlaying, QueueItem } from '@/lib/types'

export type PlaceholderPlaybackTrack = {
  id: string
  provider: string
  trackName: string
  albumName?: string
  user: string
  externalLink?: string
  status: 'playing' | 'queued'
  progressMs?: number
  durationMs?: number
  isPlaying?: boolean
}

/** Demo tracks shown when nothing is queued or playing yet. */
export const PLACEHOLDER_PLAYBACK_TRACKS: PlaceholderPlaybackTrack[] = [
  {
    id: 'placeholder-youtube',
    provider: 'youtube',
    trackName: 'Holiday',
    albumName: '1814',
    user: 'office-user',
    externalLink: 'https://youtu.be/MAqZUFL2CwQ',
    status: 'playing',
    progressMs: 45_000,
    durationMs: 240_000,
    isPlaying: true,
  },
  {
    id: 'placeholder-spotify',
    provider: 'spotify',
    trackName: 'Holiday',
    albumName: '1814',
    user: 'office-user',
    externalLink: 'https://open.spotify.com/search/Holiday%201814',
    status: 'queued',
  },
  {
    id: 'placeholder-apple-music',
    provider: 'apple-music',
    trackName: 'Holiday',
    albumName: '1814',
    user: 'office-user',
    externalLink: 'https://music.apple.com/search?term=Holiday%201814',
    status: 'queued',
  },
]

export function hasRealPlaybackData(nowPlaying: NowPlaying | null, queue: QueueItem[]) {
  return Boolean(nowPlaying?.trackName) || queue.length > 0
}

export function effectivePlaybackCount(nowPlaying: NowPlaying | null, queue: QueueItem[]) {
  if (hasRealPlaybackData(nowPlaying, queue)) {
    return queue.length + (nowPlaying?.trackName ? 1 : 0)
  }
  return PLACEHOLDER_PLAYBACK_TRACKS.length
}
