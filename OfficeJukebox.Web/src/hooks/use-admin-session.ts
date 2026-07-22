import {
  createContext,
  createElement,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { apiFetch } from '@/lib/api'

async function readErrorMessage(response: Response) {
  const body = await response.json().catch(() => ({}))
  if (typeof body.error === 'string' && body.error.length > 0) {
    return body.error
  }

  if (response.status === 404) {
    return 'Admin login API was not found. Restart the OfficeJukebox.Api project and try again.'
  }

  if (response.status === 503) {
    return 'Admin access is not configured. Set Admin:Password in OfficeJukebox.Api appsettings.'
  }

  if (response.status === 401) {
    return 'Invalid admin password.'
  }

  return `Login failed (${response.status}).`
}

type AdminSessionContextValue = {
  isAuthenticated: boolean
  isLoading: boolean
  login: (password: string) => Promise<void>
  logout: () => Promise<void>
  refresh: () => Promise<void>
}

const AdminSessionContext = createContext<AdminSessionContextValue | null>(null)

export function AdminSessionProvider({ children }: { children: ReactNode }) {
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

  const login = useCallback(async (password: string) => {
    let response: Response
    try {
      response = await apiFetch('/api/admin/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ password }),
      })
    } catch {
      throw new Error('Could not reach the API. Make sure OfficeJukebox.Api is running on port 5080.')
    }

    if (!response.ok) {
      throw new Error(await readErrorMessage(response))
    }

    setIsAuthenticated(true)
  }, [])

  const logout = useCallback(async () => {
    await apiFetch('/api/admin/logout', { method: 'POST' })
    setIsAuthenticated(false)
  }, [])

  const value = useMemo(
    () => ({ isAuthenticated, isLoading, login, logout, refresh }),
    [isAuthenticated, isLoading, login, logout, refresh],
  )

  return createElement(AdminSessionContext.Provider, { value }, children)
}

export function useAdminSession() {
  const context = useContext(AdminSessionContext)
  if (!context) {
    throw new Error('useAdminSession must be used within an AdminSessionProvider')
  }

  return context
}
