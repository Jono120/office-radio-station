import { Fragment, useMemo, useState } from 'react'
import {
  Check,
  ChevronDown,
  Circle,
  CircleDashed,
  Minus,
  Pause,
  Play,
  SignalHigh,
  SignalLow,
  SignalMedium,
} from 'lucide-react'
import {
  Avatar,
  Button,
  Card,
  Checkbox,
  ProgressBar,
  Table,
  Text,
  View,
} from 'reshaped'
import { getProfileInitials } from '@/hooks/use-profile'
import { formatMs } from '@/lib/format'
import { PLACEHOLDER_PLAYBACK_TRACKS } from '@/lib/placeholder-tracks'
import type { NowPlaying, QueueItem } from '@/lib/types'

type PlaybackRow = {
  key: string
  id: string
  trackName: string
  albumName?: string
  user: string
  provider: string
  status: 'playing' | 'paused' | 'queued'
  artworkUrl?: string
  externalLink?: string
  progressMs?: number
  durationMs?: number
  isPlaying?: boolean
  position: number
}

type PlaybackQueueTableProps = {
  nowPlaying: NowPlaying | null
  queue: QueueItem[]
}

function formatQueueId(id: string, position: number) {
  const compact = id.replace(/-/g, '').slice(0, 4).toUpperCase()
  if (compact.length > 0) {
    return `Q-${compact}`
  }
  return `#${String(position).padStart(2, '0')}`
}

function PriorityIcon({ status, position }: { status: PlaybackRow['status']; position: number }) {
  if (status === 'playing' || status === 'paused') {
    return <SignalHigh size={16} />
  }
  if (position === 2) {
    return <SignalMedium size={16} />
  }
  if (position === 3) {
    return <SignalLow size={16} />
  }
  return <Minus size={16} />
}

function TrackStatusIcon({ status }: { status: PlaybackRow['status'] }) {
  if (status === 'playing') {
    return <Check size={12} strokeWidth={3} />
  }
  if (status === 'paused') {
    return <Pause size={12} />
  }
  return <CircleDashed size={14} />
}

function buildPlaceholderRows(): PlaybackRow[] {
  return PLACEHOLDER_PLAYBACK_TRACKS.map((track, index) => ({
    key: track.id,
    id: track.id,
    trackName: track.trackName,
    albumName: track.albumName,
    user: track.user,
    provider: track.provider,
    status: track.status,
    externalLink: track.externalLink,
    progressMs: track.progressMs,
    durationMs: track.durationMs,
    isPlaying: track.isPlaying,
    position: index + 1,
  }))
}

function buildRows(nowPlaying: NowPlaying | null, queue: QueueItem[]): PlaybackRow[] {
  const rows: PlaybackRow[] = []
  let position = 1

  if (nowPlaying?.trackName) {
    rows.push({
      key: nowPlaying.id ?? 'now-playing',
      id: nowPlaying.id ?? 'now-playing',
      trackName: nowPlaying.trackName,
      albumName: nowPlaying.albumName,
      user: nowPlaying.user ?? 'office',
      provider: nowPlaying.provider ?? '',
      status: nowPlaying.isPlaying ? 'playing' : 'paused',
      artworkUrl: nowPlaying.artworkUrl,
      progressMs: nowPlaying.progressMs,
      durationMs: nowPlaying.durationMs,
      isPlaying: nowPlaying.isPlaying,
      position,
    })
    position += 1
  }

  for (const item of queue) {
    rows.push({
      key: item.id,
      id: item.id,
      trackName: item.trackName,
      albumName: item.albumName,
      user: item.user,
      provider: item.provider,
      status: item.status.toLowerCase() === 'playing' ? 'playing' : 'queued',
      position,
    })
    position += 1
  }

  if (rows.length === 0) {
    return buildPlaceholderRows()
  }

  return rows
}

