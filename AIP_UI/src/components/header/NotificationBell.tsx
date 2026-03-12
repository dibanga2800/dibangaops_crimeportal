import { Bell } from "lucide-react";
import { Link } from "react-router-dom";
import { useAlertCount } from "@/hooks/useAlertCount";

interface NotificationBellProps {
  className?: string;
}

export const NotificationBell = ({ className = "" }: NotificationBellProps) => {
  const { alertCount, isLoading } = useAlertCount();

  // Only show badge if there are tasks
  const showBadge = alertCount > 0;

  return (
    <Link
      to="/dashboard"
      className={`relative p-1 transition-colors ${className}`}
      aria-label={`Notifications - ${alertCount} ${alertCount === 1 ? 'alert' : 'alerts'}`}
    >
      <Bell className="h-5 w-5" />
      {showBadge && !isLoading && (
        <span className="absolute -top-1 -right-1 min-w-[20px] h-5 px-1 rounded-full bg-red-500 flex items-center justify-center text-[11px] font-medium text-white">
          {alertCount > 99 ? '99+' : alertCount}
        </span>
      )}
    </Link>
  );
}; 