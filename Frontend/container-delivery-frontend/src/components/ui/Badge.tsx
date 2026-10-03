import React from 'react';
import { clsx } from 'clsx';

interface BadgeProps {
  children: React.ReactNode;
  variant?: 'success' | 'warning' | 'danger' | 'info' | 'gray';
  size?: 'sm' | 'md';
  className?: string;
  dot?: boolean;
}

export const Badge: React.FC<BadgeProps> = ({ 
  children, 
  variant = 'gray', 
  size = 'md', 
  className = '',
  dot = false,
}) => {
  const variantClasses = {
    success: 'bg-success-100 text-success-800 dark:bg-success-900/30 dark:text-success-400',
    warning: 'bg-warning-100 text-warning-800 dark:bg-warning-900/30 dark:text-warning-400',
    danger: 'bg-danger-100 text-danger-800 dark:bg-danger-900/30 dark:text-danger-400',
    info: 'bg-primary-100 text-primary-800 dark:bg-primary-900/30 dark:text-primary-400',
    gray: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300',
  };

  const sizeClasses = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-2.5 py-0.5 text-xs',
  };

  const dotColors = {
    success: 'bg-success-500',
    warning: 'bg-warning-500',
    danger: 'bg-danger-500',
    info: 'bg-primary-500',
    gray: 'bg-gray-500',
  };

  return (
    <span className={clsx(
      'inline-flex items-center gap-1.5 rounded-full font-medium',
      variantClasses[variant],
      sizeClasses[size],
      className
    )}>
      {dot && <span className={clsx('w-1.5 h-1.5 rounded-full', dotColors[variant])} />}
      {children}
    </span>
  );
};

export const StatusBadge: React.FC<{ status: string; className?: string }> = ({ 
  status, 
  className = '' 
}) => {
  const statusConfig: Record<string, { label: string; variant: 'success' | 'warning' | 'danger' | 'info' | 'gray'; icon: string }> = {
    NotStarted: { label: 'Not Started', variant: 'danger', icon: '🔴' },
    InProgress: { label: 'In Progress', variant: 'warning', icon: '🟡' },
    FullyDelivered: { label: 'Fully Delivered', variant: 'success', icon: '🟢' },
    Pending: { label: 'Pending', variant: 'warning', icon: '⏳' },
    Processing: { label: 'Processing', variant: 'info', icon: '⚙️' },
    Completed: { label: 'Completed', variant: 'success', icon: '✅' },
    Failed: { label: 'Failed', variant: 'danger', icon: '❌' },
  };

  const config = statusConfig[status] || { label: status, variant: 'gray', icon: '⚪' };

  return (
    <Badge variant={config.variant} className={className} dot>
      <span>{config.icon}</span>
      {config.label}
    </Badge>
  );
};