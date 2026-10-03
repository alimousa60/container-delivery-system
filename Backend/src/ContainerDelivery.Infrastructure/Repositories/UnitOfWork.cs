using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Interfaces;

namespace ContainerDelivery.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IRepository<User> Users { get; }
    public IRepository<UserRole> UserRoles { get; }
    public IRepository<Container> Containers { get; }
    public IRepository<Vehicle> Vehicles { get; }
    public IRepository<DeliveryRecord> DeliveryRecords { get; }
    public IRepository<AuditLog> AuditLogs { get; }
    public IRepository<ContainerReport> ContainerReports { get; }
    public IRepository<ImportBatch> ImportBatches { get; }
    public IRepository<CompanySettings> CompanySettings { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        UserRoles = new Repository<UserRole>(context);
        Containers = new Repository<Container>(context);
        Vehicles = new Repository<Vehicle>(context);
        DeliveryRecords = new Repository<DeliveryRecord>(context);
        AuditLogs = new Repository<AuditLog>(context);
        ContainerReports = new Repository<ContainerReport>(context);
        ImportBatches = new Repository<ImportBatch>(context);
        CompanySettings = new Repository<CompanySettings>(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}