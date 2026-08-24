namespace Pkmds.Tests;

public sealed class BugReportPrivacyTests
{
    [Fact]
    public void BugReportContractMakesContactOptionalAndExplicit()
    {
        var properties = typeof(BugReportRequest).GetProperties();

        properties.Select(property => property.Name).Should().Contain(["ContactOptIn", "ContactEmail"]);
        properties.Select(property => property.Name).Should().NotContain(["Name", "Email"]);

        var anonymousRequest = new BugReportRequest(BugReportCategory.Bug, "version", "user-agent");
        anonymousRequest.ContactOptIn.Should().BeFalse();
        anonymousRequest.ContactEmail.Should().BeNull();
    }

    [Fact]
    public void BugReportUiDisclosesPublicAndPrivateDataBeforeSubmission()
    {
        var dialog = RepoFileTestHelper.ReadAllText(
            "Pkmds.Rcl", "Components", "Dialogs", "BugReportDialog.razor");

        dialog.Should().Contain("Allow maintainers to email me about this report");
        dialog.Should().Contain("Contact email (private)");
        dialog.Should().Contain("posted publicly to GitHub");
        dialog.Should().Contain("Save-file attachments are private");
        dialog.Should().NotContain("Label=\"Name\"");
    }

    [Fact]
    public void BugReportBackendDoesNotPublishOrLogIdentityAndDoesNotCreateSasLinks()
    {
        var function = RepoFileTestHelper.ReadAllText(
            "Pkmds.Functions", "Functions", "SubmitBugReport.cs");
        var storage = RepoFileTestHelper.ReadAllText(
            "Pkmds.Functions", "Services", "BlobService.cs");

        function.Should().NotContain("form[\"name\"]");
        function.Should().NotContain("form[\"email\"]");
        function.Should().NotContain("**Reporter:**");
        function.Should().NotContain("{Email}");
        function.Should().NotContain("GetSasUrl");
        function.Should().Contain("contactOptIn");
        function.Should().Contain("stored privately for designated maintainers");
        storage.Should().NotContain("GenerateSasUri");
        storage.Should().Contain("contacts/open/{issueNumber}.json");
        storage.Should().Contain("attachments/{issueNumber}/{AttachmentBlobName}");
    }

    [Fact]
    public void AttachmentTransportDoesNotExposeOriginalFilename()
    {
        var client = RepoFileTestHelper.ReadAllText(
            "Pkmds.Web", "Services", "BugReportService.cs");

        client.Should().Contain("\"saveFile\", \"attachment.bin\"");
        client.Should().NotContain("\"saveFile\", request.SaveFileName");
    }

    [Fact]
    public void SubmissionMarkupOnlyLinksValidatedRepositoryIssueUrls()
    {
        var validResult = new BugReportResult(true, "https://github.com/codemonkey85/PKMDS-Blazor/issues/1217");
        var maliciousResult = new BugReportResult(
            true,
            "https://example.com/\" onmouseover=\"alert(1)",
            WarningMessage: "<img src=x onerror=alert(1)>");

        validResult.ToSubmissionMarkup("Submitted").Value.Should().Contain(
            "rel=\"noopener noreferrer\"");
        maliciousResult.ToSubmissionMarkup("Submitted").Value.Should().Be(
            "&lt;img src=x onerror=alert(1)&gt;");
    }

    [Fact]
    public void ProvisioningEnforcesPrivateDataRetentionCeilings()
    {
        var setup = RepoFileTestHelper.ReadAllText("setup-azure.ps1");
        var workflow = RepoFileTestHelper.ReadAllText(".github", "workflows", "main.yml");
        var policy = RepoFileTestHelper.ReadAllText("Pkmds.Functions", "report-retention-policy.json");

        setup.Should().Contain("delete-report-attachments-after-30-days");
        setup.Should().Contain("delete-open-contact-records-after-12-months");
        setup.Should().Contain("delete-closed-contact-records-after-30-days");
        setup.Should().Contain("daysAfterCreationGreaterThan = 364");
        setup.Should().Contain("daysAfterCreationGreaterThan = 29");
        workflow.Should().Contain("--policy @Pkmds.Functions/report-retention-policy.json");
        policy.Should().Contain("\"daysAfterCreationGreaterThan\": 364");
        policy.Should().Contain("\"daysAfterCreationGreaterThan\": 29");
        policy.Should().Contain("\"bug-reports/0\"");
        policy.Should().Contain("\"bug-reports/9\"");
        setup.Should().Contain("\"$BlobContainer/0\"");
        setup.Should().Contain("\"$BlobContainer/9\"");
    }
}
