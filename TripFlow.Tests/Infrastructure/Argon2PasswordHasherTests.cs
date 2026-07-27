using FluentAssertions;
using TripFlow.Infrastructure.Security;
using Xunit;

namespace TripFlow.Tests.Infrastructure;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_SenhaCorreta_VerifyRetornaTrue()
    {
        var hash = _hasher.Hash("MinhaSenhaForte@123");

        _hasher.Verify("MinhaSenhaForte@123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_SenhaErrada_RetornaFalse()
    {
        var hash = _hasher.Hash("MinhaSenhaForte@123");

        _hasher.Verify("SenhaErrada", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_MesmaSenhaDuasVezes_GeraHashesDiferentes()
    {
        var hash1 = _hasher.Hash("MinhaSenhaForte@123");
        var hash2 = _hasher.Hash("MinhaSenhaForte@123");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_HashEmFormatoInvalido_RetornaFalseSemLancarExcecao()
    {
        _hasher.Verify("qualquer-senha", "hash-invalido-sem-o-formato-esperado").Should().BeFalse();
    }
}