export function PlaybackQueueTable({ nowPlaying, queue }: PlaybackQueueTableProps) {
  const rows = useMemo(() => buildRows(nowPlaying, queue), [nowPlaying, queue])
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [expanded, setExpanded] = useState<string | null>(null)

  const allSelected = rows.length > 0 && selected.size === rows.length

  const toggleAll = () => {
    if (allSelected) {
      setSelected(new Set())
      return
    }
    setSelected(new Set(rows.map((row) => row.key)))
  }

  if (rows.length === 0) {
    return (
      <View
        align="center"
        attributes={{
          style: {
            border: '1px dashed var(--rs-color-border-neutral-faded)',
            borderRadius: 12,
          },
        }}
        gap={1}
        padding={10}
      >
        <Text variant="body-3" weight="medium">
          Nothing in the queue
        </Text>
        <Text color="neutral-faded" variant="body-3">
          Search for a track above to start playback.
        </Text>
      </View>
    )
  }

  return (
    <Card padding={0}>
      <Table border>
        <Table.Row highlighted>
          <Table.Heading width="auto">
            <Checkbox
              checked={allSelected}
              indeterminate={selected.size > 0 && selected.size < rows.length}
              inputAttributes={{ 'aria-label': 'Select all tracks' }}
              name="select-all"
              onChange={toggleAll}
            />
          </Table.Heading>
          <Table.Heading>Priority</Table.Heading>
          <Table.Heading>ID</Table.Heading>
          <Table.Heading>Track</Table.Heading>
          <Table.Heading>Added by</Table.Heading>
          <Table.Heading width="auto" />
        </Table.Row>

        {rows.map((row) => {
          const isExpanded = expanded === row.key
          const progressPercent =
            row.durationMs && row.progressMs !== undefined
              ? Math.min(100, (row.progressMs / row.durationMs) * 100)
              : 0
          const isSelected = selected.has(row.key)

          return (
            <Fragment key={row.key}>
              <Table.Row highlighted={isSelected || row.status === 'playing'}>
                <Table.Cell>
                  <Checkbox
                    checked={isSelected}
                    inputAttributes={{ 'aria-label': `Select ${row.trackName}` }}
                    name="row"
                    onChange={() => {
                      setSelected((current) => {
                        const next = new Set(current)
                        if (next.has(row.key)) {
                          next.delete(row.key)
                        } else {
                          next.add(row.key)
                        }
                        return next
                      })
                    }}
                  />
                </Table.Cell>
                <Table.Cell>
                  <PriorityIcon position={row.position} status={row.status} />
                </Table.Cell>
                <Table.Cell>
                  <Text color="neutral-faded" variant="caption-1">
                    {formatQueueId(row.id, row.position)}
                  </Text>
                </Table.Cell>
                <Table.Cell>
                  <View align="center" direction="row" gap={3}>
                    <TrackStatusIcon status={row.status} />
                    <View>
                      <Text maxLines={1} variant="body-3" weight="medium">
                        {row.trackName}
                      </Text>
                      <Text color="neutral-faded" maxLines={1} variant="caption-1">
                        {row.albumName ? `${row.albumName} · ` : ''}
                        {row.provider || 'Unknown provider'}
                      </Text>
                    </View>
                  </View>
                </Table.Cell>
                <Table.Cell>
                  <View align="center" direction="row" gap={2}>
                    <Avatar initials={getProfileInitials(row.user)} size={7} />
                    <Text maxLines={1} variant="body-3">
                      {row.user}
                    </Text>
                  </View>
                </Table.Cell>
                <Table.Cell align="end">
                  <Button
                    attributes={{ 'aria-expanded': isExpanded }}
                    icon={ChevronDown}
                    onClick={() => setExpanded(isExpanded ? null : row.key)}
                    size="small"
                    variant="ghost"
                  />
                </Table.Cell>
              </Table.Row>

              {isExpanded ? (
                <Table.Row>
                  <Table.Cell colSpan={6}>
                    <View direction={{ s: 'column', m: 'row' }} gap={4}>
                      {row.artworkUrl ? (
                        <img
                          alt={row.albumName ?? row.trackName}
                          src={row.artworkUrl}
                          style={{ borderRadius: 8, height: 80, objectFit: 'cover', width: 80 }}
                        />
                      ) : (
                        <View
                          align="center"
                          attributes={{
                            style: {
                              background: 'var(--rs-color-background-neutral-faded)',
                              borderRadius: 8,
                              height: 80,
                              width: 80,
                            },
                          }}
                          justify="center"
                        >
                          <Circle size={24} />
                        </View>
                      )}
                      <View gap={2}>
                        {row.status === 'playing' || row.status === 'paused' ? (
                          <>
                            <View align="center" direction="row" gap={2}>
                              {row.isPlaying ? <Play size={12} /> : <Pause size={12} />}
                              <Text color="neutral-faded" variant="caption-1">
                                {row.isPlaying ? 'Now playing' : 'Paused'}
                              </Text>
                            </View>
                            <ProgressBar max={100} value={progressPercent} />
                            <Text color="neutral-faded" variant="caption-1">
                              {formatMs(row.progressMs)} / {formatMs(row.durationMs)}
                            </Text>
                          </>
                        ) : (
                          <Text color="neutral-faded" variant="caption-1">
                            Position {row.position} in queue
                          </Text>
                        )}
                        {row.externalLink ? (
                          <Text color="neutral-faded" variant="caption-1">
                            <a href={row.externalLink} rel="noopener noreferrer" target="_blank">
                              Open in {row.provider}
                            </a>
                          </Text>
                        ) : null}
                      </View>
                    </View>
                  </Table.Cell>
                </Table.Row>
              ) : null}
            </Fragment>
          )
        })}
      </Table>
    </Card>
  )
}
