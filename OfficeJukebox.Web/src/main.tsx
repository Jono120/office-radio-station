import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Reshaped } from 'reshaped'
import 'reshaped/themes/slate/theme.css'
import 'react-medium-image-zoom/dist/styles.css'
import { AdminSessionProvider } from '@/hooks/use-admin-session'
import { readColorModePreference, resolveColorMode } from '@/lib/color-mode'
import App from './App.tsx'

const initialColorMode = resolveColorMode(readColorModePreference())

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Reshaped theme="slate" defaultColorMode={initialColorMode}>
      <AdminSessionProvider>
        <App />
      </AdminSessionProvider>
    </Reshaped>
  </StrictMode>,
)
