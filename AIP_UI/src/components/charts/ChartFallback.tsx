import React from 'react';
import { BarChartIcon, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface ChartFallbackProps {
  title?: string;
  message?: string;
  onRetry?: () => void;
}

const ChartFallback: React.FC<ChartFallbackProps> = ({
  title = 'Chart could not be displayed',
  message = 'There was an error rendering this chart. Please try again or contact support if the issue persists.',
  onRetry
}) => {
  return (
    <div className="flex flex-col items-center justify-center h-[400px] p-6 bg-muted/50 rounded-lg border border-border">
      <BarChartIcon className="h-16 w-16 text-slate-300 mb-4" />
      <h3 className="text-lg font-medium text-foreground mb-2">{title}</h3>
      <p className="text-sm text-muted-foreground text-center max-w-md mb-6">{message}</p>
      {onRetry && (
        <Button 
          onClick={onRetry}
          variant="outline"
          className="flex items-center gap-2"
        >
          <RefreshCw className="h-4 w-4" />
          Retry
        </Button>
      )}
    </div>
  );
};

export default ChartFallback; 