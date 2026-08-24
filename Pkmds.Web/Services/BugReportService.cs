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
            content.Add(new StringContent(request.Category.ToString()), "category");
            content.Add(new StringContent(request.AppVersion), "appVersion");
            content.Add(new StringContent(request.UserAgent), "userAgent");
            content.Add(new StringContent(request.ContactOptIn.ToString()), "contactOptIn");

            if (request.ContactOptIn && !string.IsNullOrWhiteSpace(request.ContactEmail))
            {
                content.Add(new StringContent(request.ContactEmail), "contactEmail");
            }

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

            if (!string.IsNullOrWhiteSpace(request.SaveGameName))
            {
                content.Add(new StringContent(request.SaveGameName), "saveGameName");
            }

            if (!string.IsNullOrWhiteSpace(request.SaveRevision))
            {
                content.Add(new StringContent(request.SaveRevision), "saveRevision");
            }

            if (!string.IsNullOrWhiteSpace(request.SaveFileSource))
            {
                content.Add(new StringContent(request.SaveFileSource), "saveFileSource");
            }

            if (!string.IsNullOrWhiteSpace(request.SaveFileType))
            {
                content.Add(new StringContent(request.SaveFileType), "saveFileType");
            }

            if (request is { SaveFileBytes: { Length: > 0 } saveBytes, SaveFileName: not null })
            {
                var extension = Path.GetExtension(request.SaveFileName);
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    content.Add(new StringContent(extension), "saveFileExtension");
                }

                // Never place the user's original filename in multipart headers. Filenames can
                // contain names or local paths and may be captured by infrastructure logs.
                content.Add(new ByteArrayContent(saveBytes), "saveFile", "attachment.bin");
            }

            var response = await httpClient.PostAsync($"{functionUrl}/api/SubmitBugReport", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                var issueUrl = json.TryGetProperty("issueUrl", out var urlElement)
                    ? urlElement.GetString()
                    : null;
                var contactStored = !request.ContactOptIn ||
                                    (json.TryGetProperty("contactStored", out var contactStoredElement) &&
                                     contactStoredElement.ValueKind is JsonValueKind.True);
                var attachmentRequested = request.SaveFileBytes is { Length: > 0 };
                var attachmentStored = !attachmentRequested ||
                                       (json.TryGetProperty("attachmentStored", out var attachmentStoredElement) &&
                                        attachmentStoredElement.ValueKind is JsonValueKind.True);
                var warnings = new List<string>();
                if (!contactStored)
                {
                    warnings.Add(
                        "Your private contact address could not be stored. Please follow the GitHub issue for updates.");
                }

                if (!attachmentStored)
                {
                    warnings.Add(
                        "Your save-file attachment could not be stored. Keep your local copy in case a maintainer asks for it.");
                }

                var warningMessage = warnings.Count == 0
                    ? null
                    : $"The report was created, but {string.Join(" ", warnings)}";
                return new BugReportResult(true, IssueUrl: issueUrl, WarningMessage: warningMessage);
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
