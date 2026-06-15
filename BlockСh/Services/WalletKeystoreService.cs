using BlockСh.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlockCh.Services
{
    public class WalletKeystoreService
    {
        private class KeystoreFileStructure
        {
            public string Address { get; set; }
            public string PublicKeyBase64 { get; set; }
            public string EncryptedPrivateKeyBase64 { get; set; }
        }

        public void SaveWallet(Wallet wallet, string password)
        {
            string filePath = $"wallet_{wallet.Name}.json";
            string privateKeyRawText = Convert.ToBase64String(wallet.PrivateKey);
            string encryptedPrivateKey = EncryptWithAes(privateKeyRawText, password);

            var fileData = new KeystoreFileStructure
            {
                Address = wallet.Address,
                PublicKeyBase64 = Convert.ToBase64String(wallet.PublicKey),
                EncryptedPrivateKeyBase64 = encryptedPrivateKey
            };

            string jsonResult = JsonSerializer.Serialize(fileData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, jsonResult);
        }

        public Wallet LoadWallet(string name, string password)
        {
            string filePath = $"wallet_{name}.json";

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл гаманця 'wallet_{name}.json' не знайдено!");

            string jsonContent = File.ReadAllText(filePath);
            var fileData = JsonSerializer.Deserialize<KeystoreFileStructure>(jsonContent);

            try
            {
                string decryptedKeyText = DecryptWithAes(fileData.EncryptedPrivateKeyBase64, password);

                byte[] publicKeyBytes = Convert.FromBase64String(fileData.PublicKeyBase64);
                byte[] privateKeyBytes = Convert.FromBase64String(decryptedKeyText);

                return new Wallet(name, fileData.Address, publicKeyBytes, privateKeyBytes);
            }
            catch
            {
                throw new Exception("Невірний пароль");
            }
        }

        private string EncryptWithAes(string plainText, string password)
        {
            byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            byte[] iv = new byte[16];

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        private string DecryptWithAes(string cipherText, string password)
        {
            byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            byte[] iv = new byte[16];

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}