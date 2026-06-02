using System.Net.Http.Json;
using System.Text.Json;

namespace Pkmds.Web.Services;

public class BugReportService(IConfiguration configuration, HttpClient httpClient) : IBugReportService
{
    private readonly string? functionUrl = configuration["BugReportService:FunctionUrl"];

    public async Task<BugReportResult> SubmitBugReportAsync(BugReportRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(functionUrl))
        {
            return new BugReportResult(false, ErrorMessage: "Bug reporting is not configured.");
        }

        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(request.Name), "name");
            content.Add(new StringContent(request.Email), "email");
            content.Add(new StringContent(request.Category.ToString()), "category");
            content.Add(new StringContent(request.AppVersion), "appVersion");
            content.Add(new StringContent(request.UserAgent), "userAgent");

            if (!string.IsNullOrWhiteSpace(request.Actual))
            {
                content.Add(new StringContent(request.Actual), "actual");
            }

            if (!string.IsNullOrWhiteSpace(request.Steps))
            {
                content.Add(new StringContent(request.Steps), "steps");
            }

            if (!string.IsNullOrWhiteSpace(request.Expected))
            {
                content.Add(new StringContent(request.Expected), "expected");
            }

            if (!string.IsNullOrWhiteSpace(request.ReportedSaveSource))
            {
                content.Add(new StringContent(request.ReportedSaveSource), "reportedSaveSource");
            }

            if (!string.IsNullOrWhiteSpace(request.Details))
            {
                content.Add(new StringContent(request.Details), "details");
            }

            if (!string.IsNullOrWhiteSpace(request.PkhexVersion))
            {
                content.Add(new StringContent(request.PkhexVersion), "pkhexVersion");
            }

            if (request.SaveGameName is not null)
            {
                content.Add(new StringContent(request.SaveGameName), "saveGameName");
            }

            if (request.SaveRevision is not null)
            {
                content.Add(new StringContent(request.SaveRevision), "saveRevision");
            }

            if (request.SaveFileSource is not null)
            {
                content.Add(new StringContent(request.SaveFileSource), "saveFileSource");
            }

            if (request.SaveFileType is not null)
            {
                content.Add(new StringContent(request.SaveFileType), "saveFileType");
            }

            if (request is { SaveFileBytes: { Length: > 0 } saveBytes, SaveFileName: not null })
            {
                content.Add(new ByteArrayContent(saveBytes), "saveFile", request.SaveFileName);
            }

            var response = await httpClient.PostAsync($"{functionUrl}/api/SubmitBugReport", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                var issueUrl = json.TryGetProperty("issueUrl", out var urlElement)
                    ? urlElement.GetString()
                    : null;
                return new BugReportResult(true, IssueUrl: issueUrl);
            }

            var errorJson = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var errorMessage = errorJson.TryGetProperty("error", out var errorElement)
                ? errorElement.GetString()
                : $"Submission failed with status {(int)response.StatusCode}.";
            return new BugReportResult(false, ErrorMessage: errorMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new BugReportResult(false, ErrorMessage: "Failed to submit report. Please check your connection and try again.");
        }
    }
}
