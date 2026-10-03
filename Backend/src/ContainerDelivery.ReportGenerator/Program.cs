using ContainerDelivery.Infrastructure.Reports;

namespace ContainerDelivery.ReportGenerator;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Container Delivery System - PDF Report Generator");
        Console.WriteLine("==================================================");
        Console.WriteLine();
        
        try
        {
            SampleReportGenerator.GenerateSampleReport();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}