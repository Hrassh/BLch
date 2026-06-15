using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using BlockСh.Models;

namespace BlockСh.Services
{
    internal class WalletServices
    {
        private class EncryptedWalletData
        {
            public string Address { get; set; }
            public string PublicKeyBase64 { get; set; }
            public string EncryptedPrivateKey { get; set; }
        }

        public Wallet CreatWall(string name)
        {
            using var ecdsa = ECDsa.Create();
            var publicKey = ecdsa.ExportSubjectPublicKeyInfo();
            var privateKey = ecdsa.ExportECPrivateKey();
            string address;
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(publicKey);
                address = Convert.ToBase64String(hashBytes).Replace("/", "").Replace("+", "").Substring(0, 16);
            }

            return new Wallet(name, address, publicKey, privateKey);
        }

        public bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey)
        {
            try
            {
                if (signature == null || publicKey == null) return false;

                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
                return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
            catch
            {
                return false;
            }
        }

        public void SaveWallet(Wallet wallet, string password, string name)
        {
            string filePath = $"wallet_{name}.json";

            string encryptedPrivate = EncryptString(wallet.PrivateKeyBase64, password);

            var data = new EncryptedWalletData
            {
                Address = wallet.Address,
                PublicKeyBase64 = wallet.PublicKeyBase64,
                EncryptedPrivateKey = encryptedPrivate
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public Wallet LoadWallet(string name, string password)
        {
            string filePath = $"wallet_{name}.json";
            if (!File.Exists(filePath)) throw new FileNotFoundException("Файл гаманця не знайдено!");

            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<EncryptedWalletData>(json);

            try
            {
                string decryptedPrivate = DecryptString(data.EncryptedPrivateKey, password);

                var wallet = new Wallet();
                wallet.Name = name;
                wallet.Address = data.Address;
                wallet.PublicKeyBase64 = data.PublicKeyBase64;
                wallet.PrivateKeyBase64 = decryptedPrivate;

                return wallet;
            }
            catch
            {
                throw new Exception("Невірний пароль гаманця!");
            }
        }

        private string EncryptString(string text, string password)
        {
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var iv = new byte[16];

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            var inputBytes = Encoding.UTF8.GetBytes(text);
            var encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        private string DecryptString(string cipherText, string password)
        {
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var iv = new byte[16];

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}