import { useCallback, useState } from 'react'

const STORAGE_KEY = 'officejukebox-username'
const DEFAULT_USERNAME = 'office-user'

function readUsername() {
  return localStorage.getItem(STORAGE_KEY) ?? DEFAULT_USERNAME
}

export function getProfileInitials(username: string) {
  const trimmed = username.trim()
  if (!trimmed) {
    return '?'
  }

  const parts = trimmed.split(/[\s._-]+/).filter(Boolean)
  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
  }

  return trimmed.slice(0, 2).toUpperCase()
}

export function useProfile() {
  const [username, setUsernameState] = useState(readUsername)

  const setUsername = useCallback((next: string) => {
    setUsernameState(next)
    localStorage.setItem(STORAGE_KEY, next)
  }, [])

  return {
    username,
    setUsername,
    initials: getProfileInitials(username),
  }
}
