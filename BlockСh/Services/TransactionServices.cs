using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using BlockСh.Models;

namespace BlockСh.Services
{
    internal class TransactionServices
    {
        private readonly WalletServices _walletServices;


        public TransactionServices(WalletServices walletServices)
        {
            _walletServices = walletServices;
       
         
        }

        public Transaction CreatTrans(Wallet send, string to, decimal amount, decimal fee, string? replacesTxId = null)
        {
            var tx = new Transaction(send.Address, to, amount, fee);
            tx.ReplacesTxId = replacesTxId; // Записуємо ID старої транзакції, якщо він переданий

            tx.SenderPublicKey = send.PublicKey;
            tx.Signature = send.Sign(tx.GeDataToString());

            if (ValidateTransaction(tx).isValid)
            {
                return tx;
            }
            else
            {
                throw new ArgumentException("Invalid transaction data.");
            }
        }

        public (bool isValid, string errorMessage) ValidateTransaction(Transaction transaction, bool enableProtection = true)
        {
            if (transaction.From == "COINBASE") return (true, string.Empty);
            if (string.IsNullOrWhiteSpace(transaction.From)) return (false, "Sender address is required.");
            if (string.IsNullOrWhiteSpace(transaction.To)) return (false, "Recipient address is required.");
            if (transaction.Amount <= 0) return (false, "Amount must be greater than zero.");
            if (transaction.SenderPublicKey == null || transaction.Signature == null)
            {
                return (false, "Transaction must include sender's public key and signature.");
            }

            // =================================================================
            if (enableProtection)
            {
                string derivedAddress;
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(transaction.SenderPublicKey);
                    derivedAddress = Convert.ToBase64String(hashBytes).Substring(0, 16);
                }
                if (transaction.From != derivedAddress)
                {
                    string error = $"Публічний ключ не відповідає адресі відправника {transaction.From}!";
                    Console.WriteLine($"ВІДХИЛЕНО: {error}");
                    return (false, error);
                }
            }

            // =================================================================
            try
            {
                bool signatureValid = _walletServices.VerifySignature(transaction.GeDataToString(), transaction.Signature, transaction.SenderPublicKey);

                if (!signatureValid)
                {
                    string error = "Цифровий підпис пошкоджено або він недійсний.";
                    Console.WriteLine($"ВІДХИЛЕНО: {error}");
                    return (false, error);
                }
            }
            catch (CryptographicException)
            {
                string error = "Цифровий підпис критично пошкоджено (невірна структура байтів).";
                Console.WriteLine($"ВІДХИЛЕНО: {error}");
                return (false, error);
            }

            return (true, string.Empty);
        }
    }
}