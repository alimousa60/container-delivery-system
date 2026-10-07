import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { containerService } from '@/services/api';
import { Card, CardBody } from '@/components/ui/Card';
import { LoadingSpinner, CardSkeleton } from '@/components/ui/LoadingSpinner';
import { DashboardStats } from '@/types';
import { 
  Box, 
  Truck, 
  Package, 
  CheckCircle, 
  AlertCircle, 
  Clock, 
  AlertTriangle,
  TrendingUp,
  FileText
} from 'lucide-react';
import { clsx } from 'clsx';

interface StatCardProps {
  title: string;
  value: number;
  icon: React.ReactNode;
  color: string;
  bgColor: string;
  trend?: number;
  link?: string;
}

const StatCard: React.FC<StatCardProps> = ({ title, value, icon, color, bgColor, trend, link }) => (
  <Card hover={!!link} className={link ? 'cursor-pointer' : ''} onClick={link ? () => window.location.href = link : undefined}>
    <CardBody className="p-6">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm font-medium text-gray-500 dark:text-gray-400">{title}</p>
          <p className="text-3xl font-bold text-gray-900 dark:text-white mt-1">{value.toLocaleString()}</p>
          {trend !== undefined && (
            <p className={clsx('text-sm mt-1', trend >= 0 ? 'text-success-600' : 'text-danger-600')}>
              {trend >= 0 ? '+' : ''}{trend}% vs last month
            </p>
          )}
        </div>
        <div className={clsx('p-3 rounded-xl', bgColor)}>
          {icon}
        </div>
      </div>
    </CardBody>
  </Card>
);

