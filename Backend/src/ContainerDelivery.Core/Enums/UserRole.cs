namespace ContainerDelivery.Core.Enums;

public enum UserRole
{
    Admin = 0,
    DeliveryUser = 1
}

public static class UserRoleExtensions
{
    public static string ToDisplayString(this UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "Administrator",
            UserRole.DeliveryUser => "Delivery User",
            _ => role.ToString()
        };
    }
}

public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Login = 3,
    Logout = 4,
    Import = 5,
    Export = 6,
    Delivery = 7,
    ReportGeneration = 8
}

public enum EntityType
{
    User = 0,
    Container = 1,
    Vehicle = 2,
    DeliveryRecord = 3,
    Report = 4,
    ImportBatch = 5
}

public enum ImportStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}