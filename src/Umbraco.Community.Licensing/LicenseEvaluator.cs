using System.Text;
using Umbraco.Community.Licensing.Internal;

namespace Umbraco.Community.Licensing;

/// <summary>
/// Evaluates a list of key strings for one product (consumer side). Never throws because of a
/// key's content.
/// </summary>
/// <param name="trustedKeys">The public keys this product trusts.</param>
/// <param name="timeProvider">The clock for the expiry check.</param>
public sealed class LicenseEvaluator(TrustedSigningKeys trustedKeys, TimeProvider timeProvider)
{
    private readonly TrustedSigningKeys _trustedKeys = trustedKeys ?? throw new ArgumentNullException(nameof(trustedKeys));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>Creates an evaluator on the system clock.</summary>
    public LicenseEvaluator(TrustedSigningKeys trustedKeys)
        : this(trustedKeys, TimeProvider.System)
    {
    }

    /// <summary>
    /// Evaluates <paramref name="keyStrings"/> for <paramref name="productId"/>: one row per
    /// string in the order supplied, and the combined entitlement.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="productId"/> is not a valid product ID.</exception>
    public LicenseEvaluation Evaluate(string productId, IEnumerable<string?> keyStrings)
    {
        ArgumentNullException.ThrowIfNull(keyStrings);
        if (!ProductIdRule.IsValid(productId))
        {
            throw new ArgumentException("Not a valid product ID (vendor.product, lowercase).", nameof(productId));
        }

        var work = keyStrings.Select((supplied, position) => Read(position, supplied, productId)).ToList();
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        // Duplicates: identical signing input (ADR-0001). The first copy counts.
        var firstBySigningInput = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in work.Where(w => w.License is not null))
        {
            if (firstBySigningInput.TryGetValue(row.SigningInput, out var first))
            {
                row.State = LicenseKeyState.Duplicate;
                row.DuplicateOf = first;
            }
            else
            {
                firstBySigningInput.Add(row.SigningInput, row.Position);
            }
        }

        // Superseding among verified, non-duplicate keys: same reference, strictly earlier.
        // Expiry and role play no part (PDR-0009).
        var candidates = work.Where(w => w.License is not null && w.State is null).ToList();
        foreach (var group in candidates.GroupBy(w => w.License!.Reference, StringComparer.Ordinal))
        {
            var latestIssued = group.Max(w => w.License!.Issued);
            var latest = group.Where(w => w.License!.Issued == latestIssued).ToList();
            foreach (var row in group.Where(w => w.License!.Issued < latestIssued))
            {
                row.State = LicenseKeyState.Superseded;
                row.SupersededBy = latest[0].Position;
                if (row.License!.Role != latest[0].License!.Role)
                {
                    row.SupersededRoleChange = row.License.Role;
                }
            }

            if (latest.Count > 1)
            {
                foreach (var row in latest)
                {
                    row.TiedWith = latest.Where(other => other != row).Select(other => other.License!.KeyIdentifier).ToList();
                }
            }
        }

        // Expiry: valid to the end of the expiry date in UTC.
        foreach (var row in candidates.Where(w => w.State is null))
        {
            if (row.License!.Expires is { } expires && today > expires)
            {
                row.State = LicenseKeyState.Expired;
            }
        }

        // Base and add-on (PDR-0011).
        var validBaseCount = candidates.Count(w => w.State is null && w.License!.Role == LicenseRole.Base);
        foreach (var row in candidates.Where(w => w.State is null))
        {
            row.State = row.License!.Role == LicenseRole.Base || validBaseCount > 0
                ? LicenseKeyState.Valid
                : LicenseKeyState.Inactive;
        }

        var counted = candidates.Where(w => w.State == LicenseKeyState.Valid).ToList();
        var (features, conflicts) = Combine(counted);

