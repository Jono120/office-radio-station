import { Link, useLocation } from 'react-router-dom'
import { Radio, Settings, User } from 'lucide-react'
import { ThemeSwitcher } from '@/components/kibo-ui/theme-switcher'
import { Button } from '@/components/ui/button'
import { useProfile } from '@/hooks/use-profile'
import { useTheme } from '@/hooks/use-theme'
import { cn } from '@/lib/utils'

type AppShellProps = {
  children: React.ReactNode
}

export function AppShell({ children }: AppShellProps) {
  const { theme, setTheme } = useTheme()
  const { initials } = useProfile()
  const location = useLocation()
  const isAdmin = location.pathname.startsWith('/admin')
  const isProfile = location.pathname.startsWith('/profile')

  const subtitle = isAdmin
    ? 'Admin settings'
    : isProfile
      ? 'Your profile'
      : 'Multi-provider office music queue'

  return (
    <div className="min-h-svh bg-background text-foreground">
      <header className="border-b bg-card/50 backdrop-blur-sm">
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-4 px-6 py-5">
          <div className="flex items-center gap-3 text-left">
            <Link to="/" className="flex items-center gap-3">
              <div className="flex size-10 items-center justify-center rounded-xl bg-primary text-primary-foreground">
                <Radio className="size-5" />
              </div>
              <div>
                <h1 className="text-xl font-semibold tracking-tight">OfficeJukebox</h1>
                <p className="text-sm text-muted-foreground">{subtitle}</p>
              </div>
            </Link>
          </div>
          <div className="flex items-center gap-2">
            {!isAdmin ? (
              <Link to="/profile" aria-label="Your profile">
                <Button
                  size="icon-sm"
                  variant={isProfile ? 'secondary' : 'outline'}
                  className={cn('rounded-full', isProfile && 'ring-2 ring-ring')}
                >
                  <User className="size-4" />
                  <span className="sr-only">Profile ({initials})</span>
                </Button>
              </Link>
            ) : null}
            {!isAdmin ? (
              <Link to="/admin">
                <Button size="sm" variant="outline">
                  <Settings className="size-4" />
                  Admin
                </Button>
              </Link>
            ) : (
              <Link to="/">
                <Button size="sm" variant="outline">
                  Back to jukebox
                </Button>
              </Link>
            )}
            <ThemeSwitcher value={theme} onChange={setTheme} />
          </div>
        </div>
      </header>
      <main className="mx-auto flex max-w-5xl flex-col gap-6 px-6 py-8">{children}</main>
    </div>
  )
}
