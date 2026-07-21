import { useCallback, useEffect, useState } from 'react'
import { apiFetch } from '@/lib/api'

export function useAdminSession() {
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isLoading, setIsLoading] = useState(true)

  const refresh = useCallback(async () => {
    setIsLoading(true)
    try {
      const response = await apiFetch('/api/admin/me')
      setIsAuthenticated(response.ok)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh])

  const login = async (password: string) => {
    const response = await apiFetch('/api/admin/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ password }),
    })
    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      throw new Error(body.error ?? 'Login failed.')
    }
    setIsAuthenticated(true)
  }

  const logout = async () => {
    await apiFetch('/api/admin/logout', { method: 'POST' })
    setIsAuthenticated(false)
  }

  return { isAuthenticated, isLoading, login, logout, refresh }
}
