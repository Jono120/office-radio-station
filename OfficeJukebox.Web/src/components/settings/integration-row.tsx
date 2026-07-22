import type { ReactNode } from 'react'
import { ArrowUpRight, ChevronsUpDown } from 'lucide-react'
import { Badge, Button, Card, Text, View } from 'reshaped'

type IntegrationRowProps = {
  name: string
  description: string
  icon: ReactNode
  connected: boolean
  disabled?: boolean
  disabledReason?: string
  connectedLabel?: string
  disconnectedLabel?: string
  connectLabel?: string
  connectHref?: string
  onConnect?: () => void
  onDisconnect?: () => void
  onManage?: () => void
  expanded?: boolean
  children?: ReactNode
}

export function IntegrationRow({
  name,
  description,
  icon,
  connected,
  disabled = false,
  disabledReason,
  connectedLabel = 'Connected',
  disconnectedLabel = 'Disconnected',
  connectLabel = 'Connect',
  connectHref,
  onConnect,
  onDisconnect,
  onManage,
  expanded = false,
  children,
}: IntegrationRowProps) {
  const showConnect = !connected && (onConnect || connectHref)

  return (
    <Card padding={0}>
      <View align={{ m: 'center' }} direction={{ s: 'column', m: 'row' }} gap={4} justify="space-between" padding={4}>
        <View align="start" direction="row" gap={3}>
          <View
            align="center"
            attributes={{
              style: {
                border: '1px solid var(--rs-color-border-neutral-faded)',
                borderRadius: 8,
                height: 40,
                width: 40,
              },
            }}
            justify="center"
          >
            {icon}
          </View>
          <View gap={1}>
            <Text variant="body-3" weight="medium">
              {name}
            </Text>
            <Text color="neutral-faded" variant="body-3">
              {disabledReason ?? description}
            </Text>
          </View>
        </View>

        <View align="center" direction="row" gap={2} wrap>
          {connected ? (
            <>
              <Button size="small" variant="outline">
                <Badge color="positive">{connectedLabel}</Badge>
                {onManage || onDisconnect ? <ChevronsUpDown size={14} /> : null}
              </Button>
              {onManage ? (
                <Button disabled={disabled} onClick={onManage} size="small" variant="outline">
                  Devices
                </Button>
              ) : null}
              {onDisconnect ? (
                <Button disabled={disabled} onClick={onDisconnect} size="small" variant="ghost">
                  Disconnect
                </Button>
              ) : null}
            </>
          ) : (
            <>
              <Button disabled size="small" variant="outline">
                <Badge color="neutral">{disconnectedLabel}</Badge>
              </Button>
              {showConnect && connectHref ? (
                <Button
                  attributes={{ href: connectHref, rel: 'noopener noreferrer', target: '_blank' }}
                  endIcon={ArrowUpRight}
                  size="small"
                  variant="ghost"
                >
                  {connectLabel}
                </Button>
              ) : null}
              {showConnect && onConnect ? (
                <Button disabled={disabled} endIcon={ArrowUpRight} onClick={onConnect} size="small" variant="ghost">
                  {connectLabel}
                </Button>
              ) : null}
            </>
          )}
        </View>
      </View>

      {expanded && children ? (
        <View
          attributes={{ style: { borderTop: '1px solid var(--rs-color-border-neutral-faded)' } }}
          gap={2}
          padding={4}
        >
          {children}
        </View>
      ) : null}
    </Card>
  )
}

type ProviderIconProps = {
  providerId: string
  label: string
}

const providerStyles: Record<string, { background: string; color: string }> = {
  spotify: { background: '#1DB954', color: '#fff' },
  applemusic: { background: '#FA2D48', color: '#fff' },
  youtube: { background: '#FF0000', color: '#fff' },
}

function normalizeProviderId(providerId: string) {
  return providerId.toLowerCase().replace(/-/g, '')
}

export function ProviderIcon({ providerId, label }: ProviderIconProps) {
  const style = providerStyles[normalizeProviderId(providerId)] ?? {
    background: 'var(--rs-color-background-neutral-faded)',
    color: 'var(--rs-color-foreground-neutral)',
  }

  return (
    <Text
      attributes={{
        'aria-hidden': true,
        style: {
          ...style,
          alignItems: 'center',
          borderRadius: 6,
          display: 'flex',
          fontSize: 12,
          fontWeight: 700,
          height: 24,
          justifyContent: 'center',
          width: 24,
        },
      }}
    >
      {label.slice(0, 1).toUpperCase()}
    </Text>
  )
}
