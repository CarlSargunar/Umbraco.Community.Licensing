using Umbraco.Community.Licensing.Core.Tests.Support;

namespace Umbraco.Community.Licensing.Core.Tests.Issuing;

public class FeatureListTests
{
    private static string[] Names(FeatureList list) => list.Select(f => f.Name).ToArray();

    [Fact]
    public void Empty_HasNoFeatures() => Assert.Empty(FeatureList.Empty);

    [Fact]
    public void With_AppendsNewNamesInOrder()
    {
        var list = FeatureList.Empty.With("b", true).With("a", 3m).With("c", "x");

        Assert.Equal(["b", "a", "c"], Names(list));
        Assert.Equal(new SwitchFeature(true), list[0].Value);
        Assert.Equal(new NumberFeature(3m), list[1].Value);
        Assert.Equal(new TextFeature("x"), list[2].Value);
    }

    [Fact]
    public void With_ReplacesSameNameIgnoringAsciiCaseAndKeepsPosition()
    {
        var list = FeatureList.Empty.With("one", true).With("Two", 1m).With("three", true);

        var changed = list.With("tWO", "now");

        Assert.Equal(["one", "tWO", "three"], Names(changed));
        Assert.Equal(new TextFeature("now"), changed[1].Value);
        Assert.Equal(3, changed.Count);
    }

    [Fact]
    public void With_ReplacesOnlyTheFirstDuplicate()
    {
        var list = FeatureList.Of([new("a", new SwitchFeature(true)), new("A", new SwitchFeature(true))]);

        var changed = list.With("a", 5m);

        Assert.Equal(new NumberFeature(5m), changed[0].Value);
        Assert.Equal(new SwitchFeature(true), changed[1].Value);
    }

    [Fact]
    public void With_DoesNotFoldNonAsciiCase()
    {
        using var _ = new CultureScope("en-US");
        var kelvin = "\u212A";
        var list = FeatureList.Empty.With(kelvin, true);

        var changed = list.With("k", true);

        Assert.Equal([kelvin, "k"], Names(changed));
    }

    [Fact]
    public void WithAndWithout_IgnoreTheCurrentCulture()
    {
        using var _ = new CultureScope("tr-TR");
        var list = FeatureList.Empty.With("ID", true).With("ıd", true);

        var changed = list.With("id", 2m);

        Assert.Equal(["id", "ıd"], Names(changed));
        Assert.Equal(["ıd"], Names(changed.Without("ID")));
    }

    [Fact]
    public void Of_KeepsDuplicatesAndOrder()
    {
        var list = FeatureList.Of([new("b", new SwitchFeature(true)), new("a", new SwitchFeature(true)), new("B", new SwitchFeature(true))]);

        Assert.Equal(["b", "a", "B"], Names(list));
    }

    [Fact]
    public void Of_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => FeatureList.Of(null!));
        Assert.Throws<ArgumentException>(() => FeatureList.Of([null!]));
    }

    [Fact]
    public void Of_CopiesTheSource()
    {
        var source = new List<LicenseFeature> { new("a", new SwitchFeature(true)) };
        var list = FeatureList.Of(source);

        source.Add(new("b", new SwitchFeature(true)));

        Assert.Single(list);
    }

    [Fact]
    public void Without_RemovesEveryMatchAndKeepsOrder()
    {
        var list = FeatureList.Of([new("a", new SwitchFeature(true)), new("b", new SwitchFeature(true)), new("A", new SwitchFeature(true)), new("c", new SwitchFeature(true))]);

        Assert.Equal(["b", "c"], Names(list.Without("a")));
    }

    [Fact]
    public void Without_AbsentName_ChangesNothing() => Assert.Equal(["a"], Names(FeatureList.Empty.With("a", true).Without("z")));

    [Fact]
    public void Lists_AreImmutable()
    {
        var original = FeatureList.Empty.With("a", true).With("b", true);

        _ = original.With("a", 2m);
        _ = original.With("c", true);
        _ = original.Without("a");

        Assert.Equal(["a", "b"], Names(original));
        Assert.Equal(new SwitchFeature(true), original[0].Value);
        Assert.Empty(FeatureList.Empty);
    }

    [Fact]
    public void NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FeatureList.Empty.With(null!, true));
        Assert.Throws<ArgumentNullException>(() => FeatureList.Empty.With(null!, 1m));
        Assert.Throws<ArgumentNullException>(() => FeatureList.Empty.With(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => FeatureList.Empty.Without(null!));
    }

    [Fact]
    public void Constructors_AcceptNullContentSoIssueCanReportIt()
    {
        var list = FeatureList.Of([new(null!, new TextFeature(null!))]);

        var input = Assert.Single(list.ToInputs());
        Assert.Null(input.Name);
        Assert.Equal(FeatureKind.Text, input.Kind);
        Assert.Null(input.Text);
    }

    [Fact]
    public void ToInputs_MapsEachKindPreservingOrderAndDuplicates()
    {
        var list = FeatureList.Of([
            new("s", new SwitchFeature(true)),
            new("n", new NumberFeature(2.5m)),
            new("t", new TextFeature("hi")),
            new("S", new SwitchFeature(false)),
        ]);

        var inputs = list.ToInputs();

        Assert.Equal(4, inputs.Count);
        Assert.Equal(FeatureInput.Switch("s", true), inputs[0]);
        Assert.Equal(FeatureInput.OfNumber("n", 2.5m), inputs[1]);
        Assert.Equal(FeatureInput.OfText("t", "hi"), inputs[2]);
        Assert.Equal(FeatureInput.Switch("S", false), inputs[3]);
    }

    [Fact]
    public void RequestDefaults_LeaveOptionalFieldsNull()
    {
        var request = new LicenseRequest();

        Assert.Null(request.DisplayName);
        Assert.Null(request.VendorTag);
        Assert.Null(request.Expiry);
        Assert.Same(FeatureList.Empty, request.Features);
        Assert.Equal(string.Empty, new LicenseRequest { DisplayName = "" }.DisplayName);
    }
}
