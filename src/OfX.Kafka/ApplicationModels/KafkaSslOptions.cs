namespace OfX.Kafka.ApplicationModels;

using Confluent.Kafka;

public class KafkaSslOptions
{
    /// <summary>
    /// A cipher suite is a named combination of authentication, encryption, MAC and key exchange algorithm used to negotiate the security settings for a network connection using TLS or SSL network protocol.
    /// </summary>
    public string SslCipherSuites { get; set; }

    /// <summary>
    /// The supported-curves extension in the TLS ClientHello message specifies the curves (standard/named, or 'explicit' GF(2^k) or GF(p)) the client is willing to have the server use.
    /// </summary>
    public string SslCurvesList { get; set; }

    /// <summary>
    /// The client uses the TLS ClientHello signature_algorithms extension to indicate to the server which signature/hash algorithm pairs may be used in digital signatures.
    /// </summary>
    public string SslSigalgsList { get; set; }

    /// <summary>
    /// Path to client's private key (PEM) used for authentication.
    /// </summary>
    public string SslKeyLocation { get; set; }

    /// <summary>
    /// Private key passphrase (for use with ssl.key.location and set_ssl_cert())
    /// </summary>
    public string SslKeyPassword { get; set; }

    /// <summary>
    /// Client's private key string (PEM format) used for authentication.
    /// </summary>
    public string SslKeyPem { get; set; }

    /// <summary>
    /// Path to client's public key (PEM) used for authentication.
    /// </summary>
    public string SslCertificateLocation { get; set; }

    /// <summary>
    /// Client's public key string (PEM format) used for authentication.
    /// </summary>
    public string SslCertificatePem { get; set; }

    /// <summary>
    /// File or directory path to CA certificate(s) for verifying the broker's key.
    /// </summary>
    public string SslCaLocation { get; set; }

    /// <summary>
    /// CA certificate string (PEM format) for verifying the broker's key.
    /// </summary>
    public string SslCaPem { get; set; }

    /// <summary>
    /// Comma-separated list of Windows Certificate stores to load CA certificates from.
    /// </summary>
    public string SslCaCertificateStores { get; set; }

    /// <summary>
    /// Path to CRL for verifying broker's certificate validity.
    /// </summary>
    public string SslCrlLocation { get; set; }

    /// <summary>
    /// Path to client's keystore (PKCS#12) used for authentication.
    /// </summary>
    public string SslKeystoreLocation { get; set; }

    /// <summary>
    /// Client's keystore (PKCS#12) password.
    /// </summary>
    public string SslKeystorePassword { get; set; }

    /// <summary>
    /// Comma-separated list of OpenSSL 3.0.x implementation providers. E.g., "default,legacy".
    /// </summary>
    public string SslProviders { get; set; }

    /// <summary>
    /// **DEPRECATED** Path to OpenSSL engine library. OpenSSL >= 1.1.x required.
    /// </summary>
    public string SslEngineLocation { get; set; }

    /// <summary>
    /// OpenSSL engine id is the name used for loading engine.
    /// </summary>
    public string SslEngineId { get; set; }

    /// <summary>
    /// Endpoint identification algorithm to validate broker hostname using broker certificate.
    /// </summary>
    public SslEndpointIdentificationAlgorithm? SslEndpointIdentificationAlgorithm { get; set; }
}