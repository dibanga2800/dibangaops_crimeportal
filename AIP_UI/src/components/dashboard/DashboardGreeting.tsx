import React, { useEffect, useState } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { format } from 'date-fns';
import { Skeleton } from '@/components/ui/skeleton';

interface DashboardGreetingProps {
  className?: string;
  showLastLogin?: boolean;
  /** Replaces the default one-line summary under the greeting. */
  subtitle?: React.ReactNode;
}

export const DashboardGreeting = ({ 
  className = '',
  showLastLogin = true,
  subtitle,
}: DashboardGreetingProps) => {
  const { user, isLoading, error } = useAuth();
  const [greeting, setGreeting] = useState('');

  useEffect(() => {
    const updateGreeting = () => {
      const hour = new Date().getHours();
      if (hour < 12) setGreeting('Good morning');
      else if (hour < 17) setGreeting('Good afternoon');
      else setGreeting('Good evening');
    };

    updateGreeting();

    // Update greeting every hour
    const timer = setInterval(updateGreeting, 3600000);
    return () => clearInterval(timer);
  }, []);

  const today = format(new Date(), 'EEEE d MMMM yyyy');

  if (isLoading) {
    return (
      <div className={`space-y-2 ${className}`}>
        <Skeleton className="h-8 w-72" />
        <Skeleton className="h-4 w-56" />
      </div>
    );
  }

  if (error) {
    return (
      <div className={`rounded-lg border border-loss/30 bg-loss-soft p-4 ${className}`} role="alert">
        <h1 className="text-base font-semibold text-loss">Your session could not be verified</h1>
        <p className="mt-1 text-sm text-loss/90">{error}</p>
      </div>
    );
  }

  return (
    <div className={`min-w-0 space-y-1.5 ${className}`} aria-label="Dashboard greeting">
      <h1 className="text-[1.75rem] font-semibold leading-[1.1] tracking-tight text-foreground sm:text-[2.125rem]">
        {user ? `${greeting}, ${user.firstName}` : greeting}
      </h1>
      <p className="text-sm text-muted-foreground">
        {subtitle ?? <>Here is where loss and recovery stand on <span className="text-foreground/80">{today}</span>.</>}
      </p>
      {showLastLogin && user?.lastLogin && (
        <p className="text-xs text-muted-foreground">
          Last signed in {format(new Date(user.lastLogin), 'd MMM yyyy, HH:mm')}
        </p>
      )}
    </div>
  );
};

export default DashboardGreeting; 