using System.Net;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pkmds.Tests;

public class DescriptionServiceTests
{
    [Fact]
    public async Task ConcurrentItemQueries_ShareDownloadAndPreserveUnicode()
    {
        var handler = new PendingDataHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://pkmds.test/") };
        var service = new DescriptionService(client, NullLogger<DescriptionService>.Instance);

        var first = service.GetItemInfoAsync("Potion", GameVersion.SL);
        var second = service.GetItemInfoAsync("Potion", GameVersion.SL);
        handler.Response.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"potion":{"name":"Potion","description":"Pokémon café — 回復"}}""", System.Text.Encoding.UTF8)
        });

        var results = await Task.WhenAll(first, second);

        handler.Requests.Should().Be(1);
        results.Should().OnlyContain(result => result != null && result.Description == "Pokémon café — 回復");
        (await service.GetItemInfoAsync("Potion", GameVersion.SL))!.Description.Should().Be(results[0]!.Description);
        handler.Requests.Should().Be(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "not-json")]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    public async Task FailedItemDownload_UsesExistingCachedEmptyFallback(HttpStatusCode status, string data)
    {
        var handler = new PendingDataHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://pkmds.test/") };
        var service = new DescriptionService(client, NullLogger<DescriptionService>.Instance);
        handler.Response.SetResult(new HttpResponseMessage(status) { Content = new StringContent(data) });

        (await service.GetItemInfoAsync("Potion", GameVersion.SL)).Should().BeNull();
        (await service.GetItemInfoAsync("Potion", GameVersion.SL)).Should().BeNull();
        handler.Requests.Should().Be(1);
    }

    [Theory]
    [InlineData("Black Augurite", "Evolves Scyther into Kleavor when used.")]
    [InlineData("Peat Block", "Evolves Ursaring into Ursaluna when used during a full moon.")]
    [InlineData("Blank Plate", "Changes Arceus to the Normal type when used in Legends: Arceus.")]
    [InlineData("Legend Plate", "In Legends: Arceus, Arceus's Judgment changes type to be supereffective.")]
    public async Task GetItemInfoAsync_UsesRefreshedGeneratedDescriptions(string name, string expected)
    {
        using var client = CreateClient(RepoFileTestHelper.ReadAllText("Pkmds.Rcl", "wwwroot", "data", "item-info.json"));
        var service = new DescriptionService(client, NullLogger<DescriptionService>.Instance);

        var info = await service.GetItemInfoAsync(name, GameVersion.PLA);

        info.Should().NotBeNull();
        info.Description.Should().Be(expected);
    }

    [Theory]
    [InlineData(GameVersion.R, "Ruby flavor")]
    [InlineData(GameVersion.E, "Ruby flavor")]
    [InlineData(GameVersion.BD, "Diamond flavor")]
    [InlineData(GameVersion.RD, "Ruby flavor")]
    public async Task GetItemInfoAsync_PrefersGenerationAppropriateFlavor(GameVersion version, string expected)
    {
        const string data = """
                            {"potion":{"name":"Potion","description":"General effect",
                            "flavor":{"5":"Ruby flavor","8":"Diamond flavor","24":"Arceus flavor"}}}
                            """;
        using var client = CreateClient(data);
        var service = new DescriptionService(client, NullLogger<DescriptionService>.Instance);

        var info = await service.GetItemInfoAsync("Potion", version);

        info.Should().NotBeNull();
        info.Description.Should().Be(expected);
    }

    private static HttpClient CreateClient(string json) => new(new ItemDataHandler(json))
    {
        BaseAddress = new Uri("https://pkmds.test/")
    };

    private sealed class ItemDataHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri!.AbsolutePath.Should().Be("/_content/Pkmds.Rcl/data/item-info.json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }

    private sealed class PendingDataHandler : HttpMessageHandler
    {
        internal TaskCompletionSource<HttpResponseMessage> Response { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri!.AbsolutePath.Should().Be("/_content/Pkmds.Rcl/data/item-info.json");
            Requests++;
            return Response.Task;
        }
    }
}