        var rows = work.Select(w => w.ToRow()).ToList();
        return new LicenseEvaluation(rows, new ProductLicense(productId, validBaseCount, features, conflicts));
    }

    private Work Read(int position, string? supplied, string productId)
    {
        var read = KeyString.Read(supplied);
        if (!read.IsReadable)
        {
            return Work.Failed(position, LicenseKeyState.Unreadable, read.Identifier, null);
        }

        var payload = read.Payload!;
        var claimedProduct = ProductIdRule.IsValid(payload.ProductId) ? payload.ProductId : null;

        if (!string.Equals(payload.ProductId, productId, StringComparison.Ordinal))
        {
            return Work.Failed(position, LicenseKeyState.WrongProduct, read.Identifier, claimedProduct);
        }

        if (!_trustedKeys.TryGet(payload.SigningKeyId!, out var publicKey))
        {
            return Work.Failed(position, LicenseKeyState.SigningKeyNotRecognised, read.Identifier, claimedProduct);
        }

        if (!publicKey.Verify(Encoding.ASCII.GetBytes(read.SigningInput), read.Signature))
        {
            return Work.Failed(position, LicenseKeyState.NotVerified, read.Identifier, claimedProduct);
        }

        // Verified, but contents that break a rule checked at issue are unreadable (PDR-0021).
        if (payload.Payload is not { } contents)
        {
            return Work.Failed(position, LicenseKeyState.Unreadable, read.Identifier, claimedProduct);
        }

        var identifier = read.Identifier!;
        var license = new VerifiedLicense(
            contents.ProductId,
            contents.Role,
            LicenseReferenceRule.ReferenceOf(identifier),
            identifier,
            contents.VendorTag,
            contents.Issued,
            contents.Expires,
            contents.Features.ToDictionary(StringComparer.Ordinal),
            contents.SigningKeyId);
        return new Work(position, identifier) { License = license, SigningInput = read.SigningInput };
    }

    /// <summary>
    /// Combines the features of counted keys (PDR-0010, PDR-0018): switches any, numbers summed,
    /// equal text as one value. Different text, or different types under one name, conflict.
    /// </summary>
    private static (IReadOnlyDictionary<string, FeatureValue> Features, IReadOnlyList<string> Conflicts) Combine(
        List<Work> counted)
    {
        var features = new Dictionary<string, FeatureValue>(StringComparer.Ordinal);
        var conflicts = new List<string>();

        var byName = counted
            .SelectMany(w => w.License!.Features.Select(f => (Row: w, Name: f.Key, Value: f.Value)))
            .GroupBy(f => f.Name, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in byName)
        {
            var values = group.Select(f => f.Value).ToList();
            var kinds = values.Select(v => v.Kind).Distinct().ToList();
            var conflict = kinds.Count > 1
                || (kinds[0] == FeatureKind.Text && values.Select(v => v.Text).Distinct(StringComparer.Ordinal).Count() > 1);

            if (conflict)
            {
                conflicts.Add(group.Key);
                foreach (var row in group.Select(f => f.Row))
                {
                    row.ConflictingFeatures.Add(group.Key);
                }

                continue;
            }

            features[group.Key] = kinds[0] switch
            {
                FeatureKind.Switch => FeatureValue.Switch,
                FeatureKind.Number => FeatureValue.FromNumber(NumberRule.Normalize(values.Sum(v => v.Number!.Value))),
                _ => values[0],
            };
        }

        return (features, conflicts);
    }

    private sealed class Work(int position, string? identifier)
    {
        public int Position { get; } = position;

        public string? Identifier { get; } = identifier;

        public LicenseKeyState? State { get; set; }

        public string? ClaimedProductId { get; init; }

        public VerifiedLicense? License { get; init; }

        public string SigningInput { get; init; } = "";

        public int? DuplicateOf { get; set; }

        public int? SupersededBy { get; set; }

        public LicenseRole? SupersededRoleChange { get; set; }

        public IReadOnlyList<string> TiedWith { get; set; } = [];

        public List<string> ConflictingFeatures { get; } = [];

        public static Work Failed(int position, LicenseKeyState state, string? identifier, string? claimedProduct) =>
            new(position, identifier) { State = state, ClaimedProductId = claimedProduct };

        public LicenseKeyRow ToRow() => new(Position, State!.Value, Identifier)
        {
            ClaimedProductId = License is null ? ClaimedProductId : null,
            License = License,
            DuplicateOf = DuplicateOf,
            SupersededBy = SupersededBy,
            SupersededRoleChange = SupersededRoleChange,

            // The vendor-error flag applies only while keys are tied for latest and counting
            // as latest; a duplicate or superseded key never carries it.
            TiedWith = State is LicenseKeyState.Superseded or LicenseKeyState.Duplicate ? [] : TiedWith,
            ConflictingFeatures = ConflictingFeatures.ToList(),
        };
    }
}
