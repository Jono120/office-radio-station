import { Link as RouterLink } from 'react-router-dom'
import { Shield, ShieldCheck } from 'lucide-react'
import { Badge, Button } from 'reshaped'
import { useAdminSession } from '@/hooks/use-admin-session'

export function AdminStatusButton() {
  const { isAuthenticated, isLoading } = useAdminSession()
  const statusLabel = isLoading ? 'Checking…' : isAuthenticated ? 'Signed in' : 'Sign in'
  const ShieldIcon = isAuthenticated ? ShieldCheck : Shield

  return (
    <RouterLink aria-label={isAuthenticated ? 'Admin signed in' : 'Admin sign in'} to="/settings/accounts">
      <Button icon={ShieldIcon} size="small" variant={isAuthenticated ? 'solid' : 'outline'}>
        <Badge color={isLoading ? 'warning' : isAuthenticated ? 'positive' : 'neutral'} size="small">
          {statusLabel}
        </Badge>
      </Button>
    </RouterLink>
  )
}
