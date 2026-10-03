import React, { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { userService } from '@/services/api';
import { User, UserRole, PagedResponse } from '@/types';
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
  User, 
  Edit, 
  Trash2, 
  Shield, 
  ShieldCheck,
  MoreVertical,
  Key
} from 'lucide-react';
import { clsx } from 'clsx';

const roleOptions = [
  { value: 'Admin', label: 'Administrator' },
  { value: 'DeliveryUser', label: 'Delivery User' },
];

export const UsersPage: React.FC = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { success: showSuccess, error: showError } = useToast();
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    fullName: '',
    phoneNumber: '',
    roles: [] as string[],
    isActive: true,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['users', page, pageSize, search, roleFilter],
    queryFn: () => userService.getAll({ page, pageSize, search, role: roleFilter as UserRole, isActive: undefined }),
  });

  const createMutation = useMutation({
    mutationFn: (data: typeof formData) => userService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      showSuccess(t('users.createSuccess'));
      setShowCreateModal(false);
      setFormData({ email: '', password: '', fullName: '', phoneNumber: '', roles: [], isActive: true });
    },
    onError: (err: Error) => showError(err.message),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: typeof formData }) => userService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      showSuccess(t('users.updateSuccess'));
      setEditingUser(null);
    },
    onError: (err: Error) => showError(err.message),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => userService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      showSuccess(t('users.deleteSuccess'));
    },
    onError: (err: Error) => showError(err.message),
  });

  const assignRoleMutation = useMutation({
    mutationFn: ({ userId, role }: { userId: number; role: string }) => userService.assignRole(userId, role),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      showSuccess(t('users.roleAssigned'));
    },
    onError: (err: Error) => showError(err.message),
  });

  const removeRoleMutation = useMutation({
    mutationFn: ({ userId, roleId }: { userId: number; roleId: number }) => userService.removeRole(userId, roleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      showSuccess(t('users.roleRemoved'));
    },
    onError: (err: Error) => showError(err.message),
  });

  const columns: TableColumn<User>[] = [
    { 
      key: 'fullName', 
      header: t('users.fullName'), 
      render: (user) => (
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center">
            <User className="w-4 h-4 text-primary-600" />
          </div>
          <div>
            <p className="font-medium text-gray-900 dark:text-white">{user.fullName}</p>
            <p className="text-sm text-gray-500 dark:text-gray-400">{user.email}</p>
          </div>
        </div>
      ),
    },
    { 
      key: 'roles', 
      header: t('users.roles'), 
      render: (user) => (
        <div className="flex flex-wrap gap-1">
          {user.roles.map(role => (
            <span key={role} className={clsx(
              'badge',
              role === 'Admin' ? 'badge-info' : 'badge-gray'
            )}>
              {role === 'Admin' ? <ShieldCheck className="w-3 h-3" /> : <Shield className="w-3 h-3" />}
              {t(`users.${role.toLowerCase()}`) || role}
            </span>
          ))}
        </div>
      ),
    },
    { 
      key: 'isActive', 
      header: t('users.isActive'), 
      render: (user) => (
        <StatusBadge status={user.isActive ? 'FullyDelivered' : 'NotStarted'} />
      ),
    },
    { 
      key: 'isMfaEnabled', 
      header: 'MFA', 
      className: 'hidden md:table-cell',
      render: (user) => (
        <StatusBadge status={user.isMfaEnabled ? 'FullyDelivered' : 'NotStarted'} />
      ),
    },
    { 
      key: 'lastLoginAt', 
      header: t('users.lastLogin'), 
      className: 'hidden lg:table-cell',
      render: (user) => user.lastLoginAt ? (
        <span className="text-gray-500 dark:text-gray-400">{new Date(user.lastLoginAt).toLocaleString()}</span>
      ) : (
        <span className="text-gray-400 dark:text-gray-500">Never</span>
      ),
    },
    { 
      key: 'actions', 
      header: t('common.actions'),
      className: 'text-right',
      render: (user) => (
        <Dropdown
          trigger={
            <button className="p-1.5 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" aria-label="Actions">
              <MoreVertical className="w-5 h-5" />
            </button>
          }
          items={[
            { 
              label: t('users.editUser'), 
              icon: <Edit className="w-4 h-4" />, 
              onClick: () => { setEditingUser(user); setFormData({ email: user.email, password: '', fullName: user.fullName, phoneNumber: user.phoneNumber || '', roles: user.roles, isActive: user.isActive }); } 
            },
            { divider: true },
            ...user.roles.filter(r => r !== 'Admin').map(role => ({
              label: `Remove ${role}`,
              icon: <Shield className="w-4 h-4" />,
              onClick: () => {
                if (confirm(`Remove ${role} role from ${user.fullName}?`)) {
                  removeRoleMutation.mutate({ userId: user.id, roleId: role === 'Admin' ? 1 : 2 });
                }
              },
              danger: true,
            }),
            { divider: true },
            ...roleOptions.filter(r => !user.roles.includes(r.value)).map(role => ({
              label: `Add ${role.label}`,
              icon: <ShieldCheck className="w-4 h-4" />,
              onClick: () => assignRoleMutation.mutate({ userId: user.id, role: role.value }),
            })),
            { divider: true },
            { 
              label: t('auth.resetPassword'), 
              icon: <Key className="w-4 h-4" />, 
              onClick: () => { /* Password reset */ } 
            },
            { 
              label: t('common.delete'), 
              icon: <Trash2 className="w-4 h-4" />, 
              onClick: () => {
                if (confirm(t('users.deleteConfirm'))) {
                  deleteMutation.mutate(user.id);
                }
              },
              danger: true,
            },
          ]}
          align="right"
        />
      ),
    },
  ];

  const handleSearch = useCallback((value: string) => {
    setSearch(value);
    setPage(1);
  }, []);

  const handleRoleChange = useCallback((value: string) => {
    setRoleFilter(value as UserRole);
    setPage(1);
  }, []);

  const handlePageChange = useCallback((newPage: number) => {
    setPage(newPage);
  }, []);

  const handleRoleToggle = (role: string) => {
    setFormData(prev => ({
      ...prev,
      roles: prev.roles.includes(role) ? prev.roles.filter(r => r !== role) : [...prev.roles, role],
    }));
  };

  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;
  const totalCount = data?.totalCount || 0;

  const resetForm = () => {
    setFormData({ email: '', password: '', fullName: '', phoneNumber: '', roles: [], isActive: true });
    setEditingUser(null);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{t('users.title')}</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Manage system users and their roles</p>
        </div>
        <Button onClick={() => { resetForm(); setShowCreateModal(true); }} leftIcon={<Plus className="w-4 h-4" />}>
          {t('users.createUser')}
        </Button>
      </div>

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
              value={roleFilter}
              onChange={(e) => handleRoleChange(e.target.value)}
              className="input w-auto min-w-[180px]"
            >
              <option value="">{t('common.allStatuses')}</option>
              {roleOptions.map(opt => (
                <option key={opt.value} value={opt.value}>{t(`users.${opt.value.toLowerCase()}`) || opt.label}</option>
              ))}
            </select>
          </div>
        </CardBody>
      </Card>

      <Card>
        {isLoading ? (
          <TableLoading columns={columns.length} />
        ) : (
          <Table
            columns={columns}
            data={data?.items || []}
            keyExtractor={(u) => u.id.toString()}
            isLoading={isLoading}
            emptyMessage={t('users.noUsers')}
            hoverable
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || !!editingUser}
        onClose={() => { setShowCreateModal(false); resetForm(); }}
        title={editingUser ? t('users.editUser') : t('users.createUser')}
        size="lg"
      >
        <form onSubmit={(e) => { 
          e.preventDefault(); 
          if (editingUser) {
            updateMutation.mutate({ id: editingUser.id, data: { ...formData, password: formData.password || undefined } });
          } else {
            createMutation.mutate(formData);
          }
        }} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label={t('users.email')}
              value={formData.email}
              onChange={(e) => setFormData(prev => ({ ...prev, email: e.target.value }))}
              required
              disabled={!!editingUser}
            />
            <Input
              label={t('users.phoneNumber')}
              value={formData.phoneNumber}
              onChange={(e) => setFormData(prev => ({ ...prev, phoneNumber: e.target.value }))}
              type="tel"
            />
          </div>
          <Input
            label={t('users.fullName')}
            value={formData.fullName}
            onChange={(e) => setFormData(prev => ({ ...prev, fullName: e.target.value }))}
            required
          />
          {!editingUser && (
            <Input
              label={t('auth.password')}
              type="password"
              value={formData.password}
              onChange={(e) => setFormData(prev => ({ ...prev, password: e.target.value }))}
              required
              autoComplete="new-password"
              helperText="At least 8 characters"
            />
          )}
          <div>
            <label className="label">{t('users.roles')}</label>
            <div className="flex flex-wrap gap-2">
              {roleOptions.map(role => (
                <label key={role.value} className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formData.roles.includes(role.value)}
                    onChange={() => handleRoleToggle(role.value)}
                    className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                  />
                  <span className="text-sm text-gray-700 dark:text-gray-300">{role.label}</span>
                </label>
              ))}
            </div>
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isActive"
              checked={formData.isActive}
              onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
              className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
            />
            <label htmlFor="isActive" className="text-sm text-gray-700 dark:text-gray-300">{t('users.isActive')}</label>
          </div>
          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="secondary" onClick={() => { setShowCreateModal(false); resetForm(); }} disabled={createMutation.isPending || updateMutation.isPending}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="primary" loading={createMutation.isPending || updateMutation.isPending}>
              {t('common.save')}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};