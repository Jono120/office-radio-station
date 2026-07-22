import { Monitor, Moon, Sun } from 'lucide-react'
import { Actionable, Text, View } from 'reshaped'
import { useColorModePreference } from '@/hooks/use-color-mode-preference'
import type { ColorModePreference } from '@/lib/color-mode'

const themeOptions: { value: ColorModePreference; label: string }[] = [
  { value: 'system', label: 'System' },
  { value: 'light', label: 'Light' },
  { value: 'dark', label: 'Dark' },
]

function PreviewChrome({ mode }: { mode: 'light' | 'dark' }) {
  const light = mode === 'light'
  const sidebar = light ? '#e4e4e7' : '#27272a'
  const surface = light ? '#fafafa' : '#18181b'
  const line = light ? '#d4d4d8' : '#3f3f46'

  return (
    <View direction="row" height="112px">
      <View
        attributes={{ style: { background: sidebar, borderRight: `1px solid ${line}`, width: '28%' } }}
        padding={2}
      >
        <View attributes={{ style: { background: line, borderRadius: 999, height: 6, width: 24 } }} />
        <View gap={1} paddingTop={2}>
          <View attributes={{ style: { background: line, borderRadius: 999, height: 4, width: '100%' } }} />
          <View attributes={{ style: { background: line, borderRadius: 999, height: 4, width: '80%' } }} />
          <View attributes={{ style: { background: line, borderRadius: 999, height: 4, width: '60%' } }} />
        </View>
      </View>
      <View attributes={{ style: { background: surface, flex: 1 } }} padding={2}>
        <View attributes={{ style: { background: line, borderRadius: 999, height: 8, marginBottom: 8, width: 40 } }} />
        <View gap={1}>
          <View attributes={{ style: { background: line, borderRadius: 999, height: 6, width: '100%' } }} />
          <View attributes={{ style: { background: line, borderRadius: 999, height: 6, width: '100%' } }} />
          <View attributes={{ style: { background: line, borderRadius: 999, height: 6, width: '80%' } }} />
        </View>
      </View>
    </View>
  )
}

function ThemePreview({ variant }: { variant: ColorModePreference }) {
  if (variant === 'system') {
    return (
      <View
        attributes={{ style: { border: '1px solid var(--rs-color-border-neutral-faded)', borderRadius: 8, overflow: 'hidden' } }}
        direction="row"
      >
        <View.Item grow>
          <PreviewChrome mode="light" />
        </View.Item>
        <View.Item grow>
          <PreviewChrome mode="dark" />
        </View.Item>
      </View>
    )
  }

  return (
    <View
      attributes={{
        style: {
          background: variant === 'light' ? '#fafafa' : '#18181b',
          border: '1px solid var(--rs-color-border-neutral-faded)',
          borderRadius: 8,
          overflow: 'hidden',
        },
      }}
    >
      <PreviewChrome mode={variant} />
    </View>
  )
}

export function ThemeSelection() {
  const { preference, setPreference } = useColorModePreference()

  return (
    <View
      attributes={{
        role: 'radiogroup',
        'aria-label': 'Theme',
        style: {
          backgroundImage: 'radial-gradient(circle at 1px 1px, var(--rs-color-border-neutral-faded) 1px, transparent 0)',
          backgroundSize: '14px 14px',
          border: '1px solid var(--rs-color-border-neutral-faded)',
          borderRadius: 12,
        },
      }}
      padding={6}
    >
      <View direction="row" gap={4} wrap>
        {themeOptions.map((option) => {
          const selected = preference === option.value

          return (
            <View.Item columns={{ s: 12, m: 4 }} key={option.value}>
              <Actionable
                attributes={{
                  'aria-checked': selected,
                  role: 'radio',
                  style: {
                    border: selected ? '2px solid var(--rs-color-border-primary)' : '2px solid transparent',
                    borderRadius: 12,
                    display: 'flex',
                    flexDirection: 'column',
                    gap: 12,
                    padding: 4,
                    width: '100%',
                  },
                }}
                onClick={() => setPreference(option.value)}
              >
                <ThemePreview variant={option.value} />
                <Text align="center" variant={selected ? 'body-2' : 'body-3'} weight={selected ? 'medium' : 'regular'}>
                  {option.label}
                </Text>
              </Actionable>
            </View.Item>
          )
        })}
      </View>
    </View>
  )
}

export function HeaderColorModeToggle() {
  const { preference, setPreference } = useColorModePreference()

  const options: { value: ColorModePreference; icon: typeof Sun; label: string }[] = [
    { value: 'system', icon: Monitor, label: 'System theme' },
    { value: 'light', icon: Sun, label: 'Light theme' },
    { value: 'dark', icon: Moon, label: 'Dark theme' },
  ]

  return (
    <View
      attributes={{
        style: {
          border: '1px solid var(--rs-color-border-neutral-faded)',
          borderRadius: 999,
          display: 'flex',
          gap: 2,
          padding: 2,
        },
      }}
      direction="row"
    >
      {options.map(({ value, icon: Icon, label }) => (
        <Actionable
          key={value}
          attributes={{
            'aria-label': label,
            'aria-pressed': preference === value,
            style: {
              alignItems: 'center',
              background: preference === value ? 'var(--rs-color-background-neutral-faded)' : 'transparent',
              borderRadius: 999,
              display: 'flex',
              height: 28,
              justifyContent: 'center',
              width: 28,
            },
          }}
          onClick={() => setPreference(value)}
        >
          <Icon size={16} />
        </Actionable>
      ))}
    </View>
  )
}
