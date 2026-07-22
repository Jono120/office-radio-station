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
  const { profile, isLoading, isSignedIn, initials, signIn, signOut } = useProfile()
  const [draftEmail, setDraftEmail] = useState('')
  const [draftDisplayName, setDraftDisplayName] = useState('')
  const [queue, setQueue] = useState<QueueItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const refreshQueue = useCallback(async () => {
    const response = await apiFetch('/api/queue')
    if (response.ok) {
      setQueue(await response.json())
    }
  }, [])

  useEffect(() => {
    void refreshQueue()
  }, [refreshQueue])

  const myQueueItems = profile
    ? queue.filter((item) => item.user.toLowerCase() === profile.email.toLowerCase())
    : []

  const handleSignIn = async (event: React.FormEvent) => {
    event.preventDefault()
    const email = draftEmail.trim()
    if (!email) {
      return
    }

    setError(null)
    setIsSubmitting(true)
    try {
      await signIn(email, draftDisplayName.trim())
      setDraftEmail('')
      setDraftDisplayName('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign-in failed.')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return (
      <Text color="neutral-faded" variant="body-3">
        Checking session…
      </Text>
    )
  }

  return (
    <SettingsSection
      title="Profile"
      description="Sign in with your work email to queue tracks and vote. Your identity is verified server-side."
    >
      {isSignedIn && profile ? (
        <SettingsGroup title="Signed in" description="Tracks you queue and votes you cast are attributed to this account.">
          <Card>
            <View align="center" direction="row" gap={4} justify="space-between">
              <View align="center" direction="row" gap={4}>
                <Avatar initials={initials} size={16} />
                <View gap={1}>
                  <Text variant="body-3" weight="medium">
                    {profile.displayName}
                  </Text>
                  <Text color="neutral-faded" variant="body-3">
                    {profile.email}
                  </Text>
                </View>
              </View>
              <Button onClick={() => void signOut()} variant="outline">
                Sign out
              </Button>
            </View>
          </Card>
        </SettingsGroup>
      ) : (
        <SettingsGroup
          title="Sign in"
          description="Only company work emails are accepted. Queueing and voting are unavailable until you sign in."
        >
          <Card padding={0}>
            <form onSubmit={handleSignIn}>
              <SettingsField description="Your company email — the domain is validated." label="Work email">
                <TextField
                  inputAttributes={{ type: 'email' }}
                  name="email"
                  onChange={({ value }) => setDraftEmail(value)}
                  placeholder="you@company.com"
                  value={draftEmail}
                />
              </SettingsField>
              <SettingsField description="Shown next to the tracks you queue (optional)." label="Display name">
                <TextField
                  name="displayName"
                  onChange={({ value }) => setDraftDisplayName(value)}
                  placeholder="Your name"
                  value={draftDisplayName}
                />
              </SettingsField>
              <View align="center" direction="row" gap={3} padding={4}>
                <Button disabled={draftEmail.trim().length === 0 || isSubmitting} type="submit">
                  Sign in
                </Button>
                {error ? (
                  <Text color="critical" variant="body-3">
                    {error}
                  </Text>
                ) : null}
              </View>
            </form>
          </Card>
        </SettingsGroup>
      )}

      <SettingsGroup title="Theme" description="Choose how OfficeJukebox looks on this device.">
        <ThemeSelection />
      </SettingsGroup>

      <SettingsGroup
        title="Queue activity"
        description={
          !isSignedIn
            ? 'Sign in to see the tracks queued under your account.'
            : myQueueItems.length === 0
              ? 'You have no tracks in the queue right now.'
              : `${myQueueItems.length} track${myQueueItems.length === 1 ? '' : 's'} queued under your account.`
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
