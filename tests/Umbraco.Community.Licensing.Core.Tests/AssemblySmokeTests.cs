namespace Umbraco.Community.Licensing.Core.Tests;

public class AssemblySmokeTests
{
    [Fact]
    public void LibraryAssembly_Loads_WithExpectedName()
    {
        var assembly = typeof(AssemblyMarker).Assembly;

        Assert.Equal("Umbraco.Community.Licensing.Core", assembly.GetName().Name);
    }
}
