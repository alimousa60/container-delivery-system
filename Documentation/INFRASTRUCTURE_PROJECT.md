# ContainerDelivery.Infrastructure Project Structure

```
ContainerDelivery.Infrastructure/
├── ContainerDelivery.Infrastructure.csproj
├── Data/
│   ├── AppDbContext.cs
│   └── Configurations/
│       └── EntityConfigurations.cs
├── Repositories/
│   ├── Repository.cs
│   └── UnitOfWork.cs
├── Services/
│   ├── AuthService.cs
│   ├── ContainerService.cs
│   ├── VehicleService.cs
│   ├── ImportService.cs
│   ├── ReportService.cs
│   └── AuditService.cs
├── Auth/
│   ├── JwtTokenService.cs
│   ├── MfaService.cs
│   ├── PasswordService.cs
│   ├── JwtSettings.cs
│   ├── IJwtTokenService.cs
│   ├── IMfaService.cs
│   └── IPasswordService.cs
├── Email/
│   ├── EmailService.cs
│   └── EmailSettings.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

## Key Files Content

### 1. ContainerDelivery.Infrastructure.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ContainerDelivery.Core\ContainerDelivery.Core.csproj" />
    <ProjectReference Include="..\ContainerDelivery.Application\ContainerDelivery.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="ClosedXML" Version="0.102.1" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.4.0" />
    <PackageReference Include="Otp.NET" Version="1.3.2" />
    <PackageReference Include="QuestPDF" Version="2023.12.6" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.4.0" />
  </ItemGroup>
</Project>
```

### 2. Data/AppDbContext.cs
```csharp
using Microsoft.EntityFrameworkCore;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<DeliveryRecord> DeliveryRecords => Set<DeliveryRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ContainerReport> ContainerReports => Set<ContainerReport>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", Description = "Full system access" },
            new Role { Id = 2, Name = "DeliveryUser", Description = "Can view assigned containers, confirm deliveries, and generate reports" }
        );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified)
                entry.Entity.SetUpdatedAt();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### 3. Data/Configurations/EntityConfigurations.cs
Contains all EF Core entity configurations for:
- User, Role, UserRole
- Container, Vehicle, DeliveryRecord
- AuditLog, ContainerReport, ImportBatch

### 4. Repositories/Repository.cs
```csharp
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context) { _context = context; _dbSet = context.Set<T>(); }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default) 
        => await _dbSet.FindAsync([id], ct);

    public virtual async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        var query = ApplySpecification(spec);
        return await query.FirstOrDefaultAsync(ct);
    }

    public virtual async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        var query = ApplySpecification(spec);
        return await query.ToListAsync(ct);
    }

    public virtual async Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        var query = ApplySpecification(spec);
        return await query.CountAsync(ct);
    }

    // AddAsync, UpdateAsync, DeleteAsync, ExistsAsync...
    protected virtual IQueryable<T> ApplySpecification(ISpecification<T> spec) { /* ... */ }
}
```

### 5. Repositories/UnitOfWork.cs
```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IRepository<User> Users { get; }
    public IRepository<Role> Roles { get; }
    public IRepository<UserRole> UserRoles { get; }
    public IRepository<Container> Containers { get; }
    public IRepository<Vehicle> Vehicles { get; }
    public IRepository<DeliveryRecord> DeliveryRecords { get; }
    public IRepository<AuditLog> AuditLogs { get; }
    public IRepository<ContainerReport> ContainerReports { get; }
    public IRepository<ImportBatch> ImportBatches { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        Roles = new Repository<Role>(context);
        // ... all repositories
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) 
        => await _context.SaveChangesAsync(ct);

    public void Dispose() { _context.Dispose(); GC.SuppressFinalize(this); }
}
```

### 6. Services/AuthService.cs
Complete authentication service with:
- Login with JWT + MFA support
- Token refresh
- MFA setup/verify/disable
- Password change/reset
- Audit logging for all auth events

### 7. Services/ContainerService.cs
- CRUD operations for containers
- Dashboard statistics
- Vehicle management per container
- Import from Excel (ClosedXML)
- Status recalculation (auto-complete when all delivered)

### 8. Services/VehicleService.cs
- Vehicle search (VIN, container, description)
- Delivery confirmation (barcode scan + manual)
- Batch delivery
- Undelivery (admin only)

### 9. Services/ImportService.cs
```csharp
public async Task<ImportBatch> ImportAsync(Stream excelStream, string fileName, int userId, string? prefix)
{
    using var workbook = new XLWorkbook(excelStream);
    var worksheet = workbook.Worksheet(1);
    var rows = worksheet.RowsUsed().Skip(1).ToList(); // Skip header
    
    foreach (var row in rows)
    {
        var containerNumber = row.Cell(1).GetString().Trim();
        var vin = row.Cell(2).GetString().Trim().ToUpper();
        var description = row.Cell(3).GetString().Trim();
        
        // Validate VIN (17 chars), check duplicates
        // Get or create container
        // Create vehicle and assign to container
    }
}
```

### 10. Services/ReportService.cs
```csharp
private byte[] GeneratePdf(Container container, IReadOnlyList<Vehicle> vehicles, User user)
{
    var document = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(50);
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    });
    return document.GeneratePdf();
}
```
Generates professional PDF with:
- Container info header
- Summary stats cards
- Vehicle table with delivery status
- Footer with page numbers

### 11. Services/AuditService.cs
- Audit log querying with filters
- Excel export functionality

### 12. Auth/JwtTokenService.cs
- JWT access token generation (15 min expiry)
- Refresh token generation (7/30 days)
- Token validation and user ID extraction

### 13. Auth/MfaService.cs
- TOTP secret generation (Base32)
- QR code URL for authenticator apps
- Code verification with time tolerance
- Recovery code generation/validation

### 14. Auth/PasswordService.cs
- BCrypt hashing (cost factor 12)
- Password verification
- Rehash detection

### 15. Email/EmailService.cs
- SMTP email sending
- Password reset emails
- MFA setup emails

### 16. Extensions/ServiceCollectionExtensions.cs
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
        services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        // ... all services
        
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IPasswordService, PasswordService>();
        
        return services;
    }
}
```