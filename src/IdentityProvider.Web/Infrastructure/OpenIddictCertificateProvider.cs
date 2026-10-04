using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace IdentityProvider.Web.Infrastructure;

/// <summary>
/// Creates or loads persistent PFX certificates so token signing survives restarts
/// and works on Azure App Service (no user certificate store).
/// </summary>
public static class OpenIddictCertificateProvider
{
    public static X509Certificate2 GetOrCreate(string path, string password, bool signing)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (File.Exists(path))
        {
            return new X509Certificate2(
                path,
                password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        }

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN=IdentityProvider {(signing ? "Signing" : "Encryption")}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                signing ? X509KeyUsageFlags.DigitalSignature : X509KeyUsageFlags.KeyEncipherment,
                critical: true));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));

        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
        return certificate;
    }
}
