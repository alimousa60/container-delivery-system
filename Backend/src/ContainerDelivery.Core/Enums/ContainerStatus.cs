namespace ContainerDelivery.Core.Enums;

public enum ContainerStatus
{
    NotStarted = 0,
    InProgress = 1,
    FullyDelivered = 2,
    Closed = 3
}

public static class ContainerStatusExtensions
{
    public static string ToDisplayString(this ContainerStatus status)
    {
        return status switch
        {
            ContainerStatus.NotStarted => "Not Started",
            ContainerStatus.InProgress => "In Progress",
            ContainerStatus.FullyDelivered => "Fully Delivered",
            ContainerStatus.Closed => "Closed",
            _ => status.ToString()
        };
    }

    public static string ToCssClass(this ContainerStatus status)
    {
        return status switch
        {
            ContainerStatus.NotStarted => "status-not-started",
            ContainerStatus.InProgress => "status-in-progress",
            ContainerStatus.FullyDelivered => "status-fully-delivered",
            ContainerStatus.Closed => "status-closed",
            _ => ""
        };
    }

    public static bool IsOperational(this ContainerStatus status)
    {
        return status == ContainerStatus.NotStarted || 
               status == ContainerStatus.InProgress || 
               status == ContainerStatus.FullyDelivered;
    }

    public static bool CanClose(this ContainerStatus status)
    {
        return status == ContainerStatus.FullyDelivered;
    }

    public static bool CanReopen(this ContainerStatus status)
    {
        return status == ContainerStatus.Closed;
    }
}