import { useCallback, useEffect, useState } from 'react'
import { apiFetch } from '@/lib/api'

export type Profile = {
  email: string
  displayName: string
}

export function getProfileInitials(name: string) {
  const trimmed = name.trim()
  if (!trimmed) {
    return '?'
  }

  // For emails, derive initials from the local part (jane.doe@x → JD).
  const base = trimmed.includes('@') ? trimmed.split('@')[0] : trimmed
  const parts = base.split(/[\s._-]+/).filter(Boolean)
  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
  }

  return base.slice(0, 2).toUpperCase()
}

/**
 * Session-backed identity (server-side cookie session, not localStorage).
 * The Api derives the queueing/voting user from this session; the frontend
 * only displays it.
 */
export function useProfile() {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const response = await apiFetch('/api/session')
        if (!cancelled && response.ok) {
          setProfile(await response.json())
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const signIn = useCallback(async (email: string, displayName: string) => {
    const response = await apiFetch('/api/session', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, displayName }),
    })
    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      throw new Error(body.error ?? 'Sign-in failed.')
    }
    const next: Profile = await response.json()
    setProfile(next)
    return next
  }, [])

  const signOut = useCallback(async () => {
    await apiFetch('/api/session', { method: 'DELETE' })
    setProfile(null)
  }, [])

  return {
    profile,
    isLoading,
    isSignedIn: profile !== null,
    initials: getProfileInitials(profile?.displayName ?? profile?.email ?? ''),
    signIn,
    signOut,
  }
}
