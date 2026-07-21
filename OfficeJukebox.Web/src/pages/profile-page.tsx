import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ListMusic, User } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { useProfile } from '@/hooks/use-profile'
import { apiFetch } from '@/lib/api'
import { queueStatusVariant } from '@/lib/format'
import type { QueueItem } from '@/lib/types'

export function ProfilePage() {
  const { username, setUsername, initials } = useProfile()
  const [draftUsername, setDraftUsername] = useState(username)
  const [queue, setQueue] = useState<QueueItem[]>([])
  const [saved, setSaved] = useState(false)

  const refreshQueue = useCallback(async () => {
    const response = await apiFetch('/api/queue')
    if (response.ok) {
      setQueue(await response.json())
    }
  }, [])

  useEffect(() => {
    void refreshQueue()
  }, [refreshQueue])

  useEffect(() => {
    setDraftUsername(username)
  }, [username])

  const myQueueItems = queue.filter(
    (item) => item.user.localeCompare(username, undefined, { sensitivity: 'accent' }) === 0,
  )

  const handleSave = (event: React.FormEvent) => {
    event.preventDefault()
    const trimmed = draftUsername.trim()
    if (!trimmed) {
      return
    }

    setUsername(trimmed)
    setSaved(true)
    window.setTimeout(() => setSaved(false), 2000)
  }

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Your profile</CardTitle>
          <CardDescription>How you appear when you add tracks to the office queue.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="flex flex-col items-center gap-4 text-center sm:flex-row sm:text-left">
            <div
              aria-hidden
              className="flex size-20 shrink-0 items-center justify-center rounded-full bg-primary text-2xl font-semibold text-primary-foreground"
            >
              {initials}
            </div>
            <div className="space-y-1">
              <p className="text-lg font-semibold">{username}</p>
              <p className="text-sm text-muted-foreground">
                Tracks you queue are attributed to this name for everyone in the office.
              </p>
            </div>
          </div>

          <Separator />

          <form className="space-y-4" onSubmit={handleSave}>
            <div className="max-w-md space-y-2">
              <Label htmlFor="profile-username" className="flex items-center gap-2">
                <User className="size-4" />
                Username
              </Label>
              <Input
                id="profile-username"
                value={draftUsername}
                onChange={(e) => setDraftUsername(e.target.value)}
                placeholder="office-user"
              />
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <Button type="submit" disabled={draftUsername.trim().length === 0}>
                Save profile
              </Button>
              {saved ? <span className="text-sm text-muted-foreground">Profile updated.</span> : null}
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ListMusic className="size-4" />
            Your queue activity
          </CardTitle>
          <CardDescription>
            {myQueueItems.length === 0
              ? 'You have no tracks in the queue right now.'
              : `${myQueueItems.length} track${myQueueItems.length === 1 ? '' : 's'} queued under your name.`}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {myQueueItems.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              Head back to the{' '}
              <Link className="font-medium text-foreground underline-offset-4 hover:underline" to="/">
                jukebox
              </Link>{' '}
              to search and queue something.
            </p>
          ) : (
            myQueueItems.map((item) => (
              <div
                key={item.id}
                className="flex items-center justify-between gap-3 rounded-lg border px-3 py-2"
              >
                <div className="min-w-0 text-left">
                  <p className="truncate font-medium">{item.trackName}</p>
                  <p className="truncate text-sm text-muted-foreground">
                    {item.albumName ? `${item.albumName} · ` : ''}
                    {item.provider}
                  </p>
                </div>
                <Badge variant={queueStatusVariant(item.status)}>{item.status}</Badge>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </>
  )
}
