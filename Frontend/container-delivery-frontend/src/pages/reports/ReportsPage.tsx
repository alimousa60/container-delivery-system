import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { reportService } from '@/services/api';
import { ContainerReport, PagedResponse } from '@/types';
import { Table, TableColumn } from '@/components/ui/Table';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { LoadingSpinner, TableLoading } from '@/components/ui/LoadingSpinner';
import { useToast } from '@/components/ui/Toast';
import { FileText, Download, Eye, Trash2, Plus, RefreshCw } from 'lucide-react';
import { clsx } from 'clsx';

export const ReportsPage: React.FC = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { success: showSuccess, error: showError } = useToast();
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [selectedContainers, setSelectedContainers] = useState<number[]>([]);
  const [showBulkModal, setShowBulkModal] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ['reports', page, pageSize],
    queryFn: () => reportService.getAll({ page, pageSize }),
  });

  const generateMutation = useMutation({
    mutationFn: (containerId: number) => reportService.generate(containerId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reports'] });
      showSuccess(t('reports.reportGenerated'));
    },
    onError: (err: Error) => showError(err.message),
  });

  const downloadMutation = useMutation({
    mutationFn: (reportId: number) => reportService.download(reportId),
    onSuccess: (blob, reportId) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `report_${reportId}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    },
    onError: (err: Error) => showError(err.message),
  });

  const deleteMutation = useMutation({
    mutationFn: (reportId: number) => reportService.delete(reportId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reports'] });
      showSuccess('Report deleted');
    },
    onError: (err: Error) => showError(err.message),
  });

  const bulkGenerateMutation = useMutation({
    mutationFn: () => reportService.generateBulk(selectedContainers),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['reports'] });
      setShowBulkModal(false);
      setSelectedContainers([]);
      if (result.successCount > 0) {
        showSuccess(`${result.successCount} reports generated`);
      }
      if (result.failureCount > 0) {
        showError(`${result.failureCount} failed`);
      }
    },
    onError: (err: Error) => showError(err.message),
  });

  const columns: TableColumn<ContainerReport>[] = [
    { 
      key: 'containerNumber', 
      header: t('reports.containerNumber'), 
      render: (report) => (
        <span className="font-mono font-medium text-gray-900 dark:text-white">{report.containerNumber}</span>
      ),
    },
    { 
      key: 'generatedAt', 
      header: t('reports.reportDate'), 
      render: (report) => (
        <span className="text-gray-500 dark:text-gray-400">{new Date(report.generatedAt).toLocaleString()}</span>
      ),
    },
    { 
      key: 'generatedBy', 
      header: t('reports.generatedBy'), 
      render: (report) => (
        <span className="text-gray-500 dark:text-gray-400">{report.generatedBy}</span>
      ),
    },
    { 
      key: 'totalVehicles', 
      header: t('reports.totalVehicles'), 
      className: 'text-right',
      render: (report) => <span>{report.totalVehicles}</span>,
    },
    { 
      key: 'deliveredVehicles', 
      header: t('reports.deliveredVehicles'), 
      className: 'text-right',
      render: (report) => <span className="text-success-600">{report.deliveredVehicles}</span>,
    },
    { 
      key: 'completionPercentage', 
      header: t('reports.completionPercentage'), 
      className: 'text-right',
      render: (report) => <span className="font-medium">{report.completionPercentage.toFixed(1)}%</span>,
    },
    { 
      key: 'actions', 
      header: t('common.actions'),
      className: 'text-right',
      render: (report) => (
        <div className="flex items-center justify-end gap-2">
          <Button variant="ghost" size="sm" onClick={() => downloadMutation.mutate(report.id)} leftIcon={<Download className="w-4 h-4" />} aria-label="Download">
            <Download className="w-4 h-4" />
          </Button>
          <Button variant="ghost" size="sm" onClick={() => { if (confirm('Delete this report?')) deleteMutation.mutate(report.id); }} leftIcon={<Trash2 className="w-4 h-4" />} danger aria-label="Delete">
            <Trash2 className="w-4 h-4" />
          </Button>
        </div>
      ),
    },
  ];

  const totalCount = data?.totalCount || 0;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <FileText className="w-6 h-6 text-primary-600" />
            {t('reports.title')}
          </h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">View and generate container delivery reports</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setShowBulkModal(true)} leftIcon={<Plus className="w-4 h-4" />}>
            {t('reports.bulkGenerate')}
          </Button>
          <Button variant="primary" leftIcon={<RefreshCw className="w-4 h-4" />} onClick={() => queryClient.invalidateQueries({ queryKey: ['reports'] })}>
            {t('common.refresh')}
          </Button>
        </div>
      </div>

      <Card>
        {isLoading ? (
          <TableLoading columns={columns.length} />
        ) : error ? (
          <CardBody className="py-12 text-center">
            <p className="text-danger-500">Failed to load reports</p>
          </CardBody>
        ) : (
          <Table
            columns={columns}
            data={data?.items || []}
            keyExtractor={(r) => r.id.toString()}
            isLoading={isLoading}
            emptyMessage={t('reports.noReports')}
            hoverable
          />
        )}
      </Card>

      {/* Bulk Generate Modal */}
      <Modal
        isOpen={showBulkModal}
        onClose={() => setShowBulkModal(false)}
        title={t('reports.bulkGenerate')}
        size="md"
      >
        <div className="space-y-4">
          <p className="text-gray-600 dark:text-gray-400">
            Select containers to generate reports for:
          </p>
          <div className="max-h-60 overflow-y-auto space-y-2 border border-gray-200 dark:border-gray-700 rounded-lg p-4">
            {data?.items.map(report => (
              <label key={report.containerId} className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={selectedContainers.includes(report.containerId)}
                  onChange={(e) => {
                    if (e.target.checked) {
                      setSelectedContainers(prev => [...prev, report.containerId]);
                    } else {
                      setSelectedContainers(prev => prev.filter(id => id !== report.containerId));
                    }
                  }}
                  className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                />
                <span className="text-sm text-gray-900 dark:text-white">{report.containerNumber}</span>
              </label>
            ))}
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setShowBulkModal(false)} disabled={bulkGenerateMutation.isPending}>
              {t('common.cancel')}
            </Button>
            <Button variant="primary" onClick={() => bulkGenerateMutation.mutate()} loading={bulkGenerateMutation.isPending} disabled={selectedContainers.length === 0}>
              {t('reports.generateReport')} ({selectedContainers.length})
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};