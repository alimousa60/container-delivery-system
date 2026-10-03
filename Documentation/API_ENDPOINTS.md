# API Endpoints - Container Vehicle Delivery Management System

## Base Configuration
- **Base URL**: `https://api.container-delivery.com/api/v1`
- **Authentication**: JWT Bearer Token
- **Content-Type**: `application/json`
- **Rate Limiting**: 100 requests/minute per user

## Authentication Endpoints

### POST /auth/login
User login with email/password. Returns JWT + Refresh Token.

**Request:**
```json
{
  "email": "user@company.com",
  "password": "securePassword123",
  "rememberMe": false
}
```

**Response (200):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJl...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": {
    "id": 1,
    "email": "user@company.com",
    "fullName": "John Doe",
    "roles": ["DeliveryUser"],
    "isMfaEnabled": true
  }
}
```

### POST /auth/refresh
Refresh access token using refresh token.

**Request:**
```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJl..."
}
```

### POST /auth/logout
Revoke refresh token.

### POST /auth/mfa/setup
Setup MFA for current user.

**Response:**
```json
{
  "secret": "JBSWY3DPEHPK3PXP",
  "qrCodeUrl": "otpauth://totp/ContainerDelivery:user@company.com?secret=JBSWY3DPEHPK3PXP&issuer=ContainerDelivery",
  "recoveryCodes": ["ABC123", "DEF456", "GHI789"]
}
```

### POST /auth/mfa/verify
Verify MFA code during login.

**Request:**
```json
{
  "code": "123456",
  "rememberDevice": true
}
```

### POST /auth/mfa/disable
Disable MFA for current user.

**Request:**
```json
{
  "password": "currentPassword",
  "code": "123456"
}
```

### POST /auth/forgot-password
Request password reset.

### POST /auth/reset-password
Reset password with token.

## User Management (Admin Only)

### GET /users
Get paginated list of users.

**Query Parameters:**
- `page` (default: 1)
- `pageSize` (default: 20, max: 100)
- `search` - search email/name
- `role` - filter by role
- `isActive` - filter by status

**Response:**
```json
{
  "items": [
    {
      "id": 1,
      "email": "user@company.com",
      "fullName": "John Doe",
      "phoneNumber": "+1234567890",
      "isActive": true,
      "isMfaEnabled": true,
      "roles": ["DeliveryUser"],
      "createdAt": "2024-01-15T10:30:00Z",
      "lastLoginAt": "2024-01-20T08:15:00Z"
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

### GET /users/{id}
Get user by ID.

### POST /users
Create new user (Admin only).

**Request:**
```json
{
  "email": "newuser@company.com",
  "password": "TempPass123!",
  "fullName": "Jane Smith",
  "phoneNumber": "+1987654321",
  "roles": ["DeliveryUser"]
}
```

### PUT /users/{id}
Update user.

### DELETE /users/{id}
Deactivate user (soft delete).

### POST /users/{id}/roles
Assign role to user.

### DELETE /users/{id}/roles/{roleId}
Remove role from user.

### PUT /users/{id}/password
Admin password reset.

## Container Endpoints

### GET /containers
Get paginated list of containers with filters.

**Query Parameters:**
- `page`, `pageSize`
- `status` - NotStarted, InProgress, FullyDelivered
- `search` - container number
- `sortBy` - createdAt, containerNumber, status
- `sortOrder` - asc, desc

**Response:**
```json
{
  "items": [
    {
      "id": 1,
      "containerNumber": "MCDU5018850_1",
      "totalVehicles": 4,
      "deliveredVehicles": 3,
      "status": "InProgress",
      "completionPercentage": 75.0,
      "createdBy": "John Doe",
      "createdAt": "2024-01-15T10:30:00Z",
      "startedAt": "2024-01-16T08:00:00Z",
      "completedAt": null
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20
}
```

### GET /containers/dashboard/stats
Get dashboard statistics.

**Response:**
```json
{
  "totalContainers": 150,
  "totalVehicles": 1250,
  "pendingContainers": 45,
  "completedContainers": 80,
  "inProgressContainers": 25,
  "deliveredVehicles": 980,
  "undeliveredVehicles": 270
}
```

### GET /containers/{id}
Get container details with all vehicles.

**Response:**
```json
{
  "id": 1,
  "containerNumber": "MCDU5018850_1",
  "totalVehicles": 4,
  "deliveredVehicles": 3,
  "status": "InProgress",
  "completionPercentage": 75.0,
  "createdBy": "John Doe",
  "createdAt": "2024-01-15T10:30:00Z",
  "startedAt": "2024-01-16T08:00:00Z",
  "completedAt": null,
  "notes": "Handle with care",
  "vehicles": [
    {
      "id": 1,
      "vin": "WDBUF56X78B358793",
      "description": "MERCEDES-BENZ E350 2008",
      "isDelivered": true,
      "deliveredAt": "2024-01-16T09:30:00Z",
      "deliveredBy": "John Doe"
    },
    {
      "id": 2,
      "vin": "2C3CDYBT5DH685657",
      "description": "DODGE CHALLENGER R 2013",
      "isDelivered": false,
      "deliveredAt": null,
      "deliveredBy": null
    }
  ]
}
```

### POST /containers
Create new container (Admin only).

**Request:**
```json
{
  "containerNumber": "NEWCONT123",
  "notes": "Optional notes"
}
```

### PUT /containers/{id}
Update container notes (Admin only).

### DELETE /containers/{id}
Delete container and all vehicles (Admin only, only if FullyDelivered).

### GET /containers/{id}/vehicles
Get vehicles for a container (paginated).

### POST /containers/import
Import vehicles from Excel file.

**Request:** Multipart/form-data
- `file`: Excel file (.xlsx)
- `containerNumberPrefix`: Optional prefix for container numbers

**Response:**
```json
{
  "importBatchId": 1,
  "status": "Processing",
  "message": "Import started. 150 records queued."
}
```

### GET /containers/import/{importBatchId}/status
Get import progress.

**Response:**
```json
{
  "id": 1,
  "fileName": "vehicles.xlsx",
  "status": "Completed",
  "totalRecords": 150,
  "successfulRecords": 148,
  "failedRecords": 2,
  "errorDetails": [
    {"row": 5, "error": "Duplicate VIN: WDBUF56X78B358793"},
    {"row": 12, "error": "Invalid VIN format"}
  ],
  "importedAt": "2024-01-15T10:30:00Z",
  "completedAt": "2024-01-15T10:30:05Z"
}
```

## Vehicle Endpoints

### GET /vehicles/search
Search vehicles by VIN, container number, or description.

**Query Parameters:**
- `q` - search query
- `page`, `pageSize`

**Response:**
```json
{
  "items": [
    {
      "id": 1,
      "vin": "WDBUF56X78B358793",
      "description": "MERCEDES-BENZ E350 2008",
      "containerId": 1,
      "containerNumber": "MCDU5018850_1",
      "isDelivered": true,
      "deliveredAt": "2024-01-16T09:30:00Z"
    }
  ],
  "totalCount": 1
}
```

### GET /vehicles/{id}
Get vehicle details.

### POST /vehicles/deliver
Confirm vehicle delivery.

**Request:**
```json
{
  "vin": "WDBUF56X78B358793",
  "containerId": 1,
  "deliveryMethod": "BarcodeScan", // or "Manual"
  "scannedVin": "WDBUF56X78B358793", // for barcode verification
  "notes": "Delivered to bay 3"
}
```

**Response:**
```json
{
  "success": true,
  "vehicle": {
    "id": 1,
    "vin": "WDBUF56X78B358793",
    "description": "MERCEDES-BENZ E350 2008",
    "isDelivered": true,
    "deliveredAt": "2024-01-16T10:45:00Z",
    "deliveredBy": "John Doe"
  },
  "container": {
    "id": 1,
    "containerNumber": "MCDU5018850_1",
    "totalVehicles": 4,
    "deliveredVehicles": 4,
    "status": "FullyDelivered",
    "completionPercentage": 100.0
  },
  "warning": null // or "Warning: There are still vehicles pending delivery in this container."
}
```

### POST /vehicles/deliver/batch
Deliver multiple vehicles at once.

**Request:**
```json
{
  "containerId": 1,
  "deliveries": [
    {"vin": "VIN1", "deliveryMethod": "BarcodeScan", "scannedVin": "VIN1"},
    {"vin": "VIN2", "deliveryMethod": "Manual"}
  ]
}
```

### PUT /vehicles/{id}/undeliver
Mark vehicle as undelivered (Admin only).

## Report Endpoints

### POST /reports/container/{containerId}
Generate PDF report for container.

**Response:**
```json
{
  "reportId": 1,
  "fileName": "Container_MCDU5018850_1_Report_20240116.pdf",
  "downloadUrl": "https://storage.blob.core.windows.net/reports/...",
  "generatedAt": "2024-01-16T10:45:00Z"
}
```

### GET /reports/container/{containerId}
Get list of reports for container.

### GET /reports/download/{reportId}
Download report file (returns file stream).

### POST /reports/bulk
Generate reports for multiple containers.

## Audit Log Endpoints (Admin Only)

### GET /audit-logs
Get paginated audit logs.

**Query Parameters:**
- `page`, `pageSize`
- `userId`
- `action`
- `entityType`
- `entityId`
- `fromDate`, `toDate`

**Response:**
```json
{
  "items": [
    {
      "id": 1,
      "userId": 1,
      "userEmail": "john@company.com",
      "action": "Delivery",
      "entityType": "Vehicle",
      "entityId": 5,
      "oldValues": null,
      "newValues": "{\"isDelivered\":true,\"deliveredAt\":\"2024-01-16T10:45:00Z\"}",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "timestamp": "2024-01-16T10:45:00Z"
    }
  ],
  "totalCount": 10000
}
```

### GET /audit-logs/export
Export audit logs to Excel.

## Health Check

### GET /health
System health check.

**Response:**
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "storage": "Healthy"
  },
  "timestamp": "2024-01-16T10:45:00Z"
}
```

## Error Responses

### 400 Bad Request
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "email": ["Email is required"],
    "password": ["Password must be at least 8 characters"]
  }
}
```

### 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid or expired token"
}
```

### 403 Forbidden
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Insufficient permissions"
}
```

### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Container not found"
}
```

### 429 Too Many Requests
```json
{
  "type": "https://tools.ietf.org/html/rfc6585",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Try again in 60 seconds."
}
```

## WebSocket Endpoints (Real-time Updates)

### WS /ws/containers/{containerId}
Real-time container updates (delivery status changes).

**Messages:**
```json
// Server -> Client: Vehicle delivered
{
  "type": "VehicleDelivered",
  "payload": {
    "vehicleId": 5,
    "vin": "WDBUF56X78B358793",
    "deliveredBy": "John Doe",
    "deliveredAt": "2024-01-16T10:45:00Z",
    "container": {
      "id": 1,
      "deliveredVehicles": 4,
      "totalVehicles": 4,
      "status": "FullyDelivered"
    }
  }
}
```

```json
// Server -> Client: Container status changed
{
  "type": "ContainerStatusChanged",
  "payload": {
    "containerId": 1,
    "oldStatus": "InProgress",
    "newStatus": "FullyDelivered"
  }
}
```