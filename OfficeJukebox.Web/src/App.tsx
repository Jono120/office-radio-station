import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from '@/components/app-shell'
import { SettingsLayout } from '@/components/settings/settings-layout'
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
          path="/settings"
          element={
            <AppShell>
              <SettingsLayout />
            </AppShell>
          }
        >
          <Route index element={<Navigate replace to="profile" />} />
          <Route path="profile" element={<ProfilePage />} />
          <Route path="accounts" element={<AdminSettingsPage />} />
        </Route>
        <Route path="/profile" element={<Navigate replace to="/settings/profile" />} />
        <Route path="/admin" element={<Navigate replace to="/settings/accounts" />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
