import { useCallback, useEffect, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { Avatar, Badge, Button, Card, Link, Text, TextField, View } from 'reshaped'
import {
  SettingsField,
  SettingsGroup,
  SettingsSection,
} from '@/components/settings/settings-layout'
import { ThemeSelection } from '@/components/settings/theme-selection'
import { useProfile } from '@/hooks/use-profile'
import { apiFetch } from '@/lib/api'
import type { QueueItem } from '@/lib/types'

function queueBadgeColor(status: string): 'positive' | 'primary' | 'neutral' {
  switch (status.toLowerCase()) {
    case 'playing':
      return 'positive'
    case 'queued':
      return 'primary'
    default:
      return 'neutral'
  }
}

export function ProfilePage() {
  const { username, setUsername, initials } = useProfile()
  const [draftUsername, setDraftUsername] = useState(username)
  const [queue, setQueue] = useState<QueueItem[]>([])
  const [saved, setSaved] = useState(false)

  const refreshQueue = useCallback(async () => {
    const response = await apiFetch('/api/queue')
    if (response.ok) {
      setQueue(await response.json())
    }
  }, [])

  useEffect(() => {
    void refreshQueue()
  }, [refreshQueue])

  useEffect(() => {
    setDraftUsername(username)
  }, [username])

  const myQueueItems = queue.filter(
    (item) => item.user.localeCompare(username, undefined, { sensitivity: 'accent' }) === 0,
  )

  const handleSave = (event: React.FormEvent) => {
    event.preventDefault()
    const trimmed = draftUsername.trim()
    if (!trimmed) {
      return
    }

    setUsername(trimmed)
    setSaved(true)
    window.setTimeout(() => setSaved(false), 2000)
  }

  return (
    <SettingsSection
      title="Profile"
      description="Manage your personal information and how you appear in the office queue."
    >
      <SettingsGroup title="Profile picture" description="How teammates recognize you in the jukebox.">
        <Card>
          <View align="center" direction="row" gap={4}>
            <Avatar initials={initials} size={16} />
            <View gap={1}>
              <Text variant="body-3" weight="medium">
                {username}
              </Text>
              <Text color="neutral-faded" variant="body-3">
                Tracks you queue are attributed to this name for everyone in the office.
              </Text>
            </View>
          </View>
        </Card>
      </SettingsGroup>

      <SettingsGroup title="Personal information" description="Update the name shown when you queue music.">
        <Card padding={0}>
          <form onSubmit={handleSave}>
            <SettingsField description="Unique name used when adding tracks to the shared queue." label="User name">
              <TextField
                name="username"
                onChange={({ value }) => setDraftUsername(value)}
                placeholder="office-user"
                value={draftUsername}
              />
            </SettingsField>
            <View align="center" direction="row" gap={3} padding={4}>
              <Button disabled={draftUsername.trim().length === 0} type="submit">
                Save changes
              </Button>
              {saved ? (
                <Text color="neutral-faded" variant="body-3">
                  Profile updated.
                </Text>
              ) : null}
            </View>
          </form>
        </Card>
      </SettingsGroup>

      <SettingsGroup title="Theme" description="Choose how OfficeJukebox looks on this device.">
        <ThemeSelection />
      </SettingsGroup>

      <SettingsGroup
        title="Queue activity"
        description={
          myQueueItems.length === 0
            ? 'You have no tracks in the queue right now.'
            : `${myQueueItems.length} track${myQueueItems.length === 1 ? '' : 's'} queued under your name.`
        }
      >
        {myQueueItems.length === 0 ? (
          <Text color="neutral-faded" variant="body-3">
            Head back to the{' '}
            <RouterLink style={{ color: 'inherit' }} to="/">
              <Link color="inherit" variant="plain">
                jukebox
              </Link>
            </RouterLink>{' '}
            to search and queue something.
          </Text>
        ) : (
          <View gap={2}>
            {myQueueItems.map((item) => (
              <Card key={item.id}>
                <View align="center" direction="row" gap={3} justify="space-between">
                  <View>
                    <Text maxLines={1} variant="body-3" weight="medium">
                      {item.trackName}
                    </Text>
                    <Text color="neutral-faded" maxLines={1} variant="caption-1">
                      {item.albumName ? `${item.albumName} · ` : ''}
                      {item.provider}
                    </Text>
                  </View>
                  <Badge color={queueBadgeColor(item.status)}>{item.status}</Badge>
                </View>
              </Card>
            ))}
          </View>
        )}
      </SettingsGroup>
    </SettingsSection>
  )
}
