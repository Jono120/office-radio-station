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

export const DEFAULT_SEARCH_PROVIDER = 'spotify'

export function pickDefaultSearchProvider(providers: ProviderInfo[]) {
  return (
    providers.find((provider) => provider.id === DEFAULT_SEARCH_PROVIDER)?.id ??
    providers[0]?.id ??
    DEFAULT_SEARCH_PROVIDER
  )
}

export function sortSearchProviders(providers: ProviderInfo[]) {
  return [...providers].sort((a, b) => {
    if (a.id === DEFAULT_SEARCH_PROVIDER) {
      return -1
    }
    if (b.id === DEFAULT_SEARCH_PROVIDER) {
      return 1
    }
    return a.displayName.localeCompare(b.displayName)
  })
}

export function providerRequiresAuth(provider: ProviderInfo) {
  return provider.capabilities.includes('RequiresAuth')
}

export function providerShowsInAccounts(provider: ProviderInfo) {
  return provider.id !== 'manual' && provider.capabilities.includes('Search')
}

export function providerSupportsDevicePlayback(provider: ProviderInfo) {
  return provider.capabilities.includes('DevicePlayback')
}

export function providerSupportsManualConnection(provider: ProviderInfo) {
  return provider.id === 'spotify' || provider.id === 'youtube'
}

export function providerIsSearchable(provider: ProviderInfo) {
  return provider.enabled && provider.capabilities.includes('Search') && provider.isAuthenticated
}
