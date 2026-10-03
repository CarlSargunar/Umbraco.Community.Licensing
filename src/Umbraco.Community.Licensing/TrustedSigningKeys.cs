using System.Collections;

namespace Umbraco.Community.Licensing;

/// <summary>
/// The public keys a product trusts, addressed by signing key ID. To rotate: add the new key,
/// switch issuing to it, and remove the old one once no valid license depends on it.
/// Safe for concurrent use.
/// </summary>
public sealed class TrustedSigningKeys : IEnumerable<SigningPublicKey>
{
    private readonly Dictionary<string, SigningPublicKey> _keys = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    /// <summary>Creates an empty set.</summary>
    public TrustedSigningKeys()
    {
    }

    /// <summary>Creates a set holding <paramref name="keys"/>.</summary>
    /// <exception cref="InvalidOperationException">Two different keys share a signing key ID.</exception>
    public TrustedSigningKeys(IEnumerable<SigningPublicKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        foreach (var key in keys)
        {
            Add(key);
        }
    }

    /// <summary>The number of trusted keys.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _keys.Count;
            }
        }
    }

    /// <summary>
    /// Trusts <paramref name="key"/>. Adding a key already held does nothing.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The set already holds a different public key under the same signing key ID; the set is unchanged.
    /// </exception>
    public void Add(SigningPublicKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_lock)
        {
            if (_keys.TryGetValue(key.SigningKeyId, out var held))
            {
                if (!held.Equals(key))
                {
                    throw new InvalidOperationException(
                        $"Signing key ID {key.SigningKeyId} is already held for a different public key.");
                }

                return;
            }

            _keys.Add(key.SigningKeyId, key);
        }
    }

    /// <summary>Stops trusting the key with <paramref name="signingKeyId"/>.</summary>
    /// <returns><c>true</c> when a key was removed.</returns>
    public bool Remove(string signingKeyId)
    {
        ArgumentNullException.ThrowIfNull(signingKeyId);
        lock (_lock)
        {
            return _keys.Remove(signingKeyId);
        }
    }

    /// <summary>Finds the trusted key with <paramref name="signingKeyId"/>.</summary>
    public bool TryGet(string signingKeyId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SigningPublicKey? key)
    {
        ArgumentNullException.ThrowIfNull(signingKeyId);
        lock (_lock)
        {
            return _keys.TryGetValue(signingKeyId, out key);
        }
    }

    /// <inheritdoc />
    public IEnumerator<SigningPublicKey> GetEnumerator()
    {
        lock (_lock)
        {
            return _keys.Values.ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
