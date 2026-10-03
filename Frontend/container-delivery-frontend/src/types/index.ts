export interface User {
  id: number;
  email: string;
  fullName: string;
  phoneNumber?: string;
  isActive: boolean;
  isMfaEnabled: boolean;
  roles: string[];
  createdAt: string;
  lastLoginAt?: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface MfaVerifyRequest {
  userId: number;
  code: string;
  rememberDevice?: boolean;
}

export interface AuthResponse {
  success: boolean;
  accessToken?: string;
  refreshToken?: string;
  expiresIn?: number;
  requiresMfa?: boolean;
  user?: User;
  error?: string;
}

export interface MfaSetupResponse {
  secret: string;
  qrCodeUrl: string;
  recoveryCodes: string[];
}

export interface UserDto {
  id: number;
  email: string;
  fullName: string;
  phoneNumber?: string;
  isActive: boolean;
  isMfaEnabled: boolean;
  roles: string[];
  createdAt: string;
  lastLoginAt?: string;
}

export interface Container {
  id: number;
  containerNumber: string;
  totalVehicles: number;
  deliveredVehicles: number;
  status: ContainerStatus;
  completionPercentage: number;
  createdBy: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  notes?: string;
  vehicles?: Vehicle[];
}

export type ContainerStatus = 'NotStarted' | 'InProgress' | 'FullyDelivered';

export interface Vehicle {
  id: number;
  vin: string;
  description: string;
  containerId: number;
  containerNumber?: string;
  isDelivered: boolean;
  deliveredAt?: string;
  deliveredBy?: string;
  deliveryMethod?: DeliveryMethod;
}

export type DeliveryMethod = 'BarcodeScan' | 'Manual';

export interface DashboardStats {
  totalContainers: number;
  totalVehicles: number;
  pendingContainers: number;
  completedContainers: number;
  inProgressContainers: number;
  deliveredVehicles: number;
  undeliveredVehicles: number;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ImportBatch {
  id: number;
  fileName: string;
  totalRecords: number;
  successfulRecords: number;
  failedRecords: number;
  status: ImportStatus;
  errorDetails?: string;
  importedAt: string;
  completedAt?: string;
}

export type ImportStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';

export interface ImportErrorDto {
  row: number;
  field: string;
  value: string;
  error: string;
}

export interface ImportResultDto {
  importBatchId: number;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  duplicateVins: number;
  containersCreated: number;
  vehiclesImported: number;
  errors: ImportErrorDto[];
  processingTime: string;
}

export interface ImportBatchStatusDto {
  id: number;
  fileName: string;
  status: ImportStatus;
  totalRecords: number;
  successfulRecords: number;
  failedRecords: number;
  importedAt: string;
  completedAt?: string;
  errors: ImportErrorDto[];
}

export interface ContainerReport {
  id: number;
  containerId: number;
  containerNumber: string;
  fileName: string;
  generatedAt: string;
  generatedBy: string;
  totalVehicles: number;
  deliveredVehicles: number;
  undeliveredVehicles: number;
  completionPercentage: number;
}

export interface AuditLog {
  id: number;
  userId?: number;
  userEmail?: string;
  action: AuditAction;
  entityType: EntityType;
  entityId?: number;
  oldValues?: string;
  newValues?: string;
  ipAddress?: string;
  userAgent?: string;
  timestamp: string;
}

export type AuditAction = 
  | 'Create' | 'Update' | 'Delete' | 'Login' | 'Logout' 
  | 'Import' | 'Export' | 'Delivery' | 'Undelivery' 
  | 'ReportGeneration' | 'PasswordChange' | 'MfaSetup' 
  | 'MfaDisable' | 'RoleAssignment' | 'UserActivation' 
  | 'ContainerDeletion';

export type EntityType = 
  | 'User' | 'Container' | 'Vehicle' | 'DeliveryRecord' 
  | 'Report' | 'ImportBatch' | 'Role';

export interface DeliverVehicleRequest {
  vin: string;
  containerId: number;
  deliveryMethod: DeliveryMethod;
  notes?: string;
  scannedVin?: string;
}

export interface BatchDeliveryItemRequest {
  vin: string;
  method: DeliveryMethod;
  scannedVin?: string;
  notes?: string;
}

export interface BatchDeliveryRequest {
  containerId: number;
  items: BatchDeliveryItemRequest[];
}

export interface DeliveryResponse {
  success: boolean;
  vehicle?: Vehicle;
  container?: Container;
  warning?: string;
  error?: string;
}

export interface BatchDeliveryResponse {
  successCount: number;
  failureCount: number;
  errors: BatchDeliveryErrorDto[];
}

export interface BatchDeliveryErrorDto {
  vin: string;
  error: string;
}

export interface CreateContainerRequest {
  containerNumber: string;
  notes?: string;
}

export interface UpdateContainerRequest {
  notes?: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  fullName: string;
  phoneNumber?: string;
  roles: string[];
}

export interface UpdateUserRequest {
  fullName: string;
  phoneNumber?: string;
  isActive: boolean;
}

export interface AssignRoleRequest {
  role: string;
}

export interface AdminResetPasswordRequest {
  newPassword: string;
}

export interface ToastMessage {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  message: string;
  duration?: number;
}

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  children: React.ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full';
}

export interface TableColumn<T> {
  key: string;
  header: string;
  render?: (item: T) => React.ReactNode;
  className?: string;
  sortable?: boolean;
}

export interface SelectOption {
  value: string;
  label: string;
}