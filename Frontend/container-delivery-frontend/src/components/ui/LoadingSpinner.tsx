import React from 'react';
import { clsx } from 'clsx';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
  label?: string;
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ 
  size = 'md', 
  className = '', 
  label = 'Loading...' 
}) => {
  const sizeClasses = {
    sm: 'w-4 h-4 border-2',
    md: 'w-8 h-8 border-3',
    lg: 'w-12 h-12 border-4',
    xl: 'w-16 h-16 border-4',
  };

  return (
    <div className={clsx('flex flex-col items-center gap-3', className)} role="status" aria-live="polite">
      <svg 
        className={clsx('animate-spin text-primary-600', sizeClasses[size])} 
        xmlns="http://www.w3.org/2000/svg" 
        fill="none" 
        viewBox="0 0 24 24" 
        aria-hidden="true"
      >
        <circle 
          className="opacity-25" 
          cx="12" 
          cy="12" 
          r="10" 
          stroke="currentColor" 
          strokeWidth="4" 
        />
        <path 
          className="opacity-75" 
          fill="currentColor" 
          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" 
        />
      </svg>
      <span className="sr-only">{label}</span>
    </div>
  );
};

export const PageLoading: React.FC = () => (
  <div className="min-h-[400px] flex items-center justify-center">
    <LoadingSpinner size="lg" label="Loading page..." />
  </div>
);

export const TableLoading: React.FC<{ columns: number }> = ({ columns }) => (
  <tbody>
    {Array.from({ length: 5 }).map((_, i) => (
      <tr key={i}>
        {Array.from({ length: columns }).map((_, j) => (
          <td key={j} className="px-4 py-3">
            <div className="skeleton h-4 w-3/4" />
          </td>
        ))}
      </tr>
    ))}
  </tbody>
);

export const CardSkeleton: React.FC = () => (
  <div className="card p-6">
    <div className="space-y-4">
      <div className="h-6 w-1/4 skeleton rounded" />
      <div className="h-4 w-full skeleton rounded" />
      <div className="h-4 w-3/4 skeleton rounded" />
      <div className="h-4 w-1/2 skeleton rounded" />
    </div>
  </div>
);

export const ListSkeleton: React.FC<{ count?: number }> = ({ count = 5 }) => (
  <div className="space-y-4">
    {Array.from({ length: count }).map((_, i) => (
      <div key={i} className="card p-4">
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 rounded-lg skeleton" />
          <div className="flex-1 space-y-2">
            <div className="h-5 w-1/3 skeleton rounded" />
            <div className="h-4 w-1/2 skeleton rounded" />
          </div>
        </div>
      </div>
    ))}
  </div>
);