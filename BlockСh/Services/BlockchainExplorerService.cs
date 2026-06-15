using BlockСh.Models;
using BlockСh.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockCh.Services
{
    public class BlockchainExplorerService
    {
        private readonly BlockChainServices _blockchain;

        // Конструктор приймає твоє блокчейн-ядро
        public BlockchainExplorerService(BlockChainServices blockchain)
        {
            _blockchain = blockchain;
        }

        // 1. Пошук транзакції по всьому ланцюгу блоків (Chain) та в Мемпулі
        public Transaction FindTransactionById(string txId)
        {
            if (string.IsNullOrEmpty(txId)) return null;

            // Крок А: Шукаємо в уже змайнених блоках
            var txInChain = _blockchain.Chain
                .SelectMany(block => block.Transactions) // Згладжуємо список списків транзакцій в один потік
                .FirstOrDefault(tx => tx.Id == txId);

            if (txInChain != null) return txInChain;

            // Крок Б: Якщо в ланцюгу немає, перевіряємо пул незатверджених транзакцій (Мемпул)
            // (Залежно від твоєї архітектури поле може називатися PendingTransactions або Mempool)
            return _blockchain.PendingTransactions
                .FirstOrDefault(tx => tx.Id == txId);
        }

        // 2. Знаходження блоку, у який була успішно запакована вказана транзакція
        public Block FindBlockByTransactionId(string txId)
        {
            if (string.IsNullOrEmpty(txId)) return null;

            // Шукаємо перший блок, у списку транзакцій якого є транзакція з таким ID
            return _blockchain.Chain
                .FirstOrDefault(block => block.Transactions.Any(tx => tx.Id == txId));
        }

        // 3. Повна історія вхідних та вихідних переказів для конкретної адреси (від найновіших до найстаріших)
        public List<Transaction> GetTransactionHistory(string address)
        {
            if (string.IsNullOrEmpty(address)) return new List<Transaction>();

            return _blockchain.Chain
                .SelectMany(block => block.Transactions)
                .Where(tx => string.Equals(tx.From, address, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(tx.To, address, StringComparison.OrdinalIgnoreCase))
                .Reverse()
                .ToList();
        }

        // 4. Підрахунок суми всіх комісій (Fee), які заробив конкретний майнер за історію
        public decimal GetTotalFeesEarned(string minerAddress)
        {
            if (string.IsNullOrEmpty(minerAddress)) return 0;

            // Проходимо по всіх блоках ланцюга
            return _blockchain.Chain
                .Where(block => block.Transactions.Any(tx => tx.From == "COINBASE" && tx.To == minerAddress))

                // Для кожного такого блоку беремо всі його транзакції (крім самої системної COINBASE) і сумуємо їх комісію Fee
                .SelectMany(block => block.Transactions)
                .Where(tx => tx.From != "COINBASE")
                .Sum(tx => tx.Fee);
        }
    }
}