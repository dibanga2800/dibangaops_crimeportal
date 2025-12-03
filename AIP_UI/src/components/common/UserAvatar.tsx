import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { USER_DATA } from '@/constants/header';

interface UserAvatarProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  showBorder?: boolean;
}

/**
 * A reusable component for displaying the user's avatar consistently across the application
 */
export function UserAvatar({ 
  size = 'md', 
  className = '',
  showBorder = false
}: UserAvatarProps) {
  // Map size names to actual size classes
  const sizeClasses = {
    sm: 'h-8 w-8',
    md: 'h-10 w-10',
    lg: 'h-14 w-14'
  };
  
  const borderClass = showBorder ? 'border border-blue-700' : '';
  
  return (
    <Avatar className={`${sizeClasses[size]} ${borderClass} ${className}`}>
      <AvatarImage 
        src={USER_DATA.avatar} 
        alt={USER_DATA.name} 
        className="object-cover"
      />
      <AvatarFallback className="bg-blue-700 text-white">
        {USER_DATA.initials}
      </AvatarFallback>
    </Avatar>
  );
} 