export const API_BASE = import.meta.env.VITE_API_URL ?? ''

export async function apiFetch(input: string, init?: RequestInit) {
  return fetch(`${API_BASE}${input}`, {
    credentials: 'include',
    ...init,
  })
}