export const DashboardPage: React.FC = () => {
  const { t } = useTranslation();

  const { data: stats, isLoading, error } = useQuery({
    queryKey: ['dashboardStats'],
    queryFn: () => containerService.getDashboardStats(),
    refetchInterval: 30000,
  });

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{t('dashboard.title')}</h1>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => <CardSkeleton key={i} />)}
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          {[...Array(3)].map((_, i) => <CardSkeleton key={i} />)}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <AlertCircle className="w-12 h-12 text-danger-500 mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">Failed to load dashboard</h2>
        <p className="text-gray-500 dark:text-gray-400">Please try again later</p>
      </div>
    );
  }

  const statsData = stats as DashboardStats;

  const statCards = [
    {
      title: t('dashboard.totalContainers'),
      value: statsData.totalContainers,
      icon: <Box className="w-8 h-8 text-primary-600" />,
      color: 'text-primary-600',
      bgColor: 'bg-primary-100 dark:bg-primary-900/30',
      link: '/containers',
    },
    {
      title: t('dashboard.totalVehicles'),
      value: statsData.totalVehicles,
      icon: <Truck className="w-8 h-8 text-success-600" />,
      color: 'text-success-600',
      bgColor: 'bg-success-100 dark:bg-success-900/30',
      link: '/vehicles',
    },
    {
      title: t('dashboard.deliveredVehicles'),
      value: statsData.deliveredVehicles,
      icon: <CheckCircle className="w-8 h-8 text-success-600" />,
      color: 'text-success-600',
      bgColor: 'bg-success-100 dark:bg-success-900/30',
    },
    {
      title: t('dashboard.undeliveredVehicles'),
      value: statsData.undeliveredVehicles,
      icon: <AlertTriangle className="w-8 h-8 text-warning-600" />,
      color: 'text-warning-600',
      bgColor: 'bg-warning-100 dark:bg-warning-900/30',
    },
    {
      title: t('dashboard.pendingContainers'),
      value: statsData.pendingContainers,
      icon: <Clock className="w-8 h-8 text-gray-600" />,
      color: 'text-gray-600',
      bgColor: 'bg-gray-100 dark:bg-gray-700',
      link: '/containers?status=NotStarted',
    },
    {
      title: t('dashboard.inProgressContainers'),
      value: statsData.inProgressContainers,
      icon: <Package className="w-8 h-8 text-primary-600" />,
      color: 'text-primary-600',
      bgColor: 'bg-primary-100 dark:bg-primary-900/30',
      link: '/containers?status=InProgress',
    },
    {
      title: t('dashboard.completedContainers'),
      value: statsData.completedContainers,
      icon: <CheckCircle className="w-8 h-8 text-success-600" />,
      color: 'text-success-600',
      bgColor: 'bg-success-100 dark:bg-success-900/30',
      link: '/containers?status=FullyDelivered',
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{t('dashboard.title')}</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Overview of your container delivery operations</p>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {statCards.map((stat, index) => (
          <StatCard key={index} {...stat} />
        ))}
      </div>

      {/* Quick Actions & Recent Activity */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Quick Actions */}
        <Card>
          <CardBody className="p-6">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">{t('dashboard.quickActions')}</h2>
            <div className="space-y-3">
              <a 
                href="/containers" 
                className="flex items-center gap-3 p-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
              >
                <div className="p-2 bg-primary-100 dark:bg-primary-900/30 rounded-lg">
                  <Box className="w-5 h-5 text-primary-600" />
                </div>
                <span className="font-medium text-gray-900 dark:text-white">{t('containers.createContainer')}</span>
              </a>
              <a 
                href="/import" 
                className="flex items-center gap-3 p-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
              >
                <div className="p-2 bg-success-100 dark:bg-success-900/30 rounded-lg">
                  <Truck className="w-5 h-5 text-success-600" />
                </div>
                <span className="font-medium text-gray-900 dark:text-white">{t('import.title')}</span>
              </a>
              <a 
                href="/reports" 
                className="flex items-center gap-3 p-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
              >
                <div className="p-2 bg-warning-100 dark:bg-warning-900/30 rounded-lg">
                  <FileText className="w-5 h-5 text-warning-600" />
                </div>
                <span className="font-medium text-gray-900 dark:text-white">{t('reports.generateReport')}</span>
              </a>
              <a 
                href="/vehicles/deliver" 
                className="flex items-center gap-3 p-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
              >
                <div className="p-2 bg-purple-100 dark:bg-purple-900/30 rounded-lg">
                  <Package className="w-5 h-5 text-purple-600" />
                </div>
                <span className="font-medium text-gray-900 dark:text-white">{t('vehicles.searchPlaceholder')}</span>
              </a>
            </div>
          </CardBody>
        </Card>

        {/* Container Status Summary */}
        <Card className="lg:col-span-2">
          <CardBody className="p-6">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Container Status Overview</h2>
            <div className="space-y-4">
              {[
                { status: 'NotStarted' as const, label: t('containers.notStarted'), count: statsData.pendingContainers, color: 'danger', icon: '🔴' },
                { status: 'InProgress' as const, label: t('containers.inProgress'), count: statsData.inProgressContainers, color: 'warning', icon: '🟡' },
                { status: 'FullyDelivered' as const, label: t('containers.fullyDelivered'), count: statsData.completedContainers, color: 'success', icon: '🟢' },
              ].map((item) => (
                <a 
                  key={item.status}
                  href={`/containers?status=${item.status}`}
                  className="flex items-center justify-between p-4 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors group"
                >
                  <div className="flex items-center gap-4">
                    <span className="text-2xl">{item.icon}</span>
                    <div>
                      <p className="font-medium text-gray-900 dark:text-white">{item.label}</p>
                      <p className="text-sm text-gray-500 dark:text-gray-400">{item.count} containers</p>
                    </div>
                  </div>
                  <div className="text-right group-hover:translate-x-1 transition-transform">
                    <p className="text-2xl font-bold text-gray-900 dark:text-white">{item.count}</p>
                    <span className="badge badge-{item.color}">{((item.count / statsData.totalContainers) * 100).toFixed(1)}%</span>
                  </div>
                </a>
              ))}
            </div>
          </CardBody>
        </Card>
      </div>
    </div>
  );
};