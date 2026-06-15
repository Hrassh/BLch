using BlockСh.Models;
using System;
using System.Collections.Generic;

namespace BlockСh.Services
{
    public class BlockChainDisplayServ
    {
        public void DisplayChain(List<Models.Block> chain)
        {
            foreach (var block in chain)
            {
                Console.WriteLine($"Index: {block.index}");
                Console.WriteLine($"Timestamp: {block.Timestamp}");
                Console.WriteLine($"Hash: {block.Hash}");
                Console.WriteLine($"Nonce: {block.Nonce}");
                Console.WriteLine($"Previous Hash: {block.PreviousHash}");
                Console.WriteLine($"Diff: {block.Difficulty}");
                Console.WriteLine($"Author: {block.Author}");
                Console.WriteLine(new string('-', 40));
                PrintTransaction(block.Transactions);
            }
        }

        public void DisplayChainValidity(bool isValid)
        {
            if (isValid) Console.WriteLine("Valid");
            else Console.WriteLine("Invalid");
        }
        public void PrintTransaction(List<Models.Transaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                Console.WriteLine($"From: {transaction.From}");
                Console.WriteLine($"To: {transaction.To}");
                Console.WriteLine($"Amount: {transaction.Amount}");
                Console.WriteLine("==================================");
            }
        }
        public void PrintTransactionHistory(List<Block> chain, string name)
        {
            Console.WriteLine("Пошук для: " + name);
            bool found = false;

            for (int i = 1; i < chain.Count; i++)
            {
                Block currentBlock = chain[i];

                foreach (var tx in currentBlock.Transactions)
                {
                    if (tx.From.ToLower() == name.ToLower() || tx.To.ToLower() == name.ToLower())
                    {
                        Console.WriteLine("Блок #" + currentBlock.index + " | " + tx.From + " -> " + tx.To + " | Сума: " + tx.Amount);
                        found = true;
                    }
                }
            }

            if (found == false)
            {
                Console.WriteLine("Транзакцій не знайдено.");
            }
        }
        public void FindWhaleTransaction(List<Block> chain)
        {
            Transaction biggestTx = null;
            Block whaleBlock = null;

            for (int i = 1; i < chain.Count; i++)
            {
                Block currentBlock = chain[i];

                foreach (var tx in currentBlock.Transactions)
                {
                    if (biggestTx == null || tx.Amount > biggestTx.Amount)
                    {
                        biggestTx = tx;
                        whaleBlock = currentBlock;
                    }
                }
            }

            if (biggestTx != null && whaleBlock != null)
            {
                Console.WriteLine("🏆 Найбільша транзакція в мережі: Блок #" + whaleBlock.index + " | " + biggestTx.From + " -> " + biggestTx.To + " | Сума: " + biggestTx.Amount);
            }
        }
        public void DisplayWalletCard(string owner, string address, string publicKey)
        {
            string shortKey = publicKey.Length > 20 ? publicKey.Substring(0, 20) + "..." : publicKey;

            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine($"║ ВЛАСНИК:   {owner.PadRight(38)}    ║");
            Console.WriteLine($"║ АДРЕСА:    {address.PadRight(38)}    ║");
            Console.WriteLine($"║ ПУБ. КЛЮЧ: {shortKey.PadRight(38)}    ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        }
    }
}