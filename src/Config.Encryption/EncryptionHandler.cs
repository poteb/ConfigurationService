using System.Security.Cryptography;
using System.Text;
using pote.Config.DbModel;

namespace pote.Config.Encryption;

public static class EncryptionHandler
{
    public static string Encrypt(string text, string key)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = "megalaekkerbacon"u8.ToArray();
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(text);
        }
        var encrypted = msEncrypt.ToArray();
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string encrypted, string key)
    {
        if (!IsStringEncrypted(encrypted.AsSpan())) return encrypted;
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = "megalaekkerbacon"u8.ToArray();
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var cipher = Convert.FromBase64String(encrypted);
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }

    public static Configuration Encrypt(Configuration configuration, string key)
    {
        if (!configuration.IsJsonEncrypted) return configuration;
        configuration.Json = Encrypt(configuration.Json, key);
        return configuration;
    }
    
    public static Configuration Decrypt(Configuration configuration, string key)
    {
        if (!configuration.IsJsonEncrypted) return configuration;
        configuration.Json = Decrypt(configuration.Json, key);
        return configuration;
    }

    public static IReadOnlyList<Configuration> Encrypt(IReadOnlyList<Configuration> configurations, string key)
    {
        foreach (var configuration in configurations) 
            Encrypt(configuration, key);
        return configurations;
    }
    
    public static IReadOnlyList<Configuration> Decrypt(IReadOnlyList<Configuration> configurations, string key)
    {
        foreach (var configuration in configurations) 
            Decrypt(configuration, key);
        return configurations;
    }
    
    public static Secret Encrypt(Secret configuration, string key)
    {
        configuration.Value = Encrypt(configuration.Value, key);
        return configuration;
    }
    
    public static Secret Decrypt(Secret secret, string key)
    {
        secret.Value = Decrypt(secret.Value, key);
        return secret;
    }

    public static IReadOnlyList<Secret> Encrypt(IReadOnlyList<Secret> secrets, string key)
    {
        foreach (var secret in secrets) 
            Encrypt(secret, key);
        return secrets;
    }
    
    public static IReadOnlyList<Secret> Decrypt(IReadOnlyList<Secret> secrets, string key)
    {
        foreach (var secret in secrets) 
            Decrypt(secret, key);
        return secrets;
    }

    private static bool IsStringEncrypted(ReadOnlySpan<char> text)
    {
        if (text.Length == 0) return false;
        return text[0] != '{';
    }
}