namespace Pkmds.Tests;

public class HostServiceTests
{
    [Fact]
    public void NoQueryString_NotEmbedded()
    {
        var svc = Build("https://localhost/");

        svc.IsEmbedded.Should().BeFalse();
        svc.HostKind.Should().Be(HostKind.None);
        svc.HostName.Should().BeNull();
    }

    [Fact]
    public void HostQueryParam_SetsEmbedded()
    {
        var svc = Build("https://localhost/?host=delta");

        svc.IsEmbedded.Should().BeTrue();
        svc.HostKind.Should().Be(HostKind.Generic);
        svc.HostName.Should().Be("delta");
    }

    [Fact]
    public void EmptyHostValue_NotEmbedded()
    {
        var svc = Build("https://localhost/?host=");

        svc.IsEmbedded.Should().BeFalse();
        svc.HostName.Should().BeNull();
    }

    [Fact]
    public void HostKeyCaseInsensitive_SetsEmbedded()
    {
        var svc = Build("https://localhost/?HOST=test");

        svc.IsEmbedded.Should().BeTrue();
        svc.HostName.Should().Be("test");
    }

    [Fact]
    public void HostValuePreservesOriginalCase()
    {
        var svc = Build("https://localhost/?host=Delta");

        svc.HostName.Should().Be("Delta");
    }

    [Fact]
    public void HostMixedWithOtherParams_StillDetected()
    {
        var svc = Build("https://localhost/?foo=bar&host=test&baz=qux");

        svc.IsEmbedded.Should().BeTrue();
        svc.HostName.Should().Be("test");
    }

    [Fact]
    public void UrlEncodedHostValue_Decoded()
    {
        var svc = Build("https://localhost/?host=test%20host");

        svc.HostName.Should().Be("test host");
    }

    [Fact]
    public void UnrelatedQueryParams_NotEmbedded()
    {
        var svc = Build("https://localhost/?foo=bar&baz=qux");

        svc.IsEmbedded.Should().BeFalse();
        svc.HostName.Should().BeNull();
    }

    private static HostService Build(string uri) => new(new FakeNavigationManager(uri));

    private sealed class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager(string uri)
        {
            Initialize("https://localhost/", uri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }
}
