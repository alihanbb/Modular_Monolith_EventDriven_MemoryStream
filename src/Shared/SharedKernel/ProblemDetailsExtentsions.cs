using System.Text.Json;
using Microsoft.Extensions.Logging;
using Refit;

namespace SharedKernel;

public static class ProblemDetailsExtensions
{
    public static void LogProblemDetails(this ILogger logger, ApiException? apiException)
    {
        if (string.IsNullOrEmpty(apiException!.Content))
        {
            logger.LogError(apiException.Message);
            return;
        }

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(apiException.Content!);

        if (problemDetails is null) return;

        logger.LogError(
            "Problem Details: Title: {Title}, Detail: {Detail}, Status: {Status}",
            problemDetails.Title,
            problemDetails.Detail,
            problemDetails.Status);
    }
}