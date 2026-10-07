import axios, { AxiosError, AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/authStore';
import type { 
  User, 
  AuthTokens, 
  LoginRequest, 
  MfaVerifyRequest, 
  AuthResponse, 
  MfaSetupResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  Container,
  Vehicle,
  DashboardStats,
  PagedResponse,
  ImportResultDto,
  ImportBatchStatusDto,
  ImportBatch,
  ContainerReport,
  BulkReportResult,
  BulkReportError,
  User as UserType,
  AuditLog,
  AuditAction,
  EntityType,
  DeliverVehicleRequest,
  BatchDeliveryRequest,
  DeliveryResponse,
  BatchDeliveryResponse,
  CreateContainerRequest,
  UpdateContainerRequest,
  CreateUserRequest,
  UpdateUserRequest,
  AssignRoleRequest,
  AdminResetPasswordRequest,
  ImportResultDto as ImportResultDtoType,
  ImportBatchStatusDto as ImportBatchStatusDtoType,
  CompanySettingsDto,
  UpdateSettingsRequest
} from '@/types';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api/v1';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason: unknown) => void;
}> = [];

const processQueue = (error: Error | null, token: string | null = null) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) {
      reject(error);
    } else {
      resolve(token);
    }
  });
  failedQueue = [];
};

api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const tokens = useAuthStore.getState().tokens;
    if (tokens?.accessToken) {
      config.headers.Authorization = `Bearer ${tokens.accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const { tokens } = useAuthStore.getState();
        const refreshToken = tokens?.refreshToken;
        if (!refreshToken) {
          throw new Error('No refresh token');
        }

        const response = await axios.post(`${API_BASE_URL}/auth/refresh`, { refreshToken });
        const { accessToken, refreshToken: newRefreshToken, expiresIn } = response.data;

        useAuthStore.getState().setTokens({
          accessToken,
          refreshToken: newRefreshToken,
          expiresAt: Date.now() + (expiresIn || 900) * 1000,
        });

        processQueue(null, accessToken);

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        }

        return api(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError as Error, null);
        useAuthStore.getState().clearAuth();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export const authService = {
  login: async (credentials: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post('/auth/login', credentials);
    return response.data;
  },

  verifyMfa: async (data: MfaVerifyRequest): Promise<AuthResponse> => {
    const response = await api.post('/auth/mfa/verify', data);
    return response.data;
  },

  refreshToken: async (refreshToken: string): Promise<AuthResponse> => {
    const response = await api.post('/auth/refresh', { refreshToken });
    return response.data;
  },

  logout: async (refreshToken: string): Promise<void> => {
    await api.post('/auth/logout', { refreshToken });
  },

  setupMfa: async (): Promise<MfaSetupResponse> => {
    const response = await api.post('/auth/mfa/setup');
    return response.data;
  },

  verifyMfaSetup: async (code: string): Promise<AuthResponse> => {
    const response = await api.post('/auth/mfa/verify-setup', { code });
    return response.data;
  },

  disableMfa: async (password: string, code: string): Promise<void> => {
    await api.post('/auth/mfa/disable', { password, code });
  },

  changePassword: async (currentPassword: string, newPassword: string): Promise<void> => {
    await api.post('/auth/change-password', { currentPassword, newPassword });
  },

  forgotPassword: async (email: string): Promise<void> => {
    await api.post('/auth/forgot-password', { email });
  },

  resetPassword: async (token: string, newPassword: string): Promise<void> => {
    await api.post('/auth/reset-password', { token, newPassword });
  },
};

export const containerService = {
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    status?: string;
    search?: string;
    sortBy?: string;
    sortOrder?: string;
  }): Promise<PagedResponse<Container>> => {
    const response = await api.get('/containers', { params });
    return response.data;
  },

  getDashboardStats: async (): Promise<DashboardStats> => {
    const response = await api.get('/containers/dashboard/stats');
    return response.data;
  },

  getById: async (id: number): Promise<Container> => {
    const response = await api.get(`/containers/${id}`);
    return response.data;
  },

  create: async (data: CreateContainerRequest): Promise<Container> => {
    const response = await api.post('/containers', data);
    return response.data;
  },

  update: async (id: number, data: UpdateContainerRequest): Promise<Container> => {
    const response = await api.put(`/containers/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await api.delete(`/containers/${id}`);
  },

  startDelivery: async (id: number): Promise<Container> => {
    const response = await api.post(`/containers/${id}/start-delivery`);
    return response.data;
  },

  getVehicles: async (containerId: number): Promise<PagedResponse<Container>> => {
    const response = await api.get(`/containers/${containerId}/vehicles`);
    return response.data;
  },

  importVehicles: async (file: File, containerNumberPrefix?: string): Promise<ImportResultDtoType> => {
    const formData = new FormData();
    formData.append('file', file);
    if (containerNumberPrefix) {
      formData.append('containerNumberPrefix', containerNumberPrefix);
    }
    const response = await api.post('/containers/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  getImportStatus: async (importBatchId: number): Promise<ImportBatchStatusDtoType> => {
    const response = await api.get(`/containers/import/${importBatchId}/status`);
    return response.data;
  },

  getImports: async (params?: { page?: number; pageSize?: number }): Promise<PagedResponse<ImportBatch>> => {
    const response = await api.get('/containers/import', { params });
    return response.data;
  },

  generateReport: async (containerId: number): Promise<ContainerReport> => {
    const response = await api.post(`/reports/container/${containerId}`);
    return response.data;
  },

  getReports: async (containerId: number): Promise<ContainerReport[]> => {
    const response = await api.get(`/reports/container/${containerId}`);
    return response.data;
  },

  downloadReport: async (reportId: number): Promise<Blob> => {
    const response = await api.get(`/reports/download/${reportId}`, { responseType: 'blob' });
    return response.data;
  },

  generateBulkReports: async (containerIds: number[]): Promise<BulkReportResult> => {
    const response = await api.post('/reports/bulk', { containerIds });
    return response.data;
  },
};

export const vehicleService = {
  search: async (query: string, page = 1, pageSize = 20): Promise<PagedResponse<Vehicle>> => {
    const response = await api.get('/vehicles/search', { params: { q: query, page, pageSize } });
    return response.data;
  },

  deliver: async (data: DeliverVehicleRequest): Promise<DeliveryResponse> => {
    const response = await api.post('/vehicles/deliver', data);
    return response.data;
  },

  deliverBatch: async (containerId: number, items: Array<{
    vin: string;
    deliveryMethod: 'BarcodeScan' | 'Manual';
    scannedVin?: string;
    notes?: string;
  }>): Promise<BatchDeliveryResponse> => {
    const response = await api.post('/vehicles/deliver/batch', { containerId, deliveries: items });
    return response.data;
  },

  undeliver: async (vehicleId: number, reason?: string): Promise<Vehicle> => {
    const response = await api.put(`/vehicles/${vehicleId}/undeliver`, { reason });
    return response.data;
  },
};

export const reportService = {
  generate: async (containerId: number): Promise<ContainerReport> => {
    const response = await api.post(`/reports/container/${containerId}`);
    return response.data;
  },

  getByContainer: async (containerId: number): Promise<ContainerReport[]> => {
    const response = await api.get(`/reports/container/${containerId}`);
    return response.data;
  },

  download: async (reportId: number): Promise<Blob> => {
    const response = await api.get(`/reports/download/${reportId}`, { responseType: 'blob' });
    return response.data;
  },

  generateBulk: async (containerIds: number[]): Promise<BulkReportResult> => {
    const response = await api.post('/reports/bulk', { containerIds });
    return response.data;
  },

  getAll: async (params?: { page?: number; pageSize?: number }): Promise<PagedResponse<ContainerReport>> => {
    const response = await api.get('/reports', { params });
    return response.data;
  },

  delete: async (reportId: number): Promise<void> => {
    await api.delete(`/reports/${reportId}`);
  },
};

export const userService = {
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    search?: string;
    role?: string;
    isActive?: boolean;
  }): Promise<PagedResponse<UserType>> => {
    const response = await api.get('/users', { params });
    return response.data;
  },

  getById: async (id: number): Promise<UserType> => {
    const response = await api.get(`/users/${id}`);
    return response.data;
  },

  create: async (data: CreateUserRequest): Promise<UserType> => {
    const response = await api.post('/users', data);
    return response.data;
  },

  update: async (id: number, data: UpdateUserRequest): Promise<UserType> => {
    const response = await api.put(`/users/${id}`, data);
    return response.data;
  },

  assignRole: async (userId: number, role: string): Promise<void> => {
    await api.post(`/users/${userId}/roles`, { role });
  },

  removeRole: async (userId: number, roleId: number): Promise<void> => {
    await api.delete(`/users/${userId}/roles/${roleId}`);
  },

  resetPassword: async (id: number, newPassword: string): Promise<void> => {
    await api.put(`/users/${id}/password`, { newPassword });
  },

  delete: async (id: number): Promise<void> => {
    await api.delete(`/users/${id}`);
  },
};

export const auditService = {
  getAll: async (params?: {
    page?: number;
    pageSize?: number;
    userId?: number;
    action?: string;
    entityType?: string;
    entityId?: number;
    fromDate?: string;
    toDate?: string;
  }): Promise<PagedResponse<AuditLog>> => {
    const response = await api.get('/audit-logs', { params });
    return response.data;
  },

  export: async (fromDate?: string, toDate?: string): Promise<Blob> => {
    const response = await api.get('/audit-logs/export', { 
      params: { fromDate, toDate },
      responseType: 'blob',
    });
    return response.data;
  },
};

export const settingsService = {
  getAll: async (): Promise<CompanySettingsDto> => {
    const response = await api.get('/settings');
    return response.data;
  },

  update: async (data: UpdateSettingsRequest): Promise<CompanySettingsDto> => {
    const response = await api.put('/settings', data);
    return response.data;
  },

  uploadLogo: async (file: File): Promise<CompanySettingsDto> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post('/settings/logo', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  getCompanyName: async (): Promise<string> => {
    const response = await api.get('/settings/company-name');
    return response.data;
  },

  getLogoUrl: async (): Promise<string | null> => {
    const response = await api.get('/settings/logo');
    return response.data;
  },

  getReportFooter: async (): Promise<string> => {
    const response = await api.get('/settings/report-footer');
    return response.data;
  },
};

export default api;