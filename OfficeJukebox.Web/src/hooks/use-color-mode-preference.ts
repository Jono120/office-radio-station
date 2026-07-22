import { useCallback, useEffect, useState } from 'react'
import { useTheme } from 'reshaped'
import {
  applyColorModePreference,
  applyDocumentColorMode,
  getSystemColorMode,
  readColorModePreference,
  resolveColorMode,
  type ColorModePreference,
} from '@/lib/color-mode'

export function useColorModePreference() {
  const { setColorMode } = useTheme()
  const [preference, setPreferenceState] = useState<ColorModePreference>(readColorModePreference)

  const setPreference = useCallback(
    (next: ColorModePreference) => {
      setPreferenceState(next)
      const resolved = resolveColorMode(next)
      applyColorModePreference(next)
      setColorMode(resolved)
    },
    [setColorMode],
  )

  useEffect(() => {
    const resolved = resolveColorMode(preference)
    applyDocumentColorMode(resolved)
    setColorMode(resolved)

    if (preference !== 'system') {
      return
    }

    const media = window.matchMedia('(prefers-color-scheme: dark)')
    const onChange = () => {
      const systemMode = getSystemColorMode()
      applyDocumentColorMode(systemMode)
      setColorMode(systemMode)
    }
    media.addEventListener('change', onChange)
    return () => media.removeEventListener('change', onChange)
  }, [preference, setColorMode])

  return { preference, setPreference }
}
