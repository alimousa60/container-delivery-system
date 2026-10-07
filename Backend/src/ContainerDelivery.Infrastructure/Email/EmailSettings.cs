namespace ContainerDelivery.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 25;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromEmail { get; set; } = "noreply@localhost";
    public string FromName { get; set; } = "Container Delivery System";
}
