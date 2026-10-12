using System.Collections;

namespace Umbraco.Community.Licensing.Core;

/// <summary>An immutable list of features that keeps order. Names are matched ignoring <c>a</c>-<c>z</c> case only (ordinal, culture-free).</summary>
public sealed class FeatureList : IReadOnlyList<LicenseFeature>
{
    private readonly LicenseFeature[] _items;

    private FeatureList(LicenseFeature[] items) => _items = items;

    /// <summary>A list with no features.</summary>
    public static FeatureList Empty { get; } = new([]);

    /// <inheritdoc/>
    public int Count => _items.Length;

    /// <inheritdoc/>
    public LicenseFeature this[int index] => _items[index];

    /// <summary>Creates a list from <paramref name="features"/>, keeping order and duplicates so issue can report them.</summary>
    /// <param name="features">The features.</param>
    /// <returns>The list.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="features"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="features"/> holds a null entry.</exception>
    public static FeatureList Of(IEnumerable<LicenseFeature> features)
    {
        ArgumentNullException.ThrowIfNull(features);
        var items = features.ToArray();
        if (Array.Exists(items, f => f is null))
        {
            throw new ArgumentException("A feature list cannot hold a null entry.", nameof(features));
        }

        return items.Length == 0 ? Empty : new FeatureList(items);
    }

    /// <summary>Sets a switch: replaces the first same-name entry in place, else appends.</summary>
    /// <param name="name">The feature name.</param>
    /// <param name="on">Whether the switch is on.</param>
    /// <returns>A new list.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public FeatureList With(string name, bool on) => With(name, new SwitchFeature(on));

    /// <summary>Sets a number: replaces the first same-name entry in place, else appends.</summary>
    /// <param name="name">The feature name.</param>
    /// <param name="value">The number.</param>
    /// <returns>A new list.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public FeatureList With(string name, decimal value) => With(name, new NumberFeature(value));

    /// <summary>Sets a text: replaces the first same-name entry in place, else appends. A null text is kept and reported at issue.</summary>
    /// <param name="name">The feature name.</param>
    /// <param name="text">The text.</param>
    /// <returns>A new list.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public FeatureList With(string name, string text) => With(name, new TextFeature(text));

    /// <summary>Removes every entry whose name matches <paramref name="name"/>.</summary>
    /// <param name="name">The feature name.</param>
    /// <returns>A new list, or this list when nothing matched.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public FeatureList Without(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var kept = _items.Where(f => !NameMatches(f.Name, name)).ToArray();
        return kept.Length == _items.Length ? this : kept.Length == 0 ? Empty : new FeatureList(kept);
    }

    /// <inheritdoc/>
    public IEnumerator<LicenseFeature> GetEnumerator() => ((IEnumerable<LicenseFeature>)_items).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The features as the content rules take them, in order, duplicates kept.</summary>
    /// <exception cref="InvalidOperationException">A feature has a null value: a programming error, not a request problem.</exception>
    internal IReadOnlyList<FeatureInput> ToInputs() => _items.Select(ToInput).ToArray();

    private static FeatureInput ToInput(LicenseFeature feature) => feature.Value switch
    {
        SwitchFeature s => FeatureInput.Switch(feature.Name, s.IsOn),
        NumberFeature n => FeatureInput.OfNumber(feature.Name, n.Value),
        TextFeature t => FeatureInput.OfText(feature.Name, t.Value),
        _ => throw new InvalidOperationException("A feature needs a value."),
    };

    private FeatureList With(string name, FeatureValue value)
    {
        ArgumentNullException.ThrowIfNull(name);
        var entry = new LicenseFeature(name, value);
        var index = Array.FindIndex(_items, f => NameMatches(f.Name, name));
        if (index < 0)
        {
            return new FeatureList([.. _items, entry]);
        }

        var copy = (LicenseFeature[])_items.Clone();
        copy[index] = entry;
        return new FeatureList(copy);
    }

    /// <summary>Ordinal comparison that folds only <c>A</c>-<c>Z</c> to <c>a</c>-<c>z</c>; a null name matches nothing.</summary>
    private static bool NameMatches(string? existing, string name)
    {
        if (existing is null || existing.Length != name.Length)
        {
            return false;
        }

        for (var i = 0; i < name.Length; i++)
        {
            if (Fold(existing[i]) != Fold(name[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static char Fold(char c) => c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
}
