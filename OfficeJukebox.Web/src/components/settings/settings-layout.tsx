import type { ReactNode } from 'react'
import { Link as RouterLink, NavLink, Outlet } from 'react-router-dom'
import { ArrowLeft, Plug, User } from 'lucide-react'
import { Button, Text, View } from 'reshaped'

const navItems = [
  { to: '/settings/profile', label: 'Profile', icon: User },
  { to: '/settings/accounts', label: 'Accounts', icon: Plug },
] as const

export function SettingsLayout() {
  return (
    <View gap={8}>
      <RouterLink to="/">
        <Button icon={ArrowLeft} size="small" variant="ghost">
          Back to jukebox
        </Button>
      </RouterLink>

      <View direction={{ s: 'column', l: 'row' }} gap={{ s: 6, l: 10 }}>
        <View attributes={{ style: { minWidth: 208 } }}>
          <Text color="neutral-faded" variant="caption-1" weight="medium">
            SETTINGS
          </Text>
          <View gap={1} paddingTop={3}>
            {navItems.map(({ to, label, icon: Icon }) => (
              <NavLink key={to} style={{ textDecoration: 'none' }} to={to}>
                {({ isActive }) => (
                  <View
                    align="center"
                    attributes={{
                      style: {
                        background: isActive ? 'var(--rs-color-background-neutral-faded)' : 'transparent',
                        borderRadius: 8,
                        color: isActive ? 'var(--rs-color-foreground-neutral)' : 'var(--rs-color-foreground-neutral-faded)',
                        padding: '8px 12px',
                      },
                    }}
                    direction="row"
                    gap={2}
                  >
                    <Icon size={16} />
                    <Text variant="body-3" weight="medium">
                      {label}
                    </Text>
                  </View>
                )}
              </NavLink>
            ))}
          </View>
        </View>

        <View.Item grow>
          <Outlet />
        </View.Item>
      </View>
    </View>
  )
}

type SettingsSectionProps = {
  title: string
  description: string
  children: ReactNode
}

export function SettingsSection({ title, description, children }: SettingsSectionProps) {
  return (
    <View gap={8}>
      <View
        attributes={{ style: { borderBottom: '1px solid var(--rs-color-border-neutral-faded)' } }}
        gap={1}
        paddingBottom={6}
      >
        <Text variant="featured-2" weight="medium">
          {title}
        </Text>
        <Text color="neutral-faded" variant="body-3">
          {description}
        </Text>
      </View>
      {children}
    </View>
  )
}

type SettingsGroupProps = {
  title: string
  description: string
  children: ReactNode
}

export function SettingsGroup({ title, description, children }: SettingsGroupProps) {
  return (
    <View gap={4}>
      <View gap={1}>
        <Text variant="body-1" weight="medium">
          {title}
        </Text>
        <Text color="neutral-faded" variant="body-3">
          {description}
        </Text>
      </View>
      <View gap={3}>{children}</View>
    </View>
  )
}

type SettingsFieldProps = {
  label: string
  description?: string
  children: ReactNode
  htmlFor?: string
}

export function SettingsField({ label, description, children, htmlFor }: SettingsFieldProps) {
  return (
    <View
      attributes={{ style: { borderBottom: '1px solid var(--rs-color-border-neutral-faded)' } }}
      direction={{ s: 'column', m: 'row' }}
      gap={4}
      paddingBlock={5}
    >
      <View attributes={{ style: { maxWidth: 220 } }} gap={1}>
        <Text as="label" attributes={{ htmlFor }} variant="body-3" weight="medium">
          {label}
        </Text>
        {description ? (
          <Text color="neutral-faded" variant="body-3">
            {description}
          </Text>
        ) : null}
      </View>
      <View.Item grow>{children}</View.Item>
    </View>
  )
}
