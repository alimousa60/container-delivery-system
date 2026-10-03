# Deployment Guide - Container Vehicle Delivery Management System

## Overview

This guide covers deploying the Container Vehicle Delivery Management System to Microsoft Azure using Azure App Service, Azure SQL Database, Azure Blob Storage, and Azure Cache for Redis.

## Architecture Components

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         AZURE RESOURCES                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │ App Service  │  │ Azure SQL    │  │ Blob Storage │  │ Redis Cache    │
│  │ (Linux)      │  │ Database     │  │ (Reports/    │  │ (Sessions/     │
│  │ - API        │  │              │  │  Imports)    │  │  Caching)      │
│  │ - Frontend   │  │              │  │              │  │                │
│  └──────────────┘  └──────────────┘  └──────────────┘  └────────────────┘  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │ App Gateway  │  │ Front Door   │  │ App Insights │  │ Key Vault      │
│  │ (WAF/SSL)    │  │ (CDN)        │  │ (Monitoring) │  │ (Secrets)      │
│  └──────────────┘  └──────────────┘  └──────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Prerequisites

- Azure Subscription
- Azure CLI installed (`az`)
- Docker installed (for containerized deployment)
- GitHub/Azure DevOps repository
- Domain name (optional, for custom domain)

## Step 1: Create Resource Group

```bash
# Set variables
RESOURCE_GROUP="rg-container-delivery"
LOCATION="eastus"
ENVIRONMENT="prod"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION
```

## Step 2: Create Azure Key Vault

```bash
KEY_VAULT_NAME="kv-container-delivery-$ENVIRONMENT"

az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true \
  --sku standard

# Store secrets
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "JwtSecretKey" --value "your-256-bit-secret-key-here"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "SqlConnectionString" --value "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=ContainerDelivery;Encrypt=True;"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "RedisConnectionString" --value "your-redis.redis.cache.windows.net:6380,password=...,ssl=True,abortConnect=False"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "BlobStorageConnectionString" --value "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "EmailSmtpPassword" --value "your-smtp-password"
```

## Step 3: Create Azure SQL Database

```bash
SQL_SERVER="sql-container-delivery-$ENVIRONMENT"
SQL_DB="ContainerDelivery"
SQL_ADMIN="sqladmin"
SQL_PASSWORD="your-strong-password"

# Create SQL Server
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $SQL_ADMIN \
  --admin-password $SQL_PASSWORD \
  --minimal-tls-version 1.2

# Configure firewall for Azure services
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Create database
az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DB \
  --service-objective GeneralPurpose \
  --compute-model Serverless \
  --auto-pause-delay 60 \
  --min-capacity 0.5 \
  --max-capacity 2 \
  --zone-redundant false

# Store connection string in Key Vault
CONNECTION_STRING="Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DB;Persist Security Info=False;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "SqlConnectionString" --value "$CONNECTION_STRING"
```

## Step 4: Create Azure Cache for Redis

```bash
REDIS_NAME="redis-container-delivery-$ENVIRONMENT"

az redis create \
  --name $REDIS_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Basic \
  --vm-size C0 \
  --enable-non-ssl-port false \
  --minimum-tls-version 1.2

# Get connection string
REDIS_KEY=$(az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query primaryKey -o tsv)
REDIS_CONNECTION="$REDIS_NAME.redis.cache.windows.net:6380,password=$REDIS_KEY,ssl=True,abortConnect=False"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "RedisConnectionString" --value "$REDIS_CONNECTION"
```

## Step 5: Create Azure Blob Storage

```bash
STORAGE_ACCOUNT="stcontainerdelivery$ENVIRONMENT"

az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2 \
  --access-tier Hot \
  --https-only true \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false

# Create containers
az storage container create --name uploads --account-name $STORAGE_ACCOUNT --auth-mode login
az storage container create --name reports --account-name $STORAGE_ACCOUNT --auth-mode login
az storage container create --name imports --account-name $STORAGE_ACCOUNT --auth-mode login

# Get connection string
STORAGE_CONNECTION=$(az storage account show-connection-string --name $STORAGE_ACCOUNT --resource-group $RESOURCE_GROUP --query connectionString -o tsv)
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "BlobStorageConnectionString" --value "$STORAGE_CONNECTION"
```

## Step 6: Create App Service Plan and Web Apps

### Backend API App Service

```bash
APP_SERVICE_PLAN="asp-container-delivery-$ENVIRONMENT"
API_APP_NAME="api-container-delivery-$ENVIRONMENT"

# Create App Service Plan (Linux)
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --is-linux \
  --sku P1v3 \
  --number-of-workers 2

# Create API Web App
az webapp create \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:8.0" \
  --deployment-local-git

# Configure API App Settings
az webapp config appsettings set \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    JwtSettings__SecretKey=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/JwtSecretKey/) \
    JwtSettings__Issuer=https://$API_APP_NAME.azurewebsites.net \
    JwtSettings__Audience=https://$API_APP_NAME.azurewebsites.net \
    JwtSettings__AccessTokenExpiryMinutes=15 \
    JwtSettings__RefreshTokenExpiryDays=7 \
    ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/SqlConnectionString/) \
    Redis__ConnectionString=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/RedisConnectionString/) \
    BlobStorage__ConnectionString=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/BlobStorageConnectionString/) \
    EmailSettings__SmtpHost=smtp.yourprovider.com \
    EmailSettings__SmtpPort=587 \
    EmailSettings__SmtpUser=noreply@yourdomain.com \
    EmailSettings__SmtpPassword=@Microsoft.KeyVault(SecretUri=https://$KEY_VAULT_NAME.vault.azure.net/secrets/EmailSmtpPassword/) \
    EmailSettings__FromEmail=noreply@yourdomain.com \
    EmailSettings__FromName="Container Delivery System" \
    AllowedOrigins__0=https://your-frontend-domain.com \
    AllowedOrigins__1=https://$FRONTEND_APP_NAME.azurewebsites.net
```

### Frontend Web App

```bash
FRONTEND_APP_NAME="app-container-delivery-$ENVIRONMENT"

# Create Frontend Web App (Static Web App alternative recommended)
az staticwebapp create \
  --name $FRONTEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --source https://github.com/yourusername/container-delivery-system \
  --branch main \
  --app-location "/Frontend/container-delivery-frontend" \
  --output-location "dist" \
  --login-with-azure false
```

## Step 7: Configure Application Gateway (WAF)

```bash
APP_GATEWAY="agw-container-delivery-$ENVIRONMENT"
VNET_NAME="vnet-container-delivery"
SUBNET_NAME="snet-appgateway"

# Create VNet
az network vnet create \
  --name $VNET_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --address-prefix 10.0.0.0/16 \
  --subnet-name $SUBNET_NAME \
  --subnet-prefix 10.0.0.0/24

# Create Public IP
az network public-ip create \
  --name pip-appgateway \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --allocation-method Static \
  --sku Standard

# Create Application Gateway
az network application-gateway create \
  --name $APP_GATEWAY \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --vnet-name $VNET_NAME \
  --subnet $SUBNET_NAME \
  --public-ip-address pip-appgateway \
  --capacity 2 \
  --sku WAF_v2 \
  --tier WAF_v2 \
  --http-settings-cookie-based-affinity Disabled \
  --http-settings-protocol Https \
  --http-settings-port 443 \
  --frontend-port 443 \
  --ssl-cert <certificate-path> \
  --ssl-cert-password <password> \
  --waf-policy Enabled \
  --waf-mode Prevention
```

## Step 8: Configure CI/CD Pipeline

