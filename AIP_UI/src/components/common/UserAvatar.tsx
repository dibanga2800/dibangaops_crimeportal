import { useContext } from 'react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { AuthContext } from '@/contexts/AuthContext'
import { DEFAULT_AVATAR } from '@/constants/header'

interface UserAvatarProps {
  size?: 'sm' | 'md' | 'lg'
  className?: string
  showBorder?: boolean
}

export function UserAvatar({
  size = 'md',
  className = '',
  showBorder = false,
}: UserAvatarProps) {
  const authContext = useContext(AuthContext)
  const user = authContext?.user ?? null

  const sizeClasses = {
    sm: 'h-8 w-8',
    md: 'h-10 w-10',
    lg: 'h-14 w-14',
  }

  const borderClass = showBorder ? 'border border-blue-700' : ''

  const profileSrc = user?.profilePicture || DEFAULT_AVATAR
  const initials = user
    ? `${user.firstName?.charAt(0) ?? ''}${user.lastName?.charAt(0) ?? ''}`.toUpperCase()
    : ''
  const altText = user ? `${user.firstName} ${user.lastName}` : 'User'

  return (
    <Avatar className={`${sizeClasses[size]} ${borderClass} ${className}`}>
      <AvatarImage
        src={profileSrc}
        alt={altText}
        className="object-cover"
      />
      <AvatarFallback className="bg-blue-700 text-white">
        {initials}
      </AvatarFallback>
    </Avatar>
  )
}
