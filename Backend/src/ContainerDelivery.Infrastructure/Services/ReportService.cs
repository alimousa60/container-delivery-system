using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Exceptions;
using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Core.Specifications;
using System.Text.Json;

namespace ContainerDelivery.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<ReportService> _logger;
    private readonly ISettingsService _settingsService;

    public ReportService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<ReportService> logger, ISettingsService settingsService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<ContainerReport> GenerateAsync(int containerId, int generatedByUserId)
    {
        var container = await _unitOfWork.Containers.GetByIdAsync(containerId);
        if (container == null)
            throw new EntityNotFoundException("Container", containerId);

        var vehiclesSpec = new VehiclesByContainerSpec(containerId);
        var vehicles = await _unitOfWork.Vehicles.ListAsync(vehiclesSpec);

        var generatedByUser = await _unitOfWork.Users.GetByIdAsync(generatedByUserId);
        if (generatedByUser == null)
            throw new EntityNotFoundException("User", generatedByUserId);

        // Fetch company settings for report
        var settings = await _settingsService.GetAsync();

        var fileName = $"Container_{SanitizeFileName(container.ContainerNumber)}_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        var filePath = $"reports/{fileName}";

        // Generate PDF with company settings
        var pdfBytes = GeneratePdf(container, vehicles, generatedByUser, settings);

        var report = new ContainerReport
        {
            ContainerId = containerId,
            GeneratedByUserId = generatedByUserId,
            FilePath = filePath,
            FileName = fileName,
            TotalVehicles = container.TotalVehicles,
            DeliveredVehicles = container.DeliveredVehicles,
            UndeliveredVehicles = container.UndeliveredVehicles,
            CompletionPercentage = (decimal)container.CompletionPercentage,
            FileSizeBytes = pdfBytes.Length
        };

        await _unitOfWork.ContainerReports.AddAsync(report);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = generatedByUserId,
            Action = AuditAction.ReportGeneration,
            EntityType = EntityType.Container,
            EntityId = containerId,
            OldValues = null,
            NewValues = JsonSerializer.Serialize(new { report.FileName, report.CompletionPercentage, report.FileSizeBytes }),
            IpAddress = null,
            UserAgent = null
        });

        return report;
    }

    public async Task<ContainerReport> GetByIdAsync(int id)
    {
        return await _unitOfWork.ContainerReports.GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<ContainerReport>> GetByContainerIdAsync(int containerId)
    {
        var spec = new ContainerReportsByContainerSpec(containerId);
        return await _unitOfWork.ContainerReports.ListAsync(spec);
    }

    public async Task<PagedResult<ContainerReport>> GetPagedAsync(int page, int pageSize)
    {
        var items = await _unitOfWork.ContainerReports.ListAsync(new ContainerReportsPagedSpec(page, pageSize));
        var totalCount = await _unitOfWork.ContainerReports.CountAsync(new ContainerReportsCountSpec());

        return new PagedResult<ContainerReport>(items, totalCount, page, pageSize);
    }

    public async Task<bool> DeleteAsync(int reportId, int userId)
    {
        var report = await _unitOfWork.ContainerReports.GetByIdAsync(reportId);
        if (report == null)
            return false;

        await _unitOfWork.ContainerReports.DeleteAsync(report);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Delete,
            EntityType = EntityType.Report,
            EntityId = reportId,
            OldValues = JsonSerializer.Serialize(new { report.FileName, report.ContainerId }),
            NewValues = null,
            IpAddress = null,
            UserAgent = null
        });

        return true;
    }

    public async Task<Stream> DownloadAsync(int reportId)
    {
        var report = await GetByIdAsync(reportId);
        if (report == null)
            throw new EntityNotFoundException("Report", reportId);

        var container = await _unitOfWork.Containers.GetByIdAsync(report.ContainerId);
        var vehiclesSpec = new VehiclesByContainerSpec(report.ContainerId);
        var vehicles = await _unitOfWork.Vehicles.ListAsync(vehiclesSpec);
        var generatedByUser = await _unitOfWork.Users.GetByIdAsync(report.GeneratedByUserId);

        // Fetch company settings for report
        var settings = await _settingsService.GetAsync();

        var pdfBytes = GeneratePdf(container!, vehicles, generatedByUser!, settings);
        return new MemoryStream(pdfBytes);
    }

    public async Task<BulkReportResult> GenerateBulkAsync(IEnumerable<int> containerIds, int generatedByUserId)
    {
        var errors = new List<BulkReportError>();
        int successCount = 0;

        foreach (var containerId in containerIds)
        {
            try
            {
                await GenerateAsync(containerId, generatedByUserId);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report for container {ContainerId}", containerId);
                errors.Add(new BulkReportError(containerId, ex.Message));
            }
        }

        return new BulkReportResult(successCount, errors.Count, errors);
    }

    private byte[] GeneratePdf(Container container, IReadOnlyList<Vehicle> vehicles, User generatedByUser, CompanySettings? settings)
    {
        var document = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(50);
                page.Header().Element(c => ComposeHeader(c, container, settings));
                page.Content().Element(c => ComposeContent(c, container, vehicles, generatedByUser));
                page.Footer().Element(c => ComposeFooter(c, generatedByUser, settings));
            });
        });

        return document.GeneratePdf();

        void ComposeHeader(IContainer container, Container reportContainer, CompanySettings? settings)
        {
            var primaryColor = Colors.Blue.Darken2;
            var companyName = settings?.CompanyName ?? "Container Delivery System";
            var logoUrl = settings?.LogoUrl;

            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    if (!string.IsNullOrEmpty(logoUrl))
                    {
                        col.Item().Height(50).Image(logoUrl);
                        col.Item().PaddingTop(10);
                    }
                    else
                    {
                        col.Item().Row(r =>
                        {
                            r.AutoItem().Width(50).Height(50).Background(primaryColor)
                                .AlignCenter().AlignMiddle()
                                .Text("📦").FontSize(24).FontColor(Colors.White);
                            r.RelativeItem().PaddingLeft(10).Column(c =>
                            {
                                c.Item().Text(companyName).FontSize(22).Bold().FontColor(primaryColor);
                                c.Item().Text("VEHICLE DELIVERY REPORT").FontSize(11).FontColor(Colors.Grey.Medium).LetterSpacing(1);
                            });
                        });
                    }

                    col.Item().PaddingTop(10).Row(r =>
                    {
                        r.AutoItem().Text($"Container: ").SemiBold().FontSize(11).FontColor(Colors.Grey.Darken1);
                        r.AutoItem().Text(reportContainer.ContainerNumber).FontSize(18).Bold().FontColor(primaryColor);
                        r.ConstantItem(20);
                        r.AutoItem().PaddingTop(2).Element(c => ComposeStatusBadge(c, reportContainer.Status));
                    });
                });

                row.ConstantItem(200).Column(col =>
                {
                    col.Item().AlignRight().Row(r =>
                    {
                        r.AutoItem().Width(24).Height(24).Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("📅").FontSize(12);
                        r.AutoItem().PaddingLeft(5).Column(c =>
                        {
                            c.Item().Text("Report Date").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().Text(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC").FontSize(10).SemiBold();
                        });
                    });
                    col.Item().PaddingTop(5).AlignRight().Row(r =>
                    {
                        r.AutoItem().Width(24).Height(24).Background(Colors.Grey.Lighten2).AlignCenter().AlignMiddle()
                            .Text("👤").FontSize(12);
                        r.AutoItem().PaddingLeft(5).Column(c =>
                        {
                            c.Item().Text("Generated By").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().Text(generatedByUser.FullName).FontSize(10).SemiBold();
                        });
                    });
                });
            });

            container.PaddingTop(15).BorderBottom(2).BorderColor(primaryColor);
        }

        void ComposeStatusBadge(IContainer container, ContainerStatus status)
        {
            var (bgColor, textColor, text, icon) = status switch
            {
                ContainerStatus.NotStarted => (Colors.Red.Lighten2, Colors.Red.Darken2, "NOT STARTED", "🔴"),
                ContainerStatus.InProgress => (Colors.Orange.Lighten2, Colors.Orange.Darken2, "IN PROGRESS", "🟡"),
                ContainerStatus.FullyDelivered => (Colors.Green.Lighten2, Colors.Green.Darken2, "FULLY DELIVERED", "🟢"),
                _ => (Colors.Grey.Lighten2, Colors.Grey.Darken2, "UNKNOWN", "⚪")
            };

            container.Padding(5).Background(bgColor).AlignCenter().AlignMiddle()
                .DefaultTextStyle(x => x.FontColor(textColor).FontSize(9).SemiBold())
                .Text($"{icon} {text}");
        }

        void ComposeContent(IContainer container, Container reportContainer, IReadOnlyList<Vehicle> vehicles, User generatedByUser)
        {
            container.PaddingVertical(20).Column(col =>
            {
                col.Item().Element(c => ComposeSummaryCards(c));
                col.Item().PaddingTop(20).Element(c => ComposeProgressBar(c, reportContainer));
                col.Item().PaddingTop(20).Element(c => ComposeContainerDetails(c, reportContainer));
                col.Item().PaddingTop(20).Element(c => ComposeVehiclesTable(c, vehicles));
                
                if (!string.IsNullOrWhiteSpace(reportContainer.Notes))
                {
                    col.Item().PaddingTop(20).Element(c => ComposeNotes(c, reportContainer));
                }
                
                col.Item().PaddingTop(30).Element(c => ComposeSignatureSection(c));
            });

            void ComposeSummaryCards(IContainer container)
            {
                var stats = new[]
                {
                    ("Total Vehicles", reportContainer.TotalVehicles.ToString(), Colors.Blue.Lighten2, Colors.Blue.Darken2, "🚗"),
                    ("Delivered", reportContainer.DeliveredVehicles.ToString(), Colors.Green.Lighten2, Colors.Green.Darken2, "✅"),
                    ("Pending", reportContainer.UndeliveredVehicles.ToString(), Colors.Orange.Lighten2, Colors.Orange.Darken2, "⏳"),
                    ("Completion", $"{reportContainer.CompletionPercentage:F1}%", Colors.Purple.Lighten2, Colors.Purple.Darken2, "📊")
                };

                container.Row(row =>
                {
                    for (int i = 0; i < stats.Length; i++)
                    {
                        var (label, value, bgColor, textColor, icon) = stats[i];
                        row.RelativeItem().Element(c => ComposeStatCard(c, label, value, bgColor, textColor, icon));
                        if (i < stats.Length - 1) row.ConstantItem(15);
                    }
                });
            }

            void ComposeStatCard(IContainer container, string label, string value, string bgColor, string textColor, string icon)
            {
                container.Background(bgColor).Padding(20).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Text(icon).FontSize(18);
                        r.RelativeItem().AlignRight().Text(label).FontSize(10).FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().PaddingTop(5).Row(r =>
                    {
                        r.RelativeItem().AlignCenter().Text(value).FontSize(28).Bold().FontColor(textColor);
                    });
                });
            }

            void ComposeProgressBar(IContainer container, Container reportContainer)
            {
                container.Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Delivery Progress").SemiBold().FontSize(12).FontColor(Colors.Grey.Darken1);
                        r.AutoItem().Text($"{reportContainer.CompletionPercentage:F1}%").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                    });
                    var pct = Math.Clamp((float)reportContainer.CompletionPercentage, 0f, 100f);
                    col.Item().PaddingTop(8).Height(12).Background(Colors.Grey.Lighten2).Row(r =>
                    {
                        if (pct > 0)
                            r.RelativeItem(pct).Height(12).Background(Colors.Blue.Medium);
                        if (pct < 100)
                            r.RelativeItem(100f - pct).Height(12);
                    });
                });
            }

            void ComposeContainerDetails(IContainer container, Container reportContainer)
            {
                container.Background(Colors.Grey.Lighten4).Padding(20).Column(col =>
                {
                    col.Item().Text("Container Details").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            AddDetailRow(c, "Container Number", reportContainer.ContainerNumber);
                            AddDetailRow(c, "Created Date", reportContainer.CreatedAt.ToString("yyyy-MM-dd HH:mm UTC"));
                            if (reportContainer.StartedAt.HasValue)
                                AddDetailRow(c, "Delivery Started", reportContainer.StartedAt.Value.ToString("yyyy-MM-dd HH:mm UTC"));
                            if (reportContainer.CompletedAt.HasValue)
                                AddDetailRow(c, "Delivery Completed", reportContainer.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm UTC"));
                        });
                        row.RelativeItem().Column(c =>
                        {
                            AddDetailRow(c, "Total Vehicles", reportContainer.TotalVehicles.ToString());
                            AddDetailRow(c, "Delivered", reportContainer.DeliveredVehicles.ToString());
                            AddDetailRow(c, "Undelivered", reportContainer.UndeliveredVehicles.ToString());
                            AddDetailRow(c, "Status", reportContainer.Status.ToString());
                        });
                    });
                });

                void AddDetailRow(ColumnDescriptor c, string label, string value)
                {
                    c.Item().Row(r =>
                    {
                        r.AutoItem().Text(label).FontSize(10).FontColor(Colors.Grey.Medium);
                        r.RelativeItem().AlignRight().Text(value).FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                    });
                }
            }

            void ComposeNotes(IContainer container, Container reportContainer)
            {
                container.Background(Colors.Yellow.Lighten3).Padding(15).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Text("📝").FontSize(14);
                        r.RelativeItem().Text("Notes").FontSize(12).Bold().FontColor(Colors.Orange.Darken2);
                    });
                    col.Item().PaddingTop(5).Text(reportContainer.Notes).FontSize(10).FontColor(Colors.Grey.Darken2);
                });
            }

            void ComposeVehiclesTable(IContainer container, IReadOnlyList<Vehicle> vehicleList)
            {
                if (!vehicleList.Any())
                {
                    container.AlignCenter().Padding(40).Column(c =>
                    {
                        c.Item().Text("📭").FontSize(48);
                        c.Item().Text("No vehicles in this container").FontSize(14).FontColor(Colors.Grey.Medium);
                    });
                    return;
                }

                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(45);
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(3.5f);
                        columns.ConstantColumn(110);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        var headers = new[] { "#", "VIN", "Vehicle Description", "Status", "Delivered At", "Delivered By" };
                        foreach (var h in headers)
                        {
                            header.Cell().Element(HeaderCellStyle).Text(h).SemiBold();
                        }
                    });

                    int index = 1;
                    foreach (var vehicle in vehicleList.OrderBy(v => v.Description))
                    {
                        var isDelivered = vehicle.IsDelivered;
                        var bgColor = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                        var statusBg = isDelivered ? Colors.Green.Lighten2 : Colors.Red.Lighten2;
                        var statusTextColor = isDelivered ? Colors.Green.Darken2 : Colors.Red.Darken2;
                        var statusText = isDelivered ? "✅ Delivered" : "❌ Pending";

                        table.Cell().Element(c => DataCellStyle(c, bgColor).AlignCenter()).Text(index.ToString()).FontSize(9);
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(vehicle.Vin).FontSize(9).FontFamily("Monospace");
                        table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(vehicle.Description).FontSize(9);
                        table.Cell().Element(c => DataCellStyle(c, bgColor).Padding(5).Background(statusBg).AlignCenter()).Text(statusText).FontSize(8).FontColor(statusTextColor).SemiBold();
                        table.Cell().Element(c => DataCellStyle(c, bgColor).AlignCenter()).Text(vehicle.DeliveredAt?.ToString("yyyy-MM-dd HH:mm") ?? "—").FontSize(9).FontColor(Colors.Grey.Medium);
                        table.Cell().Element(c => DataCellStyle(c, bgColor).AlignCenter()).Text(vehicle.DeliveredByUser?.FullName ?? "—").FontSize(9).FontColor(Colors.Grey.Medium);

                        index++;
                    }

                    static IContainer HeaderCellStyle(IContainer c) => c.Padding(8).Background(Colors.Blue.Darken2).DefaultTextStyle(x => x.FontColor(Colors.White).FontSize(9));
                    static IContainer DataCellStyle(IContainer c, string bgColor) => c.Padding(8).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Background(bgColor).DefaultTextStyle(x => x.FontSize(9));
                });
            }

            void ComposeSignatureSection(IContainer container)
            {
                container.Column(col =>
                {
                    col.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(60).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(5).AlignCenter().Text("Authorized Signature").FontSize(10).FontColor(Colors.Grey.Medium);
                            c.Item().AlignCenter().Text("Date: _______________").FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(50);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Height(60).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(5).AlignCenter().Text("Receiver Signature").FontSize(10).FontColor(Colors.Grey.Medium);
                            c.Item().AlignCenter().Text("Date: _______________").FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            }
        }

        void ComposeFooter(IContainer container, User generatedByUser, CompanySettings? settings)
        {
            var footerText = settings?.ReportFooter ?? "This document is confidential and intended solely for the use of the individual or entity to whom it is addressed.";
            
            container.AlignCenter().Column(col =>
            {
                col.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10);
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                        text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                    row.ConstantItem(100);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span($"{settings?.CompanyName ?? "Container Delivery System"} | ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span("CONFIDENTIAL").FontSize(8).Bold().FontColor(Colors.Red.Medium);
                    });
                });
            });
        }
    }

    private string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');
        return fileName;
    }
}
