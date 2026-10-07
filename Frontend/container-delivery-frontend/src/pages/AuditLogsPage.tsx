import React, { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { auditService } from '@/services/api';
import { AuditLog, AuditAction, EntityType, PagedResponse } from '@/types';
import { Table, TableColumn } from '@/components/ui/Table';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { LoadingSpinner, TableLoading } from '@/components/ui/LoadingSpinner';
import { useToast } from '@/components/ui/Toast';
import { 
  Search, 
  Filter, 
  Download, 
  Calendar, 
  User, 
  Database, 
  FileText, 
  Truck,
  AlertCircle,
  MoreVertical
} from 'lucide-react';
import { clsx } from 'clsx';

const actionOptions = [
  { value: '', label: 'All Actions' },
  { value: 'Create', label: 'Create' },
  { value: 'Update', label: 'Update' },
  { value: 'Delete', label: 'Delete' },
  { value: 'Login', label: 'Login' },
  { value: 'Logout', label: 'Logout' },
  { value: 'Import', label: 'Import' },
  { value: 'Export', label: 'Export' },
  { value: 'Delivery', label: 'Delivery' },
  { value: 'Undelivery', label: 'Undelivery' },
  { value: 'ReportGeneration', label: 'Report Generation' },
  { value: 'PasswordChange', label: 'Password Change' },
  { value: 'MfaSetup', label: 'MFA Setup' },
  { value: 'MfaDisable', label: 'MFA Disable' },
  { value: 'RoleAssignment', label: 'Role Assignment' },
  { value: 'UserActivation', label: 'User Activation' },
  { value: 'ContainerDeletion', label: 'Container Deletion' },
];

const entityOptions = [
  { value: '', label: 'All Types' },
  { value: 'User', label: 'User' },
  { value: 'Container', label: 'Container' },
  { value: 'Vehicle', label: 'Vehicle' },
  { value: 'DeliveryRecord', label: 'Delivery Record' },
  { value: 'Report', label: 'Report' },
  { value: 'ImportBatch', label: 'Import Batch' },
  { value: 'Role', label: 'Role' },
];

export const AuditLogsPage: React.FC = () => {
  const { t } = useTranslation();
  const { success: showSuccess, error: showError } = useToast();
  const [page, setPage] = useState(1);
  const [pageSize] = useState(50);
  const [search, setSearch] = useState('');
  const [actionFilter, setActionFilter] = useState('');
  const [entityFilter, setEntityFilter] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['auditLogs', page, pageSize, search, actionFilter, entityFilter, fromDate, toDate],
    queryFn: () => auditService.getAll({ 
      page, 
      pageSize, 
      action: actionFilter as AuditAction, 
      entityType: entityFilter as EntityType,
      fromDate: fromDate || undefined,
      toDate: toDate || undefined,
    }),
  });

  const exportMutation = useMutation({
    mutationFn: () => auditService.export(fromDate || undefined, toDate || undefined),
    onSuccess: (blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `audit_logs_${new Date().toISOString().split('T')[0]}.xlsx`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      showSuccess('Audit logs exported successfully');
    },
    onError: (err: Error) => showError(err.message),
  });

  const columns: TableColumn<AuditLog>[] = [
    { 
      key: 'timestamp', 
      header: t('audit.timestamp'), 
      render: (log) => (
        <span className="font-mono text-sm text-gray-500 dark:text-gray-400 whitespace-nowrap">
          {new Date(log.timestamp).toLocaleString()}
        </span>
      ),
    },
    { 
      key: 'user', 
      header: t('audit.user'), 
      render: (log) => (
        <div className="flex items-center gap-2">
          <User className="w-4 h-4 text-gray-400" />
          <span className="text-gray-900 dark:text-white">{log.userEmail || 'System'}</span>
        </div>
      ),
    },
    { 
      key: 'action', 
      header: t('audit.action'), 
      render: (log) => {
        const actionIcons: Record<string, React.ReactNode> = {
          Create: <Database className="w-4 h-4 text-success-500" />,
          Update: <MoreVertical className="w-4 h-4 text-primary-500" />,
          Delete: <AlertCircle className="w-4 h-4 text-danger-500" />,
          Login: <User className="w-4 h-4 text-success-500" />,
          Logout: <User className="w-4 h-4 text-gray-500" />,
          Import: <Truck className="w-4 h-4 text-primary-500" />,
          Export: <Download className="w-4 h-4 text-warning-500" />,
          Delivery: <Truck className="w-4 h-4 text-success-500" />,
          Undelivery: <AlertCircle className="w-4 h-4 text-warning-500" />,
          ReportGeneration: <FileText className="w-4 h-4 text-primary-500" />,
        };
        return (
          <div className="flex items-center gap-2">
            {actionIcons[log.action] || <MoreVertical className="w-4 h-4" />}
            <span className="font-medium capitalize">{log.action.toLowerCase()}</span>
          </div>
        );
      },
    },
    { 
      key: 'entityType', 
      header: t('audit.entityType'), 
      render: (log) => (
        <span className="badge badge-info">{log.entityType}</span>
      ),
    },
    { 
      key: 'entityId', 
      header: t('audit.entityId'), 
      className: 'text-right font-mono',
      render: (log) => log.entityId ? <span>#{log.entityId}</span> : <span className="text-gray-400">-</span>,
    },
    { 
      key: 'ipAddress', 
      header: t('audit.ipAddress'), 
      className: 'hidden md:table-cell font-mono text-sm',
      render: (log) => log.ipAddress || '-',
    },
  ];

  const handleSearch = useCallback((value: string) => {
    setSearch(value);
    setPage(1);
  }, []);

  const handleActionChange = useCallback((value: string) => {
    setActionFilter(value as AuditAction);
    setPage(1);
  }, []);

  const handleEntityChange = useCallback((value: string) => {
    setEntityFilter(value as EntityType);
    setPage(1);
  }, []);

  const handleDateChange = useCallback((field: 'from' | 'to', value: string) => {
    if (field === 'from') setFromDate(value);
    else setToDate(value);
    setPage(1);
  }, []);

  const handlePageChange = useCallback((newPage: number) => {
    setPage(newPage);
  }, []);

  const totalCount = data?.totalCount || 0;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{t('audit.title')}</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">View and export system audit logs</p>
        </div>
        <Button variant="outline" leftIcon={<Download className="w-4 h-4" />} onClick={() => exportMutation.mutate()} loading={exportMutation.isPending}>
          {t('audit.exportLogs')}
        </Button>
      </div>

      {/* Filters */}
      <Card className="bg-white dark:bg-gray-800">
        <CardBody className="p-4">
          <div className="flex flex-col lg:flex-row gap-4">
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
              value={actionFilter}
              onChange={(e) => handleActionChange(e.target.value)}
              className="input w-auto min-w-[160px]"
            >
              {actionOptions.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
            <select
              value={entityFilter}
              onChange={(e) => handleEntityChange(e.target.value)}
              className="input w-auto min-w-[160px]"
            >
              {entityOptions.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
            <div className="flex gap-2">
              <Input
                type="date"
                value={fromDate}
                onChange={(e) => handleDateChange('from', e.target.value)}
                className="input w-auto"
                placeholder="From"
              />
              <Input
                type="date"
                value={toDate}
                onChange={(e) => handleDateChange('to', e.target.value)}
                className="input w-auto"
                placeholder="To"
              />
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Table */}
      <Card>
        {isLoading ? (
          <TableLoading columns={columns.length} />
        ) : (
          <Table
            columns={columns}
            data={data?.items || []}
            keyExtractor={(l) => l.id.toString()}
            isLoading={isLoading}
            emptyMessage={t('audit.noLogs')}
            hoverable
          />
        )}
      </Card>
    </div>
  );
};