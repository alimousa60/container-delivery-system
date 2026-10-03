# Container Vehicle Delivery Management System - Architecture

## System Overview

A secure, scalable web-based application for tracking vehicles inside shipping containers during delivery operations. The system prevents vehicles from being forgotten during delivery through comprehensive tracking, audit logging, and automated workflow management.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            CLIENT LAYER                                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │   Web Browser   │  │  Mobile Browser │  │  Barcode Scanner (Camera)   │  │
│  │  (React SPA)    │  │  (Responsive)   │  │  (HTML5 MediaDevices API)   │  │
│  └────────┬────────┘  └────────┬────────┘  └──────────────┬──────────────┘  │
└───────────┼────────────────────┼──────────────────────────┼──────────────────┘
            │                    │                          │
            ▼                    ▼                          ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         API GATEWAY / LOAD BALANCER                          │
│                        (Azure Application Gateway)                           │
│                          HTTPS Only / WAF Enabled                            │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        APPLICATION LAYER (ASP.NET Core 8)                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │ Auth Service │  │Container Svc │  │ Vehicle Svc  │  │  Report Svc    │  │
│  │  (JWT/MFA)   │  │  (CRUD)      │  │  (Delivery)  │  │  (PDF/Excel)   │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └────────────────┘  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │ Audit Logger │  │ Import Svc   │  │ Search Svc   │  │  Admin Svc     │  │
│  │  (Serilog)   │  │  (Excel)     │  │  (Full-text) │  │  (Users/Roles) │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └────────────────┘  │
└────────────────────────────────┬────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DATA LAYER                                         │
│  ┌─────────────────────────┐  ┌─────────────────────────┐                  │
│  │   SQL Server (Azure)    │  │   Azure Blob Storage    │                  │
│  │   - Containers          │  │   - Excel imports       │                  │
│  │   - Vehicles            │  │   - PDF reports         │                  │
│  │   - Users/Roles         │  │   - Audit exports       │                  │
│  │   - Audit Logs          │  │                         │                  │
│  │   - Delivery Records    │  │                         │                  │
│  └─────────────────────────┘  └─────────────────────────┘                  │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8 Web API
- **Language**: C# 12
- **ORM**: Entity Framework Core 8
- **Database**: SQL Server (Azure SQL Database)
- **Authentication**: JWT Bearer Tokens + MFA (TOTP)
- **Authorization**: Role-Based Access Control (RBAC)
- **Logging**: Serilog with structured logging
- **PDF Generation**: QuestPDF
- **Excel Processing**: ClosedXML
- **API Documentation**: Swagger/OpenAPI
- **Caching**: Redis (Azure Cache for Redis)
- **Background Jobs**: Hangfire

### Frontend
- **Framework**: React 18 with TypeScript
- **Build Tool**: Vite
- **Styling**: Tailwind CSS
- **State Management**: Zustand + React Query (TanStack Query)
- **Routing**: React Router v6
- **Forms**: React Hook Form + Zod validation
- **Internationalization**: i18next (Arabic/English)
- **Charts**: Recharts
- **PDF Viewer**: @react-pdf/renderer
- **Barcode Scanning**: @zxing/library (WebAssembly)

### Infrastructure (Azure)
- **Compute**: Azure App Service (Linux containers)
- **Database**: Azure SQL Database (General Purpose)
- **Storage**: Azure Blob Storage
- **Cache**: Azure Cache for Redis
- **Identity**: Microsoft Entra ID (preferred) / JWT fallback
- **CDN**: Azure Front Door
- **Monitoring**: Application Insights + Log Analytics
- **CI/CD**: GitHub Actions / Azure DevOps

## Security Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     SECURITY LAYERS                              │
├─────────────────────────────────────────────────────────────────┤
│  NETWORK                                                         │
│  ├─ Azure Front Door (WAF, DDoS protection)                     │
│  ├─ Application Gateway (SSL termination, WAF)                  │
│  └─ VNet Integration (Private endpoints for SQL/Redis)          │
├─────────────────────────────────────────────────────────────────┤
│  APPLICATION                                                     │
│  ├─ HTTPS Only (HSTS, Secure Cookies)                           │
│  ├─ JWT with short expiry (15min) + Refresh Tokens              │
│  ├─ MFA via TOTP (Google Authenticator compatible)              │
│  ├─ RBAC: Admin | DeliveryUser                                   │
│  ├─ Input Validation (FluentValidation)                         │
│  ├─ Rate Limiting (AspNetCoreRateLimit)                         │
│  └─ CORS Policy (Strict origins)                                │
├─────────────────────────────────────────────────────────────────┤
│  DATA                                                            │
│  ├─ Transparent Data Encryption (TDE)                           │
│  ├─ Column-level encryption for sensitive data                  │
│  ├─ Parameterized queries (EF Core prevents SQL injection)      │
│  ├─ Audit logging for all CRUD operations                       │
│  └─ Regular automated backups with geo-replication              │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow - Vehicle Delivery Process

```
1. IMPORT PHASE
   Excel File → Validation → Containers Created → Vehicles Assigned → Audit Log

2. DELIVERY PHASE
   User Login (MFA) → Select Container → Scan VIN/Manual Entry 
   → Validate VIN exists in container → Mark Delivered → Update Container Status
   → Auto-complete if all delivered → Audit Log

3. REPORTING PHASE
   Select Container → Generate PDF → Store in Blob → Download/Print → Archive

4. LIFECYCLE MANAGEMENT
   Admin Review → Delete Completed Containers → Archive Reports → Compliance Retention
```

## Scalability Considerations

- **Horizontal Scaling**: Stateless API services behind load balancer
- **Database**: Read replicas for reporting queries, connection pooling
- **Caching**: Redis for dashboard statistics, container lists, user sessions
- **Background Processing**: Hangfire for Excel imports, PDF generation, audit exports
- **Search**: Full-text search indexes on VIN, Container Number, Description
- **Pagination**: Cursor-based pagination for large datasets