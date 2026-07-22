import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { AlertCircle, Lock, LogOut, Speaker } from 'lucide-react'
import { Alert, Badge, Button, Card, Text, TextField, View } from 'reshaped'
import { IntegrationRow, ProviderIcon } from '@/components/settings/integration-row'
import { SettingsGroup, SettingsSection } from '@/components/settings/settings-layout'
import { useAdminSession } from '@/hooks/use-admin-session'
import { apiFetch } from '@/lib/api'
import type { Device, ProviderInfo } from '@/lib/types'
import {
  providerShowsInAccounts,
  providerSupportsDevicePlayback,
  providerSupportsManualConnection,
} from '@/lib/types'

const providerDescriptions: Record<string, string> = {
  spotify: 'Link your Spotify account to search tracks and play over Connect.',
  'apple-music': 'Link Apple Music to search the catalog and queue tracks for playback.',
  youtube: 'Create a YouTube Data API key in Google Cloud, then paste it below to connect.',
}

const connectionPlaceholders: Record<string, string> = {
  spotify: 'Paste your Spotify refresh token',
  youtube: 'Paste your YouTube Data API key',
}

export function AdminSettingsPage() {
  const { isAuthenticated, isLoading, login, logout } = useAdminSession()
  const [searchParams, setSearchParams] = useSearchParams()
  const [password, setPassword] = useState('')
  const [providers, setProviders] = useState<ProviderInfo[]>([])
  const [devices, setDevices] = useState<Device[]>([])
  const [activeProvider, setActiveProvider] = useState<string | null>(null)
  const [setupProvider, setSetupProvider] = useState<string | null>(null)
  const [connectionStrings, setConnectionStrings] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)
  const [loginError, setLoginError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isSavingConnection, setIsSavingConnection] = useState(false)

  const integrationProviders = providers.filter(providerShowsInAccounts)

  const refreshProviders = useCallback(async () => {
    const response = await apiFetch('/api/providers')
    if (response.ok) {
      setProviders(await response.json())
    }
  }, [])

  useEffect(() => {
    if (isAuthenticated) {
      void refreshProviders()
    }
  }, [isAuthenticated, refreshProviders])

  useEffect(() => {
    const authResult = searchParams.get('auth')
    if (!authResult) {
      return
    }

    const provider = searchParams.get('provider') ?? 'spotify'
    if (authResult === 'success') {
      setError(null)
      void refreshProviders()
    } else {
      const message = searchParams.get('message') ?? 'Provider connection failed.'
      setError(`${provider}: ${message}`)
    }

    setSearchParams({}, { replace: true })
  }, [refreshProviders, searchParams, setSearchParams])

  const handleLogin = async (event: React.FormEvent) => {
    event.preventDefault()
    setLoginError(null)
    setIsSubmitting(true)
    try {
      await login(password)
      setPassword('')
    } catch (err) {
      setLoginError(err instanceof Error ? err.message : 'Login failed.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const loadDevices = async (provider: string) => {
    setError(null)
    const response = await apiFetch(`/api/playback/devices?provider=${encodeURIComponent(provider)}`)
    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      setError(body.error ?? 'Failed to load devices.')
      return
    }
    setDevices(await response.json())
    setActiveProvider(provider)
    setSetupProvider(null)
  }

  const setDevice = async (provider: string, deviceId: string) => {
    setError(null)
    const response = await apiFetch('/api/playback/device', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ provider, deviceId }),
    })
    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      setError(body.error ?? 'Failed to set playback device.')
      return
    }
    await loadDevices(provider)
  }

  const disconnectProvider = async (providerId: string) => {
    setError(null)
    const response = await apiFetch(`/api/providers/${providerId}/connection`, { method: 'DELETE' })
    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      setError(body.error ?? `Failed to disconnect ${providerId}.`)
      return
    }
    if (activeProvider === providerId) {
      setActiveProvider(null)
      setDevices([])
    }
    if (setupProvider === providerId) {
      setSetupProvider(null)
    }
    await refreshProviders()
  }

  const beginConnect = async (providerId: string) => {
    setError(null)
    const response = await apiFetch(`/api/providers/${providerId}/connect-url`)
    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      setError(body.error ?? 'Failed to get provider connect URL.')
      return
    }

    const body = (await response.json()) as { url: string }
    window.open(body.url, '_blank', 'noopener,noreferrer')
    setSetupProvider(providerId)
    setConnectionStrings((current) => ({ ...current, [providerId]: '' }))
    setActiveProvider(null)
    setDevices([])
  }

  const saveConnection = async (event: React.FormEvent, providerId: string) => {
    event.preventDefault()
    const connectionString = connectionStrings[providerId]?.trim() ?? ''
    if (!connectionString) {
      return
    }

    setError(null)
    setIsSavingConnection(true)
    try {
      const response = await apiFetch(`/api/providers/${providerId}/connection`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ connectionString }),
      })
      if (!response.ok) {
        const body = await response.json().catch(() => ({}))
        setError(body.error ?? 'Failed to save connection string.')
        return
      }

      setSetupProvider(null)
      setConnectionStrings((current) => ({ ...current, [providerId]: '' }))
      await refreshProviders()
    } finally {
      setIsSavingConnection(false)
    }
  }

  if (isLoading) {
    return (
      <Text color="neutral-faded" variant="body-3">
        Checking admin session…
      </Text>
    )
  }

  if (!isAuthenticated) {
    return (
      <SettingsSection
        title="Accounts"
        description="Sign in to connect music providers and manage office playback devices."
      >
        <View attributes={{ style: { marginInline: 'auto', maxWidth: 420, width: '100%' } }}>
          <Card>
            <View gap={6}>
              <View gap={1}>
                <View align="center" direction="row" gap={2}>
                  <Lock size={16} />
                  <Text variant="body-1" weight="medium">
                    Admin sign in
                  </Text>
                </View>
                <Text color="neutral-faded" variant="body-3">
                  Provider connections are restricted to administrators.
                </Text>
              </View>
              <form onSubmit={(event) => void handleLogin(event)}>
                <View gap={4}>
                  <TextField
                    inputAttributes={{ autoComplete: 'current-password', type: 'password' }}
                    name="admin-password"
                    onChange={({ value }) => setPassword(value)}
                    placeholder="Enter admin password"
                    value={password}
                  />
                  {loginError ? (
                    <Text color="critical" variant="body-3">
                      {loginError}
                    </Text>
                  ) : null}
                  <Button disabled={isSubmitting || password.length === 0} fullWidth type="submit">
                    {isSubmitting ? 'Signing in…' : 'Sign in'}
                  </Button>
                </View>
              </form>
            </View>
          </Card>
        </View>
      </SettingsSection>
    )
  }

  return (
    <SettingsSection
      title="Accounts"
      description="Connect external services to power search, playback, and the shared office jukebox."
    >
      <View align="end">
        <Button icon={LogOut} onClick={() => void logout()} size="small" variant="outline">
          Sign out
        </Button>
      </View>

      {error ? (
        <Alert color="critical" icon={AlertCircle} title="Something went wrong">
          {error}
        </Alert>
      ) : null}

      <SettingsGroup
        description="Connect streaming services for catalog search and shared playback."
        title="Music providers"
      >
        {integrationProviders.length === 0 ? (
          <Text color="neutral-faded" variant="body-3">
            No music providers are enabled in server configuration.
          </Text>
        ) : (
          integrationProviders.map((provider) => {
            const supportsDevices = providerSupportsDevicePlayback(provider)
            const supportsManualConnection = providerSupportsManualConnection(provider)
            const isYouTube = provider.id === 'youtube'
            const isSettingUp = setupProvider === provider.id

            return (
              <IntegrationRow
                key={provider.id}
                connected={provider.isAuthenticated}
                connectedLabel={isYouTube ? 'Online' : undefined}
                description={providerDescriptions[provider.id] ?? `Connect ${provider.displayName}.`}
                disabled={!provider.enabled}
                disabledReason={provider.enabled ? undefined : 'Provider is disabled in server configuration.'}
                disconnectedLabel={isYouTube ? 'Offline' : undefined}
                expanded={activeProvider === provider.id || isSettingUp}
                icon={<ProviderIcon label={provider.displayName} providerId={provider.id} />}
                name={provider.displayName}
                onConnect={supportsManualConnection ? () => void beginConnect(provider.id) : undefined}
                onDisconnect={provider.isAuthenticated ? () => void disconnectProvider(provider.id) : undefined}
                onManage={supportsDevices ? () => void loadDevices(provider.id) : undefined}
              >
                {isSettingUp ? (
                  <form onSubmit={(event) => void saveConnection(event, provider.id)}>
                    <View gap={3}>
                      <Text color="neutral-faded" variant="body-3">
                        Complete setup in the new tab, then paste your connection string here to save it.
                      </Text>
                      <TextField
                        inputAttributes={{ autoComplete: 'off', type: 'password' }}
                        name={`connection-${provider.id}`}
                        onChange={({ value }) =>
                          setConnectionStrings((current) => ({
                            ...current,
                            [provider.id]: value,
                          }))
                        }
                        placeholder={connectionPlaceholders[provider.id] ?? 'Paste connection string'}
                        value={connectionStrings[provider.id] ?? ''}
                      />
                      <View direction="row" gap={2} wrap>
                        <Button
                          disabled={isSavingConnection || !connectionStrings[provider.id]?.trim()}
                          size="small"
                          type="submit"
                        >
                          {isSavingConnection ? 'Saving…' : 'Save connection'}
                        </Button>
                        <Button onClick={() => setSetupProvider(null)} size="small" type="button" variant="ghost">
                          Cancel
                        </Button>
                      </View>
                    </View>
                  </form>
                ) : null}
                {devices.map((device) => (
                  <Card key={device.id} padding={3}>
                    <View align="center" direction="row" gap={3} justify="space-between">
                      <View align="center" direction="row" gap={2}>
                        <Speaker size={16} />
                        <Text variant="body-3">{device.name}</Text>
                        {device.isActive ? <Badge color="positive">Active</Badge> : null}
                      </View>
                      <Button
                        disabled={device.isActive}
                        onClick={() => void setDevice(device.provider, device.id)}
                        size="small"
                        variant={device.isActive ? 'outline' : 'solid'}
                      >
                        Use
                      </Button>
                    </View>
                  </Card>
                ))}
              </IntegrationRow>
            )
          })
        )}
      </SettingsGroup>
    </SettingsSection>
  )
}
