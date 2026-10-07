import React, { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { containerService } from '@/services/api';
import { Container } from '@/types';
import { Table, TableColumn } from '@/components/ui/Table';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { StatusBadge } from '@/components/ui/Badge';
import { Card, CardBody } from '@/components/ui/Card';
import { Search, ArrowLeft, Eye } from 'lucide-react';
import { clsx } from 'clsx';

export const ArchivePage: React.FC = () => {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState('');

  const { data, isLoading, error } = useQuery({
    queryKey: ['containers-archive', page, pageSize, search],
    queryFn: () => containerService.getArchive({ page, pageSize, search }),
    placeholderData: (previousData) => previousData,
  });

  const handleSearch = useCallback((value: string) => {
    setSearch(value);
    setPage(1);
  }, []);

  const columns: TableColumn<Container>[] = [
    {
      key: 'containerNumber',
      header: t('containers.containerNumber'),
      sortable: true,
      render: (container) => (
        <div className="font-mono font-medium text-gray-900 dark:text-white">
          {container.containerNumber}
        </div>
      ),
    },
    {
      key: 'totalVehicles',
      header: t('containers.totalVehicles'),
      className: 'text-right',
      render: (container) => <span className="font-medium">{container.totalVehicles}</span>,
    },
    {
      key: 'deliveredVehicles',
      header: t('containers.deliveredVehicles'),
      className: 'text-right',
      render: (container) => (
        <span className="font-medium text-success-600">{container.deliveredVehicles}</span>
      ),
    },
    {
      key: 'status',
      header: t('containers.status'),
      render: (container) => <StatusBadge status={container.status} />,
    },
    {
      key: 'completion',
      header: t('containers.completion'),
      className: 'text-right',
      render: (container) => (
        <div className="flex items-center justify-end gap-2">
          <div className="w-24 h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
            <div
              className={clsx(
                'h-full rounded-full transition-all duration-300',
                container.status === 'FullyDelivered' || container.status === 'Closed'
                  ? 'bg-success-500'
                  : container.status === 'InProgress'
                    ? 'bg-primary-500'
                    : 'bg-gray-300'
              )}
              style={{ width: `${container.completionPercentage}%` }}
            />
          </div>
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300 w-16 text-right">
            {container.completionPercentage.toFixed(1)}%
          </span>
        </div>
      ),
    },
    {
      key: 'completedAt',
      header: t('containers.completedAt'),
      render: (container) => (
        <span className="text-sm text-gray-500 dark:text-gray-400">
          {container.completedAt ? new Date(container.completedAt).toLocaleString() : '-'}
        </span>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      className: 'text-right',
      render: (container) => (
        <Button
          variant="ghost"
          size="sm"
          leftIcon={<Eye className="w-4 h-4" />}
          onClick={() => (window.location.href = `/containers/${container.id}`)}
        >
          {t('containers.viewDetails')}
        </Button>
      ),
    },
  ];

  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;
  const totalCount = data?.totalCount || 0;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            {t('containers.archive')}
          </h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">{t('containers.archiveSubtitle')}</p>
        </div>
        <Button
          variant="secondary"
          onClick={() => (window.location.href = '/containers')}
          leftIcon={<ArrowLeft className="w-4 h-4" />}
        >
          {t('common.back')}
        </Button>
      </div>

      <Card className="bg-white dark:bg-gray-800">
        <CardBody className="p-4">
          <div className="max-w-md">
            <Input
              placeholder={t('common.search')}
              value={search}
              onChange={(e) => handleSearch(e.target.value)}
              leftIcon={<Search className="w-5 h-5" />}
            />
          </div>
        </CardBody>
      </Card>

      <Card>
        {error ? (
          <CardBody className="py-12 text-center">
            <p className="text-danger-500">{t('containers.archiveError')}</p>
          </CardBody>
        ) : (
          <>
            <Table
              columns={columns}
              data={data?.items || []}
              keyExtractor={(c) => c.id.toString()}
              isLoading={isLoading}
              emptyMessage={t('containers.noArchived')}
              hoverable
            />
            {totalPages > 1 && (
              <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <p className="text-sm text-gray-500 dark:text-gray-500">
                  Showing {(page - 1) * pageSize + 1} to {Math.min(page * pageSize, totalCount)} of {totalCount}
                </p>
                <div className="flex items-center gap-2">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setPage(page - 1)}
                    disabled={page === 1}
                  >
                    {t('common.previous')}
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setPage(page + 1)}
                    disabled={page === totalPages}
                  >
                    {t('common.next')}
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </Card>
    </div>
  );
};
