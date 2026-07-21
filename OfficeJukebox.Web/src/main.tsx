import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import 'react-medium-image-zoom/dist/styles.css'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