### GitHub Actions Workflow

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  AZURE_RESOURCE_GROUP: rg-container-delivery
  API_APP_NAME: api-container-delivery-prod
  FRONTEND_APP_NAME: app-container-delivery-prod
  DOTNET_VERSION: '8.0.x'
  NODE_VERSION: '20.x'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
          
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          
      - name: Restore Backend
        run: dotnet restore ./Backend/ContainerDelivery.sln
        
      - name: Build Backend
        run: dotnet build ./Backend/ContainerDelivery.sln --no-restore --configuration Release
        
      - name: Test Backend
        run: dotnet test ./Backend/ContainerDelivery.sln --no-build --configuration Release --collect:"XPlat Code Coverage"
        
      - name: Install Frontend Dependencies
        run: cd ./Frontend/container-delivery-frontend && npm ci
        
      - name: Build Frontend
        run: cd ./Frontend/container-delivery-frontend && npm run build
        
      - name: Lint Frontend
        run: cd ./Frontend/container-delivery-frontend && npm run lint

  deploy-api:
    needs: build-and-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Login to Azure
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
          
      - name: Deploy API to Azure Web App
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ env.API_APP_NAME }}
          package: ./Backend/src/ContainerDelivery.Api
          runtime-stack: 'DOTNETCORE:8.0'
          
      - name: Run Database Migrations
        run: |
          # Run EF Core migrations
          dotnet ef database update --project ./Backend/src/ContainerDelivery.Infrastructure --startup-project ./Backend/src/ContainerDelivery.Api --connection "${{ secrets.SQL_CONNECTION_STRING }}"

  deploy-frontend:
    needs: build-and-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Login to Azure
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
          
      - name: Deploy Frontend to Static Web App
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "/Frontend/container-delivery-frontend"
          output_location: "dist"
```

## Step 9: Configure Custom Domain and SSL

```bash
# Add custom domain to API App
az webapp config hostname add \
  --webapp-name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --hostname api.yourdomain.com

# Add custom domain to Frontend
az staticwebapp hostname set \
  --name $FRONTEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --hostname app.yourdomain.com

# Configure SSL binding (requires App Service Plan P1v2+)
az webapp config ssl bind \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --certificate-thumbprint <thumbprint> \
  --ssl-type SNI
```

## Step 9: Configure Monitoring and Alerts

```bash
# Create Application Insights
az monitor app-insights component create \
  --app container-delivery-insights \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --kind web

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show --app container-delivery-insights --resource-group $RESOURCE_GROUP --query instrumentationKey -o tsv)

# Add to App Settings
az webapp config appsettings set \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPINSIGHTS_INSTRUMENTATIONKEY=$INSTRUMENTATION_KEY

# Create Log Analytics Workspace
az monitor log-analytics workspace create \
  --resource-group $RESOURCE_GROUP \
  --workspace-name law-container-delivery \
  --location $LOCATION

# Configure diagnostic settings
az monitor diagnostic-settings create \
  --name "SendToLogAnalytics" \
  --resource /subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$API_APP_NAME \
  --workspace /subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.OperationalInsights/workspaces/law-container-delivery \
  --logs '[{"category":"AppServiceHTTPLogs","enabled":true},{"category":"AppServiceConsoleLogs","enabled":true},{"category":"AppServiceAuditLogs","enabled":true}]' \
  --metrics '[{"category":"AllMetrics","enabled":true}]'
```

## Step 10: Configure Backup and Disaster Recovery

```bash
# Configure SQL Database backup
az sql db backup-policy show \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DB

# Configure long-term retention (LTR)
az sql db ltr-policy create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DB \
  --weekly-retention P12W \
  --monthly-retention P36M \
  --yearly-retention P10Y \
  --week-of-year 1

# Configure Storage account lifecycle management
az storage account management-policy create \
  --account-name $STORAGE_ACCOUNT \
  --policy @lifecycle-policy.json
```

Lifecycle policy (`lifecycle-policy.json`):
```json
{
  "rules": [
    {
      "name": "delete-old-reports",
      "enabled": true,
      "type": "Lifecycle",
      "definition": {
        "actions": {
          "baseBlob": {
            "delete": { "daysAfterModificationGreaterThan": 2555 }
          }
        },
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["reports/"]
        }
      }
    }
  ]
}
```

## Step 11: Security Hardening

### Configure Managed Identity

```bash
# Enable managed identity for API App
az webapp identity assign --name $API_APP_NAME --resource-group $RESOURCE_GROUP

# Grant Key Vault access
PRINCIPAL_ID=$(az webapp identity show --name $API_APP_NAME --resource-group $RESOURCE_GROUP --query principalId -o tsv)
az keyvault set-policy --name $KEY_VAULT_NAME --object-id $PRINCIPAL_ID --secret-permissions get list
```

### Configure Private Endpoints

```bash
# Private endpoint for SQL Database
az network private-endpoint create \
  --name pe-sql \
  --resource-group $RESOURCE_GROUP \
  --vnet-name $VNET_NAME \
  --subnet snet-private \
  --private-connection-resource-id /subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Sql/servers/$SQL_SERVER \
  --group-ids sqlServer \
  --connection-name pec-sql

