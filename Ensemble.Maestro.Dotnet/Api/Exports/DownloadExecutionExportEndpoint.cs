using FastEndpoints;
using Ensemble.Maestro.Dotnet.Core.Services;

namespace Ensemble.Maestro.Dotnet.Api.Exports;

public class DownloadExecutionExportEndpoint : Endpoint<DownloadExecutionExportRequest>
{
    private readonly ExportService _exportService;

    public DownloadExecutionExportEndpoint(ExportService exportService)
    {
        _exportService = exportService;
    }

    public override void Configure()
    {
        Get("/api/exports/execution/{executionId}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Download pipeline execution export";
            s.Description = "Downloads a pipeline execution export in the specified format";
        });
    }

    public override async Task HandleAsync(DownloadExecutionExportRequest req, CancellationToken ct)
    {
        var format = Enum.TryParse<ExportFormat>(req.Format, true, out var parsedFormat) 
            ? parsedFormat 
            : ExportFormat.Json;

        var result = await _exportService.ExportPipelineExecution(req.ExecutionId, format);

        if (!result.Success)
        {
            ThrowError(result.ErrorMessage);
            return;
        }

        HttpContext.Response.ContentType = result.ContentType;
        HttpContext.Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{result.Filename}\"");
        await HttpContext.Response.Body.WriteAsync(result.Data, ct);
    }
}

public class DownloadExecutionExportRequest
{
    public Guid ExecutionId { get; set; }
    public string Format { get; set; } = "json";
}

public class DownloadSummaryExportEndpoint : Endpoint<DownloadSummaryExportRequest>
{
    private readonly ExportService _exportService;

    public DownloadSummaryExportEndpoint(ExportService exportService)
    {
        _exportService = exportService;
    }

    public override void Configure()
    {
        Get("/api/exports/summary");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Download testbench summary export";
            s.Description = "Downloads a testbench summary export in the specified format";
        });
    }

    public override async Task HandleAsync(DownloadSummaryExportRequest req, CancellationToken ct)
    {
        var format = Enum.TryParse<ExportFormat>(req.Format, true, out var parsedFormat) 
            ? parsedFormat 
            : ExportFormat.Json;

        DateTime? fromDate = null;
        DateTime? toDate = null;

        if (DateTime.TryParse(req.FromDate, out var from))
            fromDate = from;

        if (DateTime.TryParse(req.ToDate, out var to))
            toDate = to;

        var result = await _exportService.ExportTestbenchSummary(fromDate, toDate, format);

        if (!result.Success)
        {
            ThrowError(result.ErrorMessage);
            return;
        }

        HttpContext.Response.ContentType = result.ContentType;
        HttpContext.Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{result.Filename}\"");
        await HttpContext.Response.Body.WriteAsync(result.Data, ct);
    }
}

public class DownloadSummaryExportRequest
{
    public string Format { get; set; } = "json";
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
}