namespace Umbraco.Community.Licensing.Core.Tests.Signing;

/// <summary>The throwaway example key pair published in ADR-0005. Trusted by tests only.</summary>
internal static class ExampleKeys
{
    public const string SigningKeyId = "Nb_sm4Fxh5c";

    public const string PublicKey =
        "Nb_sm4Fxh5c.MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEU3Th2LxtmpokIglgJQMTPGENY3hAKh5m7pAz4inUYfZv-wgUo28dAgl5sOMDF5GZfoT2P-_UEkLARRvYoK0_dA";

    public const string PrivateKeyPem =
        """
        -----BEGIN PRIVATE KEY-----
        MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgDSxFHquMwza8xVc8
        2go00wyccvzla9OspC5gg9ZDbBmhRANCAARTdOHYvG2amiQiCWAlAxM8YQ1jeEAq
        HmbukDPiKdRh9m/7CBSjbx0CCXmw4wMXkZl+hPY/79QSQsBFG9igrT90
        -----END PRIVATE KEY-----
        """;
}
