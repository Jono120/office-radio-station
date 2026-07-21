import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AppShell } from '@/components/app-shell'
import { AdminSettingsPage } from '@/pages/admin-settings-page'
import { JukeboxPage } from '@/pages/jukebox-page'
import { ProfilePage } from '@/pages/profile-page'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/"
          element={
            <AppShell>
              <JukeboxPage />
            </AppShell>
          }
        />
        <Route
          path="/profile"
          element={
            <AppShell>
              <ProfilePage />
            </AppShell>
          }
        />
        <Route
          path="/admin"
          element={
            <AppShell>
              <AdminSettingsPage />
            </AppShell>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App
