import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { containerService, vehicleService } from '@/services/api';
import { Container, Vehicle, DeliveryMethod } from '@/types';
import { Table, TableColumn } from '@/components/ui/Table';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { Dropdown, DropdownItem } from '@/components/ui/Dropdown';
import { StatusBadge } from '@/components/ui/Badge';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { LoadingSpinner, TableLoading } from '@/components/ui/LoadingSpinner';
import { useToast } from '@/components/ui/Toast';
import { useAuth } from '@/context/AuthContext';
import { 
  ArrowLeft, 
  Truck, 
  Package, 
  CheckCircle, 
  AlertCircle, 
  Clock, 
  AlertTriangle,
  Download,
  Play,
  FileText,
  ScanBarcode,
  MoreVertical,
  X,
  Undo2,
  AlertTriangle as WarningIcon
} from 'lucide-react';
import { clsx } from 'clsx';

export const ContainerDetailPage: React.FC = () => {
  const { t } = useTranslation();
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { success: showSuccess, error: showError, warning: showWarning } = useToast();
  const { user } = useAuth();
  const [showDeliverModal, setShowDeliverModal] = useState(false);
  const [deliverVin, setDeliverVin] = useState('');
  const [deliverMethod, setDeliverMethod] = useState<DeliveryMethod>('Manual');
  const [deliverNotes, setDeliverNotes] = useState('');
  const [scannedVin, setScannedVin] = useState('');
  const [isScanning, setIsScanning] = useState(false);

  const containerId = parseInt(id || '0', 10);

  const { data: container, isLoading, error } = useQuery({
    queryKey: ['container', containerId],
    queryFn: () => containerService.getById(containerId),
    enabled: !!containerId,
  });

  const deliverMutation = useMutation({
    mutationFn: (data: { vin: string; containerId: number; deliveryMethod: DeliveryMethod; notes?: string; scannedVin?: string }) => 
      vehicleService.deliver(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['container', containerId] });
      queryClient.invalidateQueries({ queryKey: ['containers'] });
      queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
      setShowDeliverModal(false);
      setDeliverVin('');
      setDeliverNotes('');
      setScannedVin('');
      if (result.warning) showWarning(result.warning);
      else showSuccess(t('vehicles.deliverSuccess'));
    },
    onError: (err: Error) => showError(err.message),
  });

  const undeliverMutation = useMutation({
    mutationFn: (vehicleId: number) => vehicleService.undeliver(vehicleId, 'Manual correction'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['container', containerId] });
      queryClient.invalidateQueries({ queryKey: ['containers'] });
      queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
      showSuccess(t('vehicles.undeliverSuccess'));
    },
    onError: (err: Error) => showError(err.message),
  });

  const reportMutation = useMutation({
    mutationFn: () => containerService.generateReport(containerId),
    onSuccess: () => showSuccess(t('reports.reportGenerated')),
    onError: (err: Error) => showError(err.message),
  });

  const vehicles = container?.vehicles || [];
  const deliveredVehicles = vehicles.filter(v => v.isDelivered);
  const undeliveredVehicles = vehicles.filter(v => !v.isDelivered);

  const vehicleColumns: TableColumn<Vehicle>[] = [
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
        <span className="font-medium text-gray-900 dark:text-white">{vehicle.description}</span>
      ),
    },
    { 
      key: 'status', 
      header: t('vehicles.isDelivered'), 
      render: (vehicle) => (
        <div className="flex items-center gap-2">
          {vehicle.isDelivered ? (
            <>
              <CheckCircle className="w-5 h-5 text-success-500" />
              <span className="text-success-600 dark:text-success-400 font-medium">{t('vehicles.isDelivered')}</span>
            </>
          ) : (
            <>
              <AlertCircle className="w-5 h-5 text-danger-500" />
              <span className="text-danger-600 dark:text-danger-400 font-medium">{t('vehicles.isDelivered').replace('Delivered', 'Not Delivered')}</span>
            </>
          )}
        </div>
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
    { 
      key: 'deliveredBy', 
      header: t('vehicles.deliveredBy'), 
      className: 'hidden lg:table-cell',
      render: (vehicle) => vehicle.deliveredBy ? (
        <span className="text-gray-500 dark:text-gray-400">{vehicle.deliveredBy}</span>
      ) : (
        <span className="text-gray-400 dark:text-gray-500">-</span>
      ),
    },
    { 
      key: 'actions', 
      header: t('common.actions'),
      className: 'text-right',
      render: (vehicle) => (
        <Dropdown
          trigger={
            <button className="p-1.5 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" aria-label="Actions">
              <MoreVertical className="w-5 h-5" />
            </button>
          }
          items={vehicle.isDelivered ? [
            { 
              label: t('vehicles.undeliver'), 
              icon: <Undo2 className="w-4 h-4" />, 
              onClick: () => {
                if (confirm('Mark this vehicle as undelivered?')) {
                  undeliverMutation.mutate(vehicle.id);
                }
              },
              danger: true,
            },
          ] : [
            { 
              label: t('vehicles.deliver'), 
              icon: <Truck className="w-4 h-4" />, 
              onClick: () => {
                setDeliverVin(vehicle.vin);
                setShowDeliverModal(true);
              },
            },
          ]}
          align="right"
        />
      ),
    },
  ];

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Card>
          <CardBody className="p-6">
            <TableLoading columns={vehicleColumns.length} />
          </CardBody>
        </Card>
      </div>
    );
  }

  if (error || !container) {
    return (
      <div className="text-center py-12">
        <AlertCircle className="w-12 h-12 text-danger-500 mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">Container not found</h2>
        <Button onClick={() => navigate('/containers')} variant="primary" leftIcon={<ArrowLeft className="w-4 h-4" />}>
          Back to Containers
        </Button>
      </div>
    );
  }

  const canDeliver = container.status !== 'FullyDelivered' && undeliveredVehicles.length > 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="flex items-center gap-4">
          <Button variant="ghost" onClick={() => navigate('/containers')} leftIcon={<ArrowLeft className="w-4 h-4" />} size="sm">
            Back
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
              {container.containerNumber}
            </h1>
            <div className="flex items-center gap-4 mt-1 text-sm text-gray-500 dark:text-gray-400">
              <span className="flex items-center gap-1">
                <Truck className="w-4 h-4" />
                {container.totalVehicles} vehicles
              </span>
              <span className="flex items-center gap-1">
                <CheckCircle className="w-4 h-4 text-success-500" />
                {container.deliveredVehicles} delivered
              </span>
              <span className="flex items-center gap-1">
                <AlertCircle className="w-4 h-4 text-danger-500" />
                {undeliveredVehicles.length} pending
              </span>
            </div>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <StatusBadge status={container.status} />
          {canDeliver && (
            <Button variant="success" leftIcon={<Play className="w-4 h-4" />} onClick={() => { setDeliverVin(''); setShowDeliverModal(true); }}>
              {t('containers.startDelivery')}
            </Button>
          )}
          <Button variant="outline" leftIcon={<Download className="w-4 h-4" />} onClick={() => reportMutation.mutate()} loading={reportMutation.isPending}>
            {t('reports.generateReport')}
          </Button>
        </div>
      </div>

      {/* Progress Bar */}
      <Card>
        <CardBody className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Delivery Progress</h3>
            <span className="text-2xl font-bold text-gray-900 dark:text-white">{container.completionPercentage.toFixed(1)}%</span>
          </div>
          <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
            <div 
              className={clsx(
                'h-full rounded-full transition-all duration-500',
                container.status === 'FullyDelivered' ? 'bg-success-500' :
                container.status === 'InProgress' ? 'bg-primary-500' : 'bg-gray-300'
              )}
              style={{ width: `${container.completionPercentage}%` }}
            />
          </div>
          <div className="flex justify-between text-sm text-gray-500 dark:text-gray-400 mt-2">
            <span>{container.deliveredVehicles} / {container.totalVehicles} delivered</span>
            {undeliveredVehicles.length > 0 && (
              <span className="flex items-center gap-1 text-warning-600 dark:text-warning-400">
                <WarningIcon className="w-4 h-4" />
                {t('vehicles.warning')}
              </span>
            )}
          </div>
        </CardBody>
      </Card>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <Card>
          <CardBody className="p-6 text-center">
            <Package className="w-10 h-10 text-primary-600 mx-auto mb-2" />
            <p className="text-3xl font-bold text-gray-900 dark:text-white">{container.totalVehicles}</p>
            <p className="text-gray-500 dark:text-gray-400">{t('containers.totalVehicles')}</p>
          </CardBody>
        </Card>
        <Card>
          <CardBody className="p-6 text-center">
            <CheckCircle className="w-10 h-10 text-success-600 mx-auto mb-2" />
            <p className="text-3xl font-bold text-success-600">{container.deliveredVehicles}</p>
            <p className="text-gray-500 dark:text-gray-400">{t('vehicles.deliveredVehicles')}</p>
          </CardBody>
        </Card>
        <Card>
          <CardBody className="p-6 text-center">
            <AlertCircle className="w-10 h-10 text-danger-600 mx-auto mb-2" />
            <p className="text-3xl font-bold text-danger-600">{undeliveredVehicles.length}</p>
            <p className="text-gray-500 dark:text-gray-400">{t('vehicles.undeliveredVehicles')}</p>
          </CardBody>
        </Card>
      </div>

      {/* Vehicles Table */}
      <Card>
        <CardHeader className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-4">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
            Vehicles ({vehicles.length})
          </h2>
          {canDeliver && (
            <Button variant="primary" leftIcon={<Truck className="w-4 h-4" />} onClick={() => { setDeliverVin(''); setShowDeliverModal(true); }}>
              {t('vehicles.deliver')}
            </Button>
          )}
        </CardHeader>
        <CardBody className="pt-0">
          <Table
            columns={vehicleColumns}
            data={vehicles}
            keyExtractor={(v) => v.id.toString()}
            isLoading={isLoading}
            emptyMessage={t('vehicles.noVehicles')}
            hoverable
          />
        </CardBody>
      </Card>

      {/* Deliver Vehicle Modal */}
      <Modal
        isOpen={showDeliverModal}
        onClose={() => setShowDeliverModal(false)}
        title={t('vehicles.markDelivered')}
        size="md"
      >
        <form onSubmit={(e) => { 
          e.preventDefault(); 
          deliverMutation.mutate({
            vin: deliverVin,
            containerId,
            deliveryMethod: deliverMethod,
            notes: deliverNotes,
            scannedVin: deliverMethod === 'BarcodeScan' ? scannedVin : undefined,
          });
        }} className="space-y-4">
          <Input
            label={t('vehicles.vin')}
            placeholder="Enter or scan VIN"
            value={deliverVin}
            onChange={(e) => setDeliverVin(e.target.value.toUpperCase())}
            required
            disabled={!!container?.vehicles?.find(v => v.vin === deliverVin && v.isDelivered)}
          />
          <div>
            <label className="label">{t('vehicles.deliveryMethod')}</label>
            <div className="flex gap-4">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name="deliveryMethod"
                  value="Manual"
                  checked={deliverMethod === 'Manual'}
                  onChange={() => setDeliverMethod('Manual')}
                  className="w-4 h-4 text-primary-600 border-gray-300 focus:ring-primary-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">{t('vehicles.manual')}</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name="deliveryMethod"
                  value="BarcodeScan"
                  checked={deliverMethod === 'BarcodeScan'}
                  onChange={() => setDeliverMethod('BarcodeScan')}
                  className="w-4 h-4 text-primary-600 border-gray-300 focus:ring-primary-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">{t('vehicles.barcodeScan')}</span>
              </label>
            </div>
          </div>
          {deliverMethod === 'BarcodeScan' && (
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <Button 
                  variant="outline" 
                  onClick={() => setIsScanning(true)}
                  loading={isScanning}
                  leftIcon={<ScanBarcode className="w-4 h-4" />}
                  fullWidth
                >
                  {isScanning ? 'Scanning...' : t('vehicles.scanVIN')}
                </Button>
              </div>
              <Input
                label={t('vehicles.scannedVin') + ' (for verification)'}
                placeholder="Scanned VIN will appear here"
                value={scannedVin}
                onChange={(e) => setScannedVin(e.target.value.toUpperCase())}
                required
                disabled={!isScanning}
              />
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {t('vehicles.vinMismatch')}
              </p>
            </div>
          )}
          <Input
            label={t('vehicles.notes') + ' (optional)'}
            placeholder="Delivery notes..."
            value={deliverNotes}
            onChange={(e) => setDeliverNotes(e.target.value)}
          />
          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="secondary" onClick={() => setShowDeliverModal(false)} disabled={deliverMutation.isPending}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="primary" loading={deliverMutation.isPending}>
              {t('vehicles.markDelivered')}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};