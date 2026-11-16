namespace DocuMind.Application.Interfaces;

public interface ISecretProtector
{
    string Protect(string plainText);
    string Unprotect(string cipherText);
}
