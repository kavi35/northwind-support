using System.Net.Mime;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SupportDesk.Presentation.Health;

/// <summary>Writes the health report as JSON, including each check's status.</summary>
public static class HealthCheckResponse
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;

        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        });
    }
}
