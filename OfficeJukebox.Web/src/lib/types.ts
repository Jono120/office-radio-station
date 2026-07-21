export type QueueItem = {
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

export type NowPlaying = {
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

export type SearchResult = {
  provider: string
  externalId: string
  name: string
  albumName?: string
  artworkUrl?: string
  durationMs: number
  externalLink?: string
}

export type ProviderInfo = {
  id: string
  displayName: string
  enabled: boolean
  isAuthenticated: boolean
  capabilities: string[]
}

export type Device = {
  id: string
  name: string
  provider: string
  isActive: boolean
}

export function providerRequiresAuth(provider: ProviderInfo) {
  return provider.capabilities.includes('RequiresAuth')
}

export function providerIsSearchable(provider: ProviderInfo) {
  return provider.enabled && provider.capabilities.includes('Search') && (
    provider.isAuthenticated || !providerRequiresAuth(provider)
  )
}
