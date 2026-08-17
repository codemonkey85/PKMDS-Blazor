namespace Pkmds.Tests;

public class AutocompleteSelectTests
{
    [Fact]
    public void MaxItems_DefaultsToBoundedMudBlazorValue()
    {
        var component = new AutocompleteSelect<ComboItem>();

        component.MaxItems.Should().Be(10);
    }
}