# Private endpoint for Redis
az network private-endpoint create \
  --name pe-redis \
  --resource-group $RESOURCE_GROUP \
  --vnet-name $VNET_NAME \
  --subnet snet-private \
  --private-connection-resource-id /subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Cache/Redis/$REDIS_NAME \
  --group-ids redisCache \
  --connection-name pec-redis

# Private endpoint for Storage
az network private-endpoint create \
  --name pe-storage \
  --resource-group $RESOURCE_GROUP \
  --vnet-name $VNET_NAME \
  --subnet snet-private \
  --private-connection-resource-id /subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT \
  --group-ids blob \
  --connection-name pec-storage
```

## Step 12: Post-Deployment Verification

```bash
# Health check
curl https://api.yourdomain.com/health

# Test authentication
curl -X POST https://api.yourdomain.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@yourdomain.com","password":"your-password"}'

# Verify database connectivity
# Check Application Insights for telemetry
# Verify SSL certificate
# Test all critical user flows
```

## Environment-Specific Configurations

### Development
- Use `Basic` tier App Service Plan
- Use `Basic` tier SQL Database
- Disable WAF in Application Gateway
- Use local development certificates

### Staging
- Use `Standard` tier App Service Plan
- Use `GeneralPurpose` tier SQL Database
- Enable WAF in Detection mode
- Use staging certificates

### Production
- Use `Premium` tier App Service Plan (P1v3+)
- Use `BusinessCritical` tier SQL Database with zone redundancy
- Enable WAF in Prevention mode
- Use production certificates with auto-renewal
- Enable geo-replication for SQL Database
- Configure Traffic Manager for multi-region

## Cost Optimization

1. **App Service**: Use auto-scaling rules
2. **SQL Database**: Use serverless compute for variable workloads
3. **Redis**: Use Basic tier for dev/staging, Standard for production
4. **Storage**: Use lifecycle policies to move old data to cool/archive tier
5. **Monitoring**: Set appropriate log retention periods

## Rollback Procedure

```bash
# Quick rollback using deployment slots
az webapp deployment slot swap \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --slot staging \
  --target-slot production

# Database rollback (if using LTR)
az sql db restore \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name ContainerDelivery_Restored \
  --source-database ContainerDelivery \
  --time "2024-01-15T10:00:00Z"
```

## Maintenance Windows

- **SQL Database**: Configure maintenance window during low-traffic hours
- **App Service**: Use deployment slots for zero-downtime deployments
- **Redis**: Schedule updates during maintenance window
- **Storage**: No maintenance required

## Compliance Checklist

- [ ] HTTPS enforced everywhere
- [ ] WAF enabled in Prevention mode
- [ ] Private endpoints for all PaaS services
- [ ] Managed identities used (no connection strings in code)
- [ ] Key Vault for all secrets
- [ ] Audit logging enabled
- [ ] Data encryption at rest and in transit
- [ ] Regular penetration testing scheduled
- [ ] Backup and DR tested quarterly
- [ ] GDPR/privacy compliance verified

## Support and Troubleshooting

### Common Issues

1. **Database connection failures**: Check firewall rules, connection strings, managed identity permissions
2. **Redis connection issues**: Verify TLS 1.2, connection string format, firewall
3. **Blob storage upload failures**: Check SAS tokens, container permissions, network rules
4. **Authentication errors**: Verify JWT settings, clock skew, token expiration
5. **CORS errors**: Check AllowedOrigins configuration

### Useful Commands

```bash
# View app logs
az webapp log tail --name $API_APP_NAME --resource-group $RESOURCE_GROUP

# Check deployment status
az webapp deployment list --name $API_APP_NAME --resource-group $RESOURCE_GROUP

# View metrics
az monitor metrics list --resource /subscriptions/<sub-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$API_APP_NAME --metric "CpuPercentage,MemoryPercentage,Http5xx"
```