import React, { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { containerService } from '@/services/api';
import { ImportBatch, PagedResponse } from '@/types';
import { Table, TableColumn } from '@/components/ui/Table';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { LoadingSpinner, TableLoading } from '@/components/ui/LoadingSpinner';
import { useToast } from '@/components/ui/Toast';
import { Upload, FileText, RefreshCw, AlertCircle, CheckCircle, Download } from 'lucide-react';
import { clsx } from 'clsx';

export const ImportPage: React.FC = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { success: showSuccess, error: showError } = useToast();
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [containerPrefix, setContainerPrefix] = useState('');
  const [uploadProgress, setUploadProgress] = useState(0);
  const [currentImportId, setCurrentImportId] = useState<number | null>(null);

  const { data: imports, isLoading } = useQuery({
    queryKey: ['imports', page, pageSize],
    queryFn: () => containerService.getImports({ page, pageSize }),
  });

  const importMutation = useMutation({
    mutationFn: ({ file, prefix }: { file: File; prefix?: string }) => 
      containerService.importVehicles(file, prefix),
    onMutate: () => {
      setUploadProgress(0);
      const progressInterval = setInterval(() => {
        setUploadProgress(prev => Math.min(prev + 10, 90));
      }, 500);
      return () => clearInterval(progressInterval);
    },
    onSuccess: (result) => {
      setUploadProgress(100);
      setCurrentImportId(result.id);
      queryClient.invalidateQueries({ queryKey: ['imports'] });
      queryClient.invalidateQueries({ queryKey: ['containers'] });
      queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
      setSelectedFile(null);
      setContainerPrefix('');
      if (result.failedRecords > 0) {
        showError(`${result.failedRecords} records failed. Check import details.`);
      } else {
        showSuccess(t('import.importComplete'));
      }
    },
    onError: (err: Error) => {
      showError(err.message);
      setUploadProgress(0);
    },
    onSettled: () => {
      setTimeout(() => setUploadProgress(0), 1000);
    },
  });

  const columns: TableColumn<ImportBatch>[] = [
    { 
      key: 'fileName', 
      header: t('import.fileName'), 
      render: (batch) => (
        <div className="flex items-center gap-2">
          <FileText className="w-5 h-5 text-gray-400" />
          <span className="font-medium text-gray-900 dark:text-white truncate max-w-xs">{batch.fileName}</span>
        </div>
      ),
    },
    { 
      key: 'status', 
      header: t('common.status'), 
      render: (batch) => {
        const statusColors = {
          Pending: 'gray',
          Processing: 'info',
          Completed: 'success',
          Failed: 'danger',
        };
        return (
          <span className={clsx('badge', `badge-${statusColors[batch.status as keyof typeof statusColors] || 'gray'}`)}>
            {t(`import.${batch.status.toLowerCase()}`) || batch.status}
          </span>
        );
      },
    },
    { 
      key: 'totalRecords', 
      header: t('import.totalRecords'), 
      className: 'text-right',
      render: (batch) => <span>{batch.totalRecords}</span>,
    },
    { 
      key: 'successfulRecords', 
      header: t('import.successfulRecords'), 
      className: 'text-right',
      render: (batch) => <span className="text-success-600">{batch.successfulRecords}</span>,
    },
    { 
      key: 'failedRecords', 
      header: t('import.failedRecords'), 
      className: 'text-right',
      render: (batch) => <span className="text-danger-600">{batch.failedRecords}</span>,
    },
    { 
      key: 'importedAt', 
      header: t('common.createdAt'), 
      render: (batch) => (
        <span className="text-gray-500 dark:text-gray-400">{new Date(batch.importedAt).toLocaleString()}</span>
      ),
    },
    { 
      key: 'actions', 
      header: t('common.actions'),
      className: 'text-right',
      render: (batch) => (
        <div className="flex items-center justify-end gap-2">
          {batch.failedRecords > 0 && (
            <Button variant="ghost" size="sm" onClick={() => alert(batch.errorDetails || 'No errors')} leftIcon={<AlertCircle className="w-4 h-4" />}>Errors</Button>
          )}
        </div>
      ),
    },
  ];

  const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      const allowedTypes = ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'application/vnd.ms-excel'];
      if (!allowedTypes.includes(file.type)) {
        showError('Please select an Excel file (.xlsx or .xls)');
        return;
      }
      setSelectedFile(file);
    }
  }, [showError]);

  const handleUpload = () => {
    if (!selectedFile) {
      showError('Please select a file first');
      return;
    }
    importMutation.mutate({ file: selectedFile, prefix: containerPrefix || undefined });
  };

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.currentTarget.classList.add('border-primary-500', 'bg-primary-50', 'dark:bg-primary-900/10');
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.currentTarget.classList.remove('border-primary-500', 'bg-primary-50', 'dark:bg-primary-900/10');
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.currentTarget.classList.remove('border-primary-500', 'bg-primary-50', 'dark:bg-primary-900/10');
    const file = e.dataTransfer.files[0];
    if (file) {
      const allowedTypes = ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'application/vnd.ms-excel'];
      if (!allowedTypes.includes(file.type)) {
        showError('Please select an Excel file (.xlsx or .xls)');
        return;
      }
      setSelectedFile(file);
    }
  }, [showError]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <Upload className="w-6 h-6 text-primary-600" />
            {t('import.title')}
          </h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Upload Excel files to import vehicles into containers</p>
        </div>
      </div>

      {/* Upload Area */}
      <Card className={clsx(
        'border-2 border-dashed transition-colors',
        selectedFile ? 'border-success-500 bg-success-50 dark:bg-success-900/10' : 'border-gray-300 dark:border-gray-700'
      )} onDragOver={handleDragOver} onDragLeave={handleDragLeave} onDrop={handleDrop}>
        <CardBody className="p-8 text-center">
          {selectedFile ? (
            <div className="flex flex-col items-center gap-4">
              <div className="flex items-center gap-3 p-4 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 w-full max-w-md">
                <FileText className="w-10 h-10 text-primary-600" />
                <div className="flex-1 text-left min-w-0">
                  <p className="font-medium text-gray-900 dark:text-white truncate">{selectedFile.name}</p>
                  <p className="text-sm text-gray-500 dark:text-gray-400">{(selectedFile.size / 1024).toFixed(1)} KB</p>
                </div>
                <Button variant="ghost" size="sm" onClick={() => setSelectedFile(null)} leftIcon={<X className="w-4 h-4" />}>
                  Remove
                </Button>
              </div>
              <div className="flex items-center gap-4">
                <Input
                  label={t('import.containerNumberPrefix') + ' (optional)'}
                  placeholder="e.g., SHIP001"
                  value={containerPrefix}
                  onChange={(e) => setContainerPrefix(e.target.value)}
                  className="w-full max-w-md"
                />
                <Button variant="primary" onClick={handleUpload} loading={importMutation.isPending} leftIcon={<Upload className="w-4 h-4" />} disabled={importMutation.isPending}>
                  {importMutation.isPending ? 'Importing...' : t('import.uploadFile')}
                </Button>
              </div>
              {importMutation.isPending && (
                <div className="w-full max-w-md mx-auto">
                  <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                    <div 
                      className="h-full bg-primary-500 rounded-full transition-all duration-300"
                      style={{ width: `${uploadProgress}%` }}
                    />
                  </div>
                  <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">{uploadProgress}%</p>
                </div>
              )}
            </div>
          ) : (
            <div className="flex flex-col items-center gap-4">
              <Upload className="w-16 h-16 text-gray-400" />
              <div>
                <p className="text-lg font-medium text-gray-900 dark:text-white">{t('import.dragDrop')}</p>
                <p className="text-gray-500 dark:text-gray-400 mt-1">{t('import.requiredColumns')}</p>
              </div>
              <label className="cursor-pointer">
                <Button variant="outline" leftIcon={<Upload className="w-4 h-4" />}>
                  {t('import.uploadFile')}
                </Button>
                <input
                  type="file"
                  accept=".xlsx,.xls"
                  onChange={handleFileSelect}
                  className="sr-only"
                />
              </label>
              <Button variant="ghost" onClick={() => window.open('/template.xlsx', '_blank')} leftIcon={<Download className="w-4 h-4" />}>
                {t('import.downloadTemplate')}
              </Button>
            </div>
          )}
        </CardBody>
      </Card>

      {/* Import History */}
      <Card>
        <CardHeader className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-4">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
            Import History ({imports?.totalCount || 0})
          </h2>
        </CardHeader>
        <CardBody className="pt-0">
          {isLoading ? (
            <TableLoading columns={7} />
          ) : (
            <Table
              columns={columns}
              data={imports?.items || []}
              keyExtractor={(i) => i.id.toString()}
              isLoading={isLoading}
              emptyMessage="No imports yet"
              hoverable
            />
          )}
        </CardBody>
      </Card>
    </div>
  );
};