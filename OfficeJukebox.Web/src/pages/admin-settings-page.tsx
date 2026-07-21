import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { AlertCircle, Lock, LogOut, Speaker } from 'lucide-react'
import { Status, StatusIndicator, StatusLabel } from '@/components/kibo-ui/status'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAdminSession } from '@/hooks/use-admin-session'
import { apiFetch } from '@/lib/api'
import type { Device, ProviderInfo } from '@/lib/types'
import { providerRequiresAuth } from '@/lib/types'

export function AdminSettingsPage() {
  const { isAuthenticated, isLoading, login, logout } = useAdminSession()
  const [searchParams, setSearchParams] = useSearchParams()
  const [password, setPassword] = useState('')
  const [providers, setProviders] = useState<ProviderInfo[]>([])
  const [devices, setDevices] = useState<Device[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loginError, setLoginError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const adminProviders = providers.filter(providerRequiresAuth)

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
    await refreshProviders()
  }

  const connectProvider = (providerId: string) => {
    window.location.href = `/api/providers/${providerId}/auth`
  }

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Checking admin session…</p>
  }

  if (!isAuthenticated) {
    return (
      <Card className="mx-auto w-full max-w-md">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Lock className="size-4" />
            Admin sign in
          </CardTitle>
          <CardDescription>
            Provider connections and playback devices are restricted to administrators.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={(event) => void handleLogin(event)}>
            <div className="space-y-2">
              <Label htmlFor="admin-password">Admin password</Label>
              <Input
                id="admin-password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter admin password"
              />
            </div>
            {loginError ? <p className="text-sm text-destructive">{loginError}</p> : null}
            <Button className="w-full" type="submit" disabled={isSubmitting || password.length === 0}>
              {isSubmitting ? 'Signing in…' : 'Sign in'}
            </Button>
          </form>
        </CardContent>
      </Card>
    )
  }

  return (
    <>
      <div className="flex items-center justify-between gap-4">
        <div>
          <h2 className="text-lg font-semibold">Music provider settings</h2>
          <p className="text-sm text-muted-foreground">
            Connect Spotify, Apple Music, or YouTube and choose the office playback device.
          </p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={() => void logout()}>
          <LogOut className="size-4" />
          Sign out
        </Button>
      </div>

      {error ? (
        <Alert variant="destructive">
          <AlertCircle />
          <AlertTitle>Something went wrong</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>Provider connections</CardTitle>
          <CardDescription>OAuth connections shared by the whole office jukebox.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {adminProviders.length === 0 ? (
            <p className="text-sm text-muted-foreground">No OAuth providers are enabled in server configuration.</p>
          ) : (
            adminProviders.map((provider) => (
              <div
                key={provider.id}
                className="flex flex-col gap-3 rounded-xl border p-4 sm:flex-row sm:items-center sm:justify-between"
              >
                <div className="space-y-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="font-medium">{provider.displayName}</p>
                    <Status status={provider.isAuthenticated ? 'online' : 'offline'}>
                      <StatusIndicator />
                      <StatusLabel>
                        {provider.isAuthenticated ? 'Connected' : 'Not connected'}
                      </StatusLabel>
                    </Status>
                  </div>
                  {!provider.enabled ? (
                    <p className="text-sm text-muted-foreground">Provider is disabled in server configuration.</p>
                  ) : null}
                </div>
                <div className="flex flex-wrap gap-2">
                  <Button type="button" size="sm" onClick={() => connectProvider(provider.id)} disabled={!provider.enabled}>
                    Connect
                  </Button>
                  {provider.isAuthenticated ? (
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      onClick={() => void disconnectProvider(provider.id)}
                    >
                      Disconnect
                    </Button>
                  ) : null}
                  <Button
                    type="button"
                    size="sm"
                    variant="secondary"
                    onClick={() => void loadDevices(provider.id)}
                    disabled={!provider.isAuthenticated}
                  >
                    Devices
                  </Button>
                </div>
              </div>
            ))
          )}
        </CardContent>
      </Card>

      {devices.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>Playback devices</CardTitle>
            <CardDescription>Select which speaker or Connect device plays office music.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            {devices.map((device) => (
              <div
                key={device.id}
                className="flex items-center justify-between gap-3 rounded-lg border px-3 py-2"
              >
                <div className="flex items-center gap-2 text-sm">
                  <Speaker className="size-4 text-muted-foreground" />
                  <span>{device.name}</span>
                  {device.isActive ? <Badge>Active</Badge> : null}
                </div>
                <Button
                  type="button"
                  size="sm"
                  variant={device.isActive ? 'secondary' : 'default'}
                  disabled={device.isActive}
                  onClick={() => void setDevice(device.provider, device.id)}
                >
                  Use
                </Button>
              </div>
            ))}
          </CardContent>
        </Card>
      ) : null}
    </>
  )
}
