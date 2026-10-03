using ContainerDelivery.Core.Entities;

namespace ContainerDelivery.Core.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}

public interface IReadRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<UserRole> UserRoles { get; }
    IRepository<Container> Containers { get; }
    IRepository<Vehicle> Vehicles { get; }
    IRepository<DeliveryRecord> DeliveryRecords { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<ContainerReport> ContainerReports { get; }
    IRepository<ImportBatch> ImportBatches { get; }
    IRepository<CompanySettings> CompanySettings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}