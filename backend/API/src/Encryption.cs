
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

public static class Encryption
{
    public static (string public_key, string private_key) generateRSA()
    {
        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        return (rsa.ToXmlString(false),rsa.ToXmlString(true));
    }

    public static string AsymmetricEncrypt(string content, string key)
    {
        RSACryptoServiceProvider cipher = new RSACryptoServiceProvider();
        cipher.FromXmlString(key);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
        byte[] cipherText = cipher.Encrypt(data, false);
        return Convert.ToBase64String(cipherText);
    }

    public static string AsymmetricDecrypt(string content, string key)
    {
            RSACryptoServiceProvider cipher = new RSACryptoServiceProvider();
            cipher.FromXmlString(key);

            byte[] ciphterText = Convert.FromBase64String(content);
            byte[] plainText = cipher.Decrypt(ciphterText, false);
            return System.Text.Encoding.UTF8.GetString(plainText);    
    }

    public static string generateSalt(int bits = 128)
    {
        byte[] salt = new byte[bits / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetNonZeroBytes(salt);
            return Convert.ToBase64String(salt);
        }
    }

    public static string HashPassword(string password, string salt_string, int bytes = 256/8)
    {
        byte[] salt = Convert.FromBase64String(salt_string);

        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: bytes));

        return hashed;
    }

    public static string SymmetricEncrypt(string key, string plainText)  
        {  
            byte[] iv = new byte[16];  
            byte[] array;  

            using (Aes aes = Aes.Create())  
            {  
        
                aes.Key = Convert.FromBase64String(HashPassword(key, "", 24)); //System.Text.Encoding.UTF8.GetBytes(key);  
                aes.IV = iv;  
  
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);  
  
                using (MemoryStream memoryStream = new MemoryStream())  
                {  
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))  
                    {  
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))  
                        {  
                            streamWriter.Write(plainText);  
                        }  
  
                        array = memoryStream.ToArray();  
                    }  
                }  
            }  
  
            return Convert.ToBase64String(array);  
        }  
  
        public static string SymmetricDecrypt(string key, string cipherText)  
        {  
            byte[] iv = new byte[16];  
            byte[] buffer = Convert.FromBase64String(cipherText);  
  
            using (Aes aes = Aes.Create())  
            {  
                aes.Key = Convert.FromBase64String(HashPassword(key, "", 24)); 
                aes.IV = iv;  
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);  
  
                using (MemoryStream memoryStream = new MemoryStream(buffer))  
                {  
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))  
                    {  
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))  
                        {  
                            return streamReader.ReadToEnd();  
                        }  
                    }  
                }  
            }  
        }
}