namespace TripFlow.Application.Abstractions;

/// <summary>Criptografia simetrica reversivel pra dado que precisa ser lido de volta em claro
/// (ao contrario de senha, que so precisa ser verificada - por isso usa hash, nao isso aqui).</summary>
public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
