import React from 'react';
import { clsx } from 'clsx';
import { ChevronUp, ChevronDown, ChevronUpDown } from 'lucide-react';

export interface TableColumn<T> {
  key: string;
  header: string;
  render?: (item: T, index: number) => React.ReactNode;
  className?: string;
  sortable?: boolean;
  width?: string;
}

interface TableProps<T> {
  columns: TableColumn<T>[];
  data: T[];
  keyExtractor: (item: T) => string;
  isLoading?: boolean;
  emptyMessage?: string;
  onRowClick?: (item: T) => void;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  onSort?: (key: string) => void;
  striped?: boolean;
  hoverable?: boolean;
  className?: string;
}

export function Table<T>({
  columns,
  data,
  keyExtractor,
  isLoading = false,
  emptyMessage = 'No data available',
  onRowClick,
  sortBy,
  sortOrder,
  onSort,
  striped = true,
  hoverable = true,
  className = '',
}: TableProps<T>) {
  const handleSort = (key: string) => {
    if (onSort) {
      onSort(key);
    }
  };

  const getSortIcon = (key: string) => {
    if (sortBy !== key) return <ChevronUpDown className="w-4 h-4 text-gray-400" />;
    return sortOrder === 'asc' ? <ChevronUp className="w-4 h-4 text-primary-600" /> : <ChevronDown className="w-4 h-4 text-primary-600" />;
  };

  if (isLoading) {
    return (
      <div className={clsx('table-container', className)}>
        <table className="table w-full">
          <thead>
            <tr>
              {columns.map((column) => (
                <th key={column.key} className={clsx(column.className)} style={{ width: column.width }}>
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: 5 }).map((_, i) => (
              <tr key={i}>
                {columns.map((column) => (
                  <td key={column.key} className={clsx(column.className)}>
                    <div className="skeleton h-4 w-3/4" />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className={clsx('table-container', className)}>
        <table className="table w-full">
          <thead>
            <tr>
              {columns.map((column) => (
                <th key={column.key} className={clsx(column.className)} style={{ width: column.width }}>
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan={columns.length} className="px-4 py-12 text-center text-gray-500 dark:text-gray-400">
                {emptyMessage}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    );
  }

  return (
    <div className={clsx('table-container scrollbar-thin', className)}>
      <table className="table w-full" role="grid">
        <thead>
          <tr>
            {columns.map((column) => (
              <th
                key={column.key}
                className={clsx(
                  column.className,
                  column.sortable && 'cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-gray-800/50'
                )}
                style={{ width: column.width }}
                onClick={() => column.sortable && handleSort(column.key)}
                scope="col"
              >
                <div className="flex items-center gap-1">
                  {column.header}
                  {column.sortable && getSortIcon(column.key)}
                </div>
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((item, index) => (
            <tr
              key={keyExtractor(item)}
              className={clsx(
                hoverable && 'hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors',
                onRowClick && 'cursor-pointer',
                striped && index % 2 === 1 && 'bg-gray-50/50 dark:bg-gray-800/30'
              )}
              onClick={() => onRowClick?.(item)}
            >
              {columns.map((column) => (
                <td key={column.key} className={clsx(column.className)}>
                  {column.render ? column.render(item, index) : (item as Record<string, unknown>)[column.key] as React.ReactNode}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}