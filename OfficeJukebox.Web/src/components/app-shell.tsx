import { Link as RouterLink, useLocation } from 'react-router-dom'
import { Radio, User } from 'lucide-react'
import { Button, Container, Text, View } from 'reshaped'
import { AdminStatusButton } from '@/components/admin-status-button'
import { HeaderColorModeToggle } from '@/components/settings/theme-selection'
import { useProfile } from '@/hooks/use-profile'

type AppShellProps = {
  children: React.ReactNode
}

export function AppShell({ children }: AppShellProps) {
  const { initials } = useProfile()
  const location = useLocation()
  const isSettings = location.pathname.startsWith('/settings')
  const subtitle = isSettings ? 'Settings' : 'Multi-provider office music queue'

  return (
    <View minHeight="100vh">
      <View
        attributes={{
          style: {
            backdropFilter: 'blur(8px)',
            borderBottom: '1px solid var(--rs-color-border-neutral-faded)',
          },
        }}
        paddingBlock={4}
      >
        <Container width="1200px">
          <View align="center" direction="row" gap={4} justify="space-between">
            <RouterLink style={{ color: 'inherit', textDecoration: 'none' }} to="/">
              <View align="center" direction="row" gap={3}>
                <View
                  align="center"
                  attributes={{
                    style: {
                      background: 'var(--rs-color-background-primary)',
                      borderRadius: 12,
                      color: 'var(--rs-color-foreground-primary)',
                      height: 40,
                      width: 40,
                    },
                  }}
                  justify="center"
                >
                  <Radio size={20} />
                </View>
                <View>
                  <Text variant="body-1" weight="medium">
                    OfficeJukebox
                  </Text>
                  <Text color="neutral-faded" variant="caption-1">
                    {subtitle}
                  </Text>
                </View>
              </View>
            </RouterLink>

            <View align="center" direction="row" gap={2}>
              <AdminStatusButton />
              <RouterLink aria-label="Settings" to="/settings/profile">
                <Button
                  highlighted={isSettings}
                  icon={User}
                  size="small"
                  variant={isSettings ? 'solid' : 'outline'}
                >
                  <Text attributes={{ className: 'sr-only' }}>Settings ({initials})</Text>
                </Button>
              </RouterLink>
              <HeaderColorModeToggle />
            </View>
          </View>
        </Container>
      </View>

      <Container padding={10} width="1200px">
        <View gap={4}>{children}</View>
      </Container>
    </View>
  )
}
