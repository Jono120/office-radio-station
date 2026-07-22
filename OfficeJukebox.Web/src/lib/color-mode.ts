export const COLOR_MODE_PREFERENCE_KEY = 'officejukebox-color-mode-preference'
export const RESHAPED_COLOR_MODE_KEY = '__reshaped-mode'

export type ColorModePreference = 'system' | 'light' | 'dark'

export function readColorModePreference(): ColorModePreference {
  const stored = localStorage.getItem(COLOR_MODE_PREFERENCE_KEY)
  if (stored === 'system' || stored === 'light' || stored === 'dark') {
    return stored
  }
  return 'system'
}

export function getSystemColorMode(): 'light' | 'dark' {
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export function resolveColorMode(preference: ColorModePreference): 'light' | 'dark' {
  if (preference === 'system') {
    return getSystemColorMode()
  }
  return preference
}

export function applyDocumentColorMode(mode: 'light' | 'dark') {
  document.documentElement.setAttribute('data-rs-theme', 'slate')
  document.documentElement.setAttribute('data-rs-color-mode', mode)
  localStorage.setItem(RESHAPED_COLOR_MODE_KEY, mode)
}

export function applyColorModePreference(preference: ColorModePreference) {
  localStorage.setItem(COLOR_MODE_PREFERENCE_KEY, preference)
  applyDocumentColorMode(resolveColorMode(preference))
}
