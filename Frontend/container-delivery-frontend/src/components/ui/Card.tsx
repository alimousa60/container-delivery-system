import React from 'react';

interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
  className?: string;
  hover?: boolean;
}

export const Card: React.FC<CardProps> = ({ children, className = '', hover, onClick, ...rest }) => (
  <div
    className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm ${hover ? 'hover:shadow-md hover:border-gray-300 dark:hover:border-gray-600 transition-shadow' : ''} ${className}`}
    onClick={onClick}
    {...rest}
  >
    {children}
  </div>
);

interface CardHeaderProps {
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
  action?: React.ReactNode;
  className?: string;
  children?: React.ReactNode;
}

export const CardHeader: React.FC<CardHeaderProps> = ({ title, subtitle, action, className = '', children }) => (
  <div className={`px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between ${className}`}>
    <div>
      {title && <h3 className="text-lg font-semibold text-gray-900 dark:text-white">{title}</h3>}
      {subtitle && <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">{subtitle}</p>}
      {children}
    </div>
    {action && <div className="flex items-center gap-2">{action}</div>}
  </div>
);

interface CardBodyProps {
  children: React.ReactNode;
  className?: string;
}

export const CardBody: React.FC<CardBodyProps> = ({ children, className = '' }) => (
  <div className={`px-6 py-4 ${className}`}>{children}</div>
);
