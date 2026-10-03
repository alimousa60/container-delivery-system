using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Infrastructure.Reports;

namespace ContainerDelivery.Infrastructure.Reports;

/// <summary>
/// Sample program to generate a PDF report for container MCDU5018850_1
/// Run this as a console application to generate the PDF file.
/// </summary>
public class SampleReportGenerator
{
    public static void GenerateSampleReport()
    {
        // Set QuestPDF license (Community license is free for non-commercial use)
        QuestPDF.Settings.License = LicenseType.Community;
        
        // Set up the sample data matching the container MCDU5018850_1
        var container = new ContainerData
        {
            ContainerNumber = "MCDU5018850_1",
            TotalVehicles = 2,
            DeliveredVehicles = 2,
            Status = ContainerStatus.FullyDelivered,
            CompletionPercentage = 100.0,
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            StartedAt = new DateTime(2024, 1, 16, 9, 30, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2024, 1, 16, 10, 15, 0, DateTimeKind.Utc),
            Notes = "All vehicles delivered successfully. Container sealed and ready for shipment."
        };

        var vehicles = new List<VehicleData>
        {
            new VehicleData
            {
                Vin = "WDBUF56X78B358793",
                Description = "MERCEDES-BENZ E 350 2008",
                IsDelivered = true,
                DeliveredAt = new DateTime(2024, 1, 16, 9, 30, 0, DateTimeKind.Utc),
                DeliveredBy = "John Doe"
            },
            new VehicleData
            {
                Vin = "KNDMB233296299370",
                Description = "KIA SEDONA LX 2009",
                IsDelivered = true,
                DeliveredAt = new DateTime(2024, 1, 16, 10, 15, 0, DateTimeKind.Utc),
                DeliveredBy = "John Doe"
            }
        };

        var generatedBy = "John Doe";

        // Create the report
        var report = new ContainerDeliveryReport(container, vehicles, generatedBy, "Container Delivery System");

        // Generate PDF bytes
        byte[] pdfBytes = report.GeneratePdf();

        // Save to file
        string fileName = $"Container_{container.ContainerNumber}_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        string filePath = Path.Combine(Environment.CurrentDirectory, "Reports", fileName);
        
        // Ensure directory exists
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Reports"));
        
        File.WriteAllBytes(filePath, pdfBytes);
        
        Console.WriteLine($"Report generated successfully!");
        Console.WriteLine($"File: {filePath}");
        Console.WriteLine($"Size: {pdfBytes.Length:N0} bytes");
    }
    
    public static void Main(string[] args)
    {
        try
        {
            GenerateSampleReport();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating report: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}