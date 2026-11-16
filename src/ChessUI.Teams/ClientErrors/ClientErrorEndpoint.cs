using System.Text.Json;

namespace ChessUI.Teams.ClientErrors;

/// <summary>Only the name for the log category of the Endpoint</summary>
public sealed class ClientErrorLog;


/// <summary>
/// The place where the browser reports its errors. The endpoint is open to everybody, so it is strict:
/// only JSON, small, rate-limited (see Program.cs), and the content is cleaned before it is logged
/// </summary>
public static class ClientErrorEndpoint
{
    public const string RateLimitPolicy = "client-errors";
    public const int MaxBodyBytes = 8 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IEndpointRouteBuilder MapClientErrors(this IEndpointRouteBuilder app)
    {
        app.MapPost("/client-errors", async (HttpRequest request, ILogger<ClientErrorLog> logger) =>
                Results.StatusCode(await ProcessAsync(request, logger)))
            .RequireRateLimiting(RateLimitPolicy)
            .DisableAntiforgery();      // It's a JSON endpoint: a form of another site cannot send JSON without a permission check

        return app;
    }

    /// <summary>Processes the Request</summary>
    /// <returns>The HTTP status code of the answer</returns>
    public static async Task<int> ProcessAsync(HttpRequest request, ILogger logger)
    {
        // A form on another website can only send simple content types, never JSON
        if (!request.HasJsonContentType())
            return StatusCodes.Status415UnsupportedMediaType;

        if (request.ContentLength > MaxBodyBytes)
            return StatusCodes.Status413PayloadTooLarge;

        // The length may be unknown (chunked upload), so never read more than the limit
        byte[] buffer = new byte[MaxBodyBytes + 1];
        int total = 0;
        int read;

        while ((read = await request.Body.ReadAsync(buffer.AsMemory(total))) > 0)
        {
            total += read;

            if (total > MaxBodyBytes)
                return StatusCodes.Status413PayloadTooLarge;
        }

        ClientErrorReport report;

        try
        {
            report = JsonSerializer.Deserialize<ClientErrorReport>(buffer.AsSpan(0, total), JsonOptions);
        }
        catch (JsonException)
        {
            return StatusCodes.Status400BadRequest;
        }

        if (report is null)
            return StatusCodes.Status400BadRequest;

        logger.LogWarning(
            "Browser reported {Kind} on page {Page}: {Message} ({Source}:{Line}:{Column})",
            ClientErrorSanitizer.Kind(report.Kind),
            ClientErrorSanitizer.Path(report.Page),
            ClientErrorSanitizer.Text(report.Message),
            ClientErrorSanitizer.Path(report.Source),
            ClientErrorSanitizer.Number(report.Line),
            ClientErrorSanitizer.Number(report.Column));

        return StatusCodes.Status204NoContent;
    }
}
