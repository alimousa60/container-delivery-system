import React, { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { vehicleService } from '@/services/api';
import { Vehicle, PagedResponse } from '@/types';
import { Table, TableColumn } from '@/components/ui/Table';
import { Input } from '@/components/ui/Input';
import { StatusBadge } from '@/components/ui/Badge';
import { Card, CardBody } from '@/components/ui/Card';
import { LoadingSpinner, TableLoading } from '@/components/ui/LoadingSpinner';
import { Search, Filter, Truck } from 'lucide-react';
import { clsx } from 'clsx';

export const VehiclesPage: React.FC = () => {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [search]);

  const { data, isLoading, error } = useQuery({
    queryKey: ['vehicles', 'search', debouncedSearch, page, pageSize],
    queryFn: () => vehicleService.search(debouncedSearch, page, pageSize),
    enabled: debouncedSearch.length >= 2 || debouncedSearch.length === 0,
    placeholderData: (previousData) => previousData,
  });

  const columns: TableColumn<Vehicle>[] = [
    { 
      key: 'vin', 
      header: t('vehicles.vin'), 
      render: (vehicle) => (
        <code className="font-mono text-sm bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">{vehicle.vin}</code>
      ),
    },
    { 
      key: 'description', 
      header: t('vehicles.description'), 
      render: (vehicle) => (
        <span className="font-medium text-gray-900 dark:text-white max-w-xs truncate block">{vehicle.description}</span>
      ),
    },
    { 
      key: 'containerNumber', 
      header: t('vehicles.container'), 
      render: (vehicle) => (
        <span className="font-mono text-sm text-primary-600 dark:text-primary-400">{vehicle.containerNumber}</span>
      ),
    },
    { 
      key: 'isDelivered', 
      header: t('vehicles.isDelivered'), 
      render: (vehicle) => (
        <StatusBadge status={vehicle.isDelivered ? 'FullyDelivered' : 'NotStarted'} />
      ),
    },
    { 
      key: 'deliveredAt', 
      header: t('vehicles.deliveredAt'), 
      className: 'hidden md:table-cell',
      render: (vehicle) => vehicle.deliveredAt ? (
        <span className="text-gray-500 dark:text-gray-400">{new Date(vehicle.deliveredAt).toLocaleString()}</span>
      ) : (
        <span className="text-gray-400 dark:text-gray-500">-</span>
      ),
    },
  ];

  const handleSearch = useCallback((value: string) => {
    setSearch(value);
  }, []);

  const handlePageChange = useCallback((newPage: number) => {
    setPage(newPage);
  }, []);

  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;
  const totalCount = data?.totalCount || 0;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <Truck className="w-6 h-6 text-primary-600" />
            {t('vehicles.title')}
          </h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Search and view vehicle delivery status</p>
        </div>
      </div>

      {/* Search */}
      <Card className="bg-white dark:bg-gray-800">
        <CardBody className="p-4">
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
              <Input
                placeholder={t('vehicles.searchPlaceholder')}
                value={search}
                onChange={(e) => handleSearch(e.target.value)}
                className="pl-10"
                leftIcon={<span />}
              />
            </div>
            {debouncedSearch && (
              <span className="text-sm text-gray-500 dark:text-gray-400 self-center">
                {totalCount} {t('common.results', { defaultValue: 'results' })}
              </span>
            )}
          </div>
        </CardBody>
      </Card>

      {/* Results Table */}
      <Card>
        {isLoading && debouncedSearch ? (
          <TableLoading columns={columns.length} />
        ) : error ? (
          <CardBody className="py-12 text-center">
            <p className="text-danger-500">Failed to search vehicles</p>
          </CardBody>
        ) : (
          <>
            <Table
              columns={columns}
              data={data?.items || []}
              keyExtractor={(v) => v.id.toString()}
              isLoading={isLoading}
              emptyMessage={debouncedSearch ? t('vehicles.noResults') : 'Enter at least 2 characters to search'}
              hoverable
            />
            {totalPages > 1 && (
              <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  Showing {(page - 1) * pageSize + 1} to {Math.min(page * pageSize, totalCount)} of {totalCount}
                </p>
                <div className="flex items-center gap-2">
                  <button 
                    onClick={() => handlePageChange(page - 1)} 
                    disabled={page === 1}
                    className="px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg transition-colors"
                  >
                    Previous
                  </button>
                  <button 
                    onClick={() => handlePageChange(page + 1)} 
                    disabled={page === totalPages}
                    className="px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg transition-colors"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </Card>
    </div>
  );
};