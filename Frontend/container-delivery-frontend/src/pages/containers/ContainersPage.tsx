import React, { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { containerService } from '@/services/api';
import { Container, ContainerStatus, PagedResponse } from '@/types';
import { Table, TableColumn } from '@/components/ui/Table';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Modal } from '@/components/ui/Modal';
import { Dropdown, DropdownItem } from '@/components/ui/Dropdown';
import { StatusBadge } from '@/components/ui/Badge';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { LoadingSpinner, TableLoading } from '@/components/ui/LoadingSpinner';
import { useToast } from '@/components/ui/Toast';
import { 
  Plus, 
  Search, 
  Filter, 
  Download, 
  Eye, 
  Edit, 
  Trash2, 
  Play, 
  FileText,
  ChevronDown,
  MoreVertical,
  Lock,
  RotateCcw,
  Archive
} from 'lucide-react';
import { clsx } from 'clsx';

const statusOptions = [
  { value: '', labelKey: 'allStatuses' },
  { value: 'NotStarted', labelKey: 'notStarted' },
  { value: 'InProgress', labelKey: 'inProgress' },
  { value: 'FullyDelivered', labelKey: 'fullyDelivered' },
  { value: 'Closed', labelKey: 'closed' },
];

export const ContainersPage: React.FC = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { success: showSuccess, error: showError } = useToast();
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newContainerNumber, setNewContainerNumber] = useState('');
  const [newContainerNotes, setNewContainerNotes] = useState('');

  const { data, isLoading, error } = useQuery({
    queryKey: ['containers', page, pageSize, search, statusFilter, sortBy, sortOrder],
    queryFn: () => containerService.getAll({ page, pageSize, search, status: statusFilter, sortBy, sortOrder }),
    placeholderData: (previousData) => previousData,
  });

  const createMutation = useMutation({
    mutationFn: (data: { containerNumber: string; notes?: string }) => containerService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['containers'] });
      queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
      showSuccess(t('containers.createSuccess'));
      setShowCreateModal(false);
      setNewContainerNumber('');
      setNewContainerNotes('');
    },
    onError: (err: Error) => showError(err.message),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => containerService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['containers'] });
      queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
      showSuccess(t('containers.deleteSuccess'));
    },
    onError: (err: Error) => showError(err.message),
  });

  const closeMutation = useMutation({
    mutationFn: (id: number) => containerService.close(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['containers'] });
      queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
      showSuccess(t('containers.closeSuccess'));
    },
    onError: (err: Error) => showError(err.message),
  });

  const reopenMutation = useMutation({
    mutationFn: (id: number) => containerService.reopen(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['containers'] });
      queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
      showSuccess(t('containers.reopenSuccess'));
    },
    onError: (err: Error) => showError(err.message),
  });

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
      sortable: true,
      className: 'text-right',
      render: (container) => (
        <span className="font-medium">{container.totalVehicles}</span>
      ),
    },
    { 
      key: 'deliveredVehicles', 
      header: t('containers.deliveredVehicles'), 
      sortable: true,
      className: 'text-right',
      render: (container) => (
        <span className="font-medium text-success-600">{container.deliveredVehicles}</span>
      ),
    },
    { 
      key: 'status', 
      header: t('containers.status'), 
      sortable: true,
      render: (container) => (
        <StatusBadge status={container.status} />
      ),
    },
    { 
      key: 'completion', 
      header: t('containers.completion'), 
      sortable: true,
      className: 'text-right',
      render: (container) => (
        <div className="flex items-center justify-end gap-2">
          <div className="w-24 h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
            <div 
              className={clsx(
                'h-full rounded-full transition-all duration-300',
                container.status === 'FullyDelivered' ? 'bg-success-500' :
                container.status === 'InProgress' ? 'bg-primary-500' : 'bg-gray-300'
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
      key: 'actions', 
      header: t('common.actions'),
      className: 'text-right',
      render: (container) => (
        <Dropdown
          trigger={
            <button className="p-1.5 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" aria-label="Actions">
              <MoreVertical className="w-5 h-5" />
            </button>
          }
          items={[
            { 
              label: t('containers.viewDetails'), 
              icon: <Eye className="w-4 h-4" />, 
              onClick: () => window.location.href = `/containers/${container.id}` 
            },
            container.status === 'NotStarted' && {
              label: t('containers.startDelivery'),
              icon: <Play className="w-4 h-4" />,
              onClick: async () => {
                try {
                  await containerService.startDelivery(container.id);
                  queryClient.invalidateQueries({ queryKey: ['containers'] });
                  queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
                  showSuccess('Delivery started');
                } catch (err) {
                  showError(err instanceof Error ? err.message : 'Failed to start delivery');
                }
              }
            },
            container.status === 'FullyDelivered' && {
              label: t('containers.close'),
              icon: <Lock className="w-4 h-4" />,
              onClick: () => {
                if (confirm(t('containers.closeConfirm'))) {
                  closeMutation.mutate(container.id);
                }
              },
            },
            container.status === 'Closed' && {
              label: t('containers.reopen'),
              icon: <RotateCcw className="w-4 h-4" />,
              onClick: () => {
                if (confirm(t('containers.reopenConfirm'))) {
                  reopenMutation.mutate(container.id);
                }
              },
            },
            { 
              label: t('reports.generateReport'), 
              icon: <FileText className="w-4 h-4" />, 
              onClick: async () => {
                try {
                  await containerService.generateReport(container.id);
                  showSuccess(t('reports.reportGenerated'));
                } catch (err) {
                  showError(err instanceof Error ? err.message : 'Failed to generate report');
                }
              }
            },
            { divider: true },
            { 
              label: t('common.delete'), 
              icon: <Trash2 className="w-4 h-4" />, 
              onClick: () => {
                if (confirm(t('containers.deleteConfirm'))) {
                  deleteMutation.mutate(container.id);
                }
              },
              danger: true,
              disabled: container.status !== 'FullyDelivered',
            },
          ].filter(Boolean) as DropdownItem[]}
          align="right"
        />
      ),
    },
  ];

  const handleSort = useCallback((key: string) => {
    if (sortBy === key) {
      setSortOrder(prev => prev === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(key);
      setSortOrder('asc');
    }
    setPage(1);
  }, [sortBy]);

  const handleSearch = useCallback((value: string) => {
    setSearch(value);
    setPage(1);
  }, []);

  const handleStatusChange = useCallback((value: string) => {
    setStatusFilter(value);
    setPage(1);
  }, []);

  const handlePageChange = useCallback((newPage: number) => {
    setPage(newPage);
  }, []);

  const handleCreateContainer = () => {
    createMutation.mutate({ containerNumber: newContainerNumber, notes: newContainerNotes || undefined });
  };

  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;
  const totalCount = data?.totalCount || 0;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{t('containers.title')}</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Manage shipping containers and track vehicle deliveries</p>
        </div>
        <div className="flex flex-wrap gap-3">
          <Button
            variant="secondary"
            onClick={() => (window.location.href = '/archive')}
            leftIcon={<Archive className="w-4 h-4" />}
          >
            {t('containers.archive')}
          </Button>
          <Button onClick={() => setShowCreateModal(true)} leftIcon={<Plus className="w-4 h-4" />}>
            {t('containers.createContainer')}
          </Button>
        </div>
      </div>

      {/* Filters */}
      <Card className="bg-white dark:bg-gray-800">
        <CardBody className="p-4">
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
              <Input
                placeholder={t('common.search')}
                value={search}
                onChange={(e) => handleSearch(e.target.value)}
                className="pl-10"
                leftIcon={<span />}
              />
            </div>
            <select
              value={statusFilter}
              onChange={(e) => handleStatusChange(e.target.value)}
              className="input w-auto min-w-[180px]"
            >
              {statusOptions.map(opt => (
                <option key={opt.value} value={opt.value}>{t(`containers.${opt.labelKey}`)}</option>
              ))}
            </select>
          </div>
        </CardBody>
      </Card>

      {/* Table */}
      <Card>
        {isLoading ? (
          <TableLoading columns={columns.length} />
        ) : error ? (
          <CardBody className="py-12 text-center">
            <p className="text-danger-500">Failed to load containers</p>
          </CardBody>
        ) : (
          <>
            <Table
              columns={columns}
              data={data?.items || []}
              keyExtractor={(c) => c.id.toString()}
              isLoading={isLoading}
              emptyMessage={t('containers.noContainers')}
              sortBy={sortBy}
              sortOrder={sortOrder}
              onSort={handleSort}
              hoverable
            />
            {/* Pagination */}
            {totalPages > 1 && (
              <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  Showing {(page - 1) * pageSize + 1} to {Math.min(page * pageSize, totalCount)} of {totalCount}
                </p>
                <div className="flex items-center gap-2">
                  <Button 
                    variant="ghost" 
                    size="sm" 
                    onClick={() => handlePageChange(page - 1)} 
                    disabled={page === 1}
                  >
                    Previous
                  </Button>
                  <Button 
                    variant="ghost" 
                    size="sm" 
                    onClick={() => handlePageChange(page + 1)} 
                    disabled={page === totalPages}
                  >
                    Next
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </Card>

      {/* Create Container Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        title={t('containers.createContainer')}
        size="md"
      >
        <form onSubmit={(e) => { e.preventDefault(); handleCreateContainer(); }} className="space-y-4">
          <Input
            label={t('containers.containerNumber')}
            placeholder="e.g., MCDU5018850_1"
            value={newContainerNumber}
            onChange={(e) => setNewContainerNumber(e.target.value.toUpperCase())}
            error={createMutation.isError ? (createMutation.error as Error).message : undefined}
            required
            autoFocus
          />
          <div>
            <label className="label">{t('containers.notes')}</label>
            <textarea
              className="input min-h-[100px] resize-y"
              placeholder={t('containers.notes')}
              value={newContainerNotes}
              onChange={(e) => setNewContainerNotes(e.target.value)}
              rows={3}
            />
          </div>
          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="secondary" onClick={() => setShowCreateModal(false)} disabled={createMutation.isPending}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="primary" loading={createMutation.isPending}>
              {t('common.save')}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};