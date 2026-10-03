# Container Vehicle Delivery Management System

A secure, scalable web-based application for tracking vehicles inside shipping containers during delivery operations.

## Features

- **Vehicle & Container Tracking** - Track vehicles inside shipping containers with real-time status updates
- **Delivery Workflow** - Support for barcode scanning (camera) and manual delivery confirmation
- **Automated Status Management** - Containers automatically move from "In Progress" to "Fully Delivered" when all vehicles are delivered
- **PDF Report Generation** - Professional reports with delivery statistics and vehicle details
- **Excel Import** - Bulk import vehicles and containers from Excel files with validation
- **Search System** - Search by VIN, container number, or vehicle description
- **Audit Logging** - Complete audit trail of all actions with user attribution
- **Multi-Factor Authentication (MFA)** - TOTP-based 2FA with recovery codes
- **Role-Based Access Control** - Admin and Delivery User roles
- **Dark/Light Mode** - System preference or manual toggle
- **Multi-language** - English and Arabic (RTL support)

## Tech Stack

### Backend
- **Framework**: ASP.NET Core 8 Web API
- **Language**: C# 12
- **ORM**: Entity Framework Core 8
- **Database**: SQL Server (Azure SQL Database)
- **Authentication**: JWT Bearer Tokens + MFA (TOTP)
- **PDF Generation**: QuestPDF
- **Excel Processing**: ClosedXML
- **Caching**: Redis (Azure Cache for Redis)
- **Logging**: Serilog
- **API Documentation**: Swagger/OpenAPI

### Frontend
- **Framework**: React 18 with TypeScript
- **Build Tool**: Vite
- **Styling**: Tailwind CSS
- **State Management**: Zustand + React Query (TanStack Query)
- **Routing**: React Router v6
- **Forms**: React Hook Form + Zod validation
- **Internationalization**: i18next (Arabic/English)
- **Charts**: Recharts
- **Barcode Scanning**: @zxing/library (WebAssembly)

### Infrastructure (Azure)
- **Compute**: Azure App Service (Linux containers)
- **Database**: Azure SQL Database
- **Storage**: Azure Blob Storage
- **Cache**: Azure Cache for Redis
- **CDN**: Azure Front Door
- **Monitoring**: Application Insights + Log Analytics
- **CI/CD**: GitHub Actions

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker & Docker Compose
- SQL Server (or use Docker)
- Redis (or use Docker)

## Quick Start with Docker

```bash
# Clone repository
git clone https://github.com/yourusername/container-delivery-system.git
cd ContainerDeliverySystem

# Start all services
docker-compose up -d

# Access services
# Frontend: http://localhost:3000
# Backend API: http://localhost:5001
# Swagger UI: http://localhost:5001/swagger
# Mailhog: http://localhost:8025
```

## Manual Setup

### Backend
```bash
cd Backend

# Restore dependencies
dotnet restore ContainerDelivery.sln

# Update connection string in appsettings.Development.json
# Run migrations
cd src/ContainerDelivery.Api
dotnet ef database update

# Start API
dotnet run --urls "http://localhost:5001"
```

### Frontend
```bash
cd Frontend/container-delivery-frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

## Default Credentials

After running migrations, a default admin user is created:
- **Email**: admin@container-delivery.com
- **Password**: Admin123!
- **Role**: Administrator

Demo delivery user:
- **Email**: delivery@container-delivery.com
- **Password**: Delivery123!
- **Role**: Delivery User

## API Endpoints

### Authentication
- `POST /api/v1/auth/login` - User login
- `POST /api/v1/auth/refresh` - Refresh access token
- `POST /api/v1/auth/mfa/verify` - Verify MFA code
- `POST /api/v1/auth/mfa/setup` - Setup MFA
- `POST /api/v1/auth/logout` - Logout

### Containers
- `GET /api/v1/containers` - List containers (paginated, filtered)
- `GET /api/v1/containers/dashboard/stats` - Dashboard statistics
- `GET /api/v1/containers/{id}` - Get container details
- `POST /api/v1/containers` - Create container (Admin)
- `PUT /api/v1/containers/{id}` - Update container (Admin)
- `DELETE /api/v1/containers/{id}` - Delete container (Admin)
- `POST /api/v1/containers/{id}/start-delivery` - Start delivery
- `POST /api/v1/containers/import` - Import vehicles from Excel (Admin)

### Vehicles
- `GET /api/v1/vehicles/search` - Search vehicles
- `POST /api/v1/vehicles/deliver` - Deliver vehicle
- `POST /api/v1/vehicles/deliver/batch` - Batch deliver vehicles
- `PUT /api/v1/vehicles/{id}/undeliver` - Mark as undelivered (Admin)

### Reports
- `POST /api/v1/reports/container/{containerId}` - Generate PDF report
- `GET /api/v1/reports/container/{containerId}` - List reports
- `GET /api/v1/reports/download/{reportId}` - Download report
- `POST /api/v1/reports/bulk` - Generate bulk reports (Admin)

### Users (Admin)
- `GET /api/v1/users` - List users
- `POST /api/v1/users` - Create user
- `PUT /api/v1/users/{id}` - Update user
- `POST /api/v1/users/{id}/roles` - Assign role
- `DELETE /api/v1/users/{id}/roles/{roleId}` - Remove role
- `DELETE /api/v1/users/{id}` - Deactivate user

### Audit Logs (Admin)
- `GET /api/v1/audit-logs` - List audit logs
- `GET /api/v1/audit-logs/export` - Export to Excel

## Excel Import Format

Required columns in Excel file (.xlsx):
| Container Number | VIN | Vehicle Description |
|-----------------|-----|---------------------|
| MCDU5018850_1 | WDBUF56X78B358793 | MERCEDES-BENZ E350 2008 |
| MCDU5018850_1 | 2C3CDYBT5DH685657 | DODGE CHALLENGER R 2013 |

## Configuration

### Backend (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=ContainerDeliveryDB;User Id=sa;Password=...;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key",
    "Issuer": "https://api.yourdomain.com",
    "Audience": "https://api.yourdomain.com",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "Redis": {
    "ConnectionString": "your-redis:6380,password=...,ssl=True"
  },
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.provider.com",
    "SmtpPort": 587,
    "SmtpUser": "noreply@yourdomain.com",
    "SmtpPassword": "password",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Container Delivery System"
  },
  "AllowedOrigins": ["https://app.yourdomain.com"]
}
```

### Frontend (.env)
```env
VITE_API_URL=https://api.yourdomain.com/api/v1
```

## Deployment to Azure

Follow the [Deployment Guide](Documentation/DEPLOYMENT_GUIDE.md) for detailed instructions on deploying to Azure using:
- Azure App Service (Linux containers)
- Azure SQL Database
- Azure Cache for Redis
- Azure Blob Storage
- Azure Key Vault
- Azure Application Gateway with WAF
- GitHub Actions CI/CD

## Testing

```bash
# Backend tests
cd Backend
dotnet test

# Frontend tests
cd Frontend/container-delivery-frontend
npm test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
npm run test:coverage
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support, email support@container-delivery.com or create an issue on GitHub.