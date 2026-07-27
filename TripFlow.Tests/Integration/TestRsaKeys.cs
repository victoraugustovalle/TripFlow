using System.Security.Cryptography;

namespace TripFlow.Tests.Integration;

/// <summary>Par RSA gerado uma vez por execucao de teste - so serve pra assinar/validar
/// os JWTs emitidos durante os testes, nao tem nenhum uso fora disso.</summary>
public static class TestRsaKeys
{
    private static readonly RSA Rsa = RSA.Create(2048);

    public static readonly string PrivateKeyPem = Rsa.ExportRSAPrivateKeyPem();
    public static readonly string PublicKeyPem = Rsa.ExportRSAPublicKeyPem();
}
