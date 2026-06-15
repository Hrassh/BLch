using System;

namespace BlockСh.Models
{
    public class Wallet
    {
        public string Name { get; set; }
        public string Address { get; set; }

        // Твои оригинальные бинарные поля (для метода Sign)
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }

        // =================================================================
        // ДОБАВЛЕНО ДЛЯ ЭКЗАМЕНА (ЧАСТЬ 1: СОВМЕСТИМОСТЬ С JSON И ШИФРОВАНИЕМ)
        // =================================================================
        // Эти свойства автоматически переводят byte[] в string Base64 для работы в WalletServices
        public string PublicKeyBase64
        {
            get => PublicKey != null ? Convert.ToBase64String(PublicKey) : null;
            set => PublicKey = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
        }

        public string PrivateKeyBase64
        {
            get => PrivateKey != null ? Convert.ToBase64String(PrivateKey) : null;
            set => PrivateKey = !string.IsNullOrEmpty(value) ? Convert.FromBase64String(value) : null;
        }

        // ВАЖНО: Пустой конструктор (исправляет ошибку "Отсутствует аргумент...")
        public Wallet() { }
        // =================================================================

        // Твой оригинальный конструктор
        public Wallet(string name, string address, byte[] publicKey, byte[] privateKey)
        {
            Name = name;
            Address = address;
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public byte[] Sign(byte[] data)
        {
            using var ecdsa = System.Security.Cryptography.ECDsa.Create();
            ecdsa.ImportECPrivateKey(PrivateKey, out _);
            return ecdsa.SignData(data, System.Security.Cryptography.HashAlgorithmName.SHA256);
        }
    }
}