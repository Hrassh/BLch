using BlockСh.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlockСh.Services
{
    public class BlockChainServices
    {
        public List<Block> Chain { get; set; }
        public List<Transaction> PendingTransactions { get; set; }
        public decimal MiningReward { get; set; } = 50;
        public int Difficulty { get; set; } = 3;
        private readonly int _adjustmentInterval = 2;
        private readonly double _targetTimePerBlock = 5;
        public int MaxTransactionsPerBlock { get; set; } = 3;
        public int MaxPendingTxPerAddress { get; set; } = 2;
        public decimal BaseFeePerByte { get; set; } = 0.05m;
        public int MaxBlockSizeBytes { get; set; } = 150;
        public int CoinbaseMaturity { get; set; } = 3;
        public int MaxReorgDepth { get; set; } = 5;
        private readonly HashingService _hashingService;
        private readonly MiningServices _miningServices;
        private readonly TransactionServices _transactionServices;
        private readonly FileService _fileServices;

        public BlockChainServices()
        {
            Chain = new List<Block>();
            _hashingService = new HashingService();
            _miningServices = new MiningServices();
            _transactionServices = new TransactionServices(new WalletServices());
            PendingTransactions = new List<Transaction>();
            _fileServices = new FileService();

            var loadedChain = _fileServices.LoadChain();

            if (loadedChain != null && loadedChain.Any())
            {
                bool linksValid = true;
                for (int i = 1; i < loadedChain.Count; i++)
                {
                    if (loadedChain[i].PreviousHash != loadedChain[i - 1].Hash)
                    {
                        linksValid = false;
                        break;
                    }
                }

                if (linksValid)
                {
                    Chain = loadedChain;
                    Console.WriteLine("Blockchain loaded successfully from file.");
                }
                else
                {
                    Console.WriteLine("Loaded blockchain structure is corrupted. Starting with a new chain.");
                    CreateGenesisBlock();
                }
            }
            else
            {
                CreateGenesisBlock();
                Console.WriteLine("Created a new genesis block.");
            }
        }

        private void CreateGenesisBlock()
        {
            var genesisBlock = new Block(0, DateTime.UtcNow, new List<Transaction>(), "", "0", "System", "Genesis");
            genesisBlock.Timestamp = new DateTime(2024, 1, 1);
            genesisBlock.Hash = _hashingService.ComputeHash(genesisBlock);
            Chain.Add(genesisBlock);
        }

        public decimal GetCurrentNetworkFee()
        {
            int currentMempoolSizeBytes = PendingTransactions.Sum(tx => tx.Size);

            if (currentMempoolSizeBytes <= MaxBlockSizeBytes)
            {
                return BaseFeePerByte;
            }
            int multiplier = currentMempoolSizeBytes / MaxBlockSizeBytes;

            if (multiplier < 1) multiplier = 1;
            return BaseFeePerByte * multiplier;
        }

        public Block MinePendingTransactions(string minerAddress)
        {
            var previousBlock = Chain.Last();
            int txSpaceAvailable = MaxTransactionsPerBlock - 1;

            var transactionsToPack = PendingTransactions
                .OrderByDescending(tx => tx.Fee)
                .Take(txSpaceAvailable)
                .ToList();

            var blockTransactions = new List<Transaction>(transactionsToPack);

            var totalFees = transactionsToPack.Sum(tx => tx.Fee);


            var rewardTransaction = new Transaction("COINBASE", minerAddress, MiningReward + totalFees, 0, "MAIN");
            blockTransactions.Add(rewardTransaction);

            var newBlock = new Block(
                previousBlock.index + 1,
                DateTime.UtcNow,
                blockTransactions,
                "",
                "",
                previousBlock.Hash,
                minerAddress
            );

            newBlock.Difficulty = Difficulty;

            var tree = _hashingService.BuildMerkleTree(newBlock.Transactions);
            newBlock.MerkleRoot = tree.Count > 0 ? tree.Last().FirstOrDefault() : string.Empty;

            _miningServices.MineBlock(newBlock, Difficulty);
            newBlock.Hash = _hashingService.ComputeHash(newBlock);

            Chain.Add(newBlock);
            _fileServices.SaveChain(Chain);

            foreach (var tx in transactionsToPack)
            {
                PendingTransactions.Remove(tx);
            }

            Console.WriteLine($"\n[Майнинг] У блок #{newBlock.index} успішно упаковано {transactionsToPack.Count} найвигідніших транзакцій з пулу + 1 нагорода.");
            Console.WriteLine($"[Майнинг] У Mempool залишилося чекати черги: {PendingTransactions.Count} транзакцій.\n");

            if (newBlock.index % _adjustmentInterval == 0)
            {
                AdjustDifficulty();
            }

            return newBlock;
        }

        private void AdjustDifficulty()
        {
            var recentBlocks = Chain.Where(x => x.index > 0).TakeLast(_adjustmentInterval).ToList();
            if (recentBlocks.Count == 0) return;

            double avarageTime = recentBlocks.Average(x => (x.Timestamp - Chain[x.index - 1].Timestamp).TotalSeconds);
            if (avarageTime < _targetTimePerBlock) Difficulty++;
            else if (avarageTime > _targetTimePerBlock) Difficulty = Math.Max(1, Difficulty - 1);
        }

        public bool IsChainValid(List<Block> chainToValidate)
        {
            if (chainToValidate == null || chainToValidate.Count == 0) return false;

            for (int i = 1; i < chainToValidate.Count; i++)
            {
                var currentBlock = chainToValidate[i];
                var previousBlock = chainToValidate[i - 1];

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(currentBlock.Hash) || string.IsNullOrEmpty(previousBlock.Hash))
                {
                    return false;
                }

              
                if (currentBlock.Timestamp <= previousBlock.Timestamp)
                {
                    Console.WriteLine($"Блок #{currentBlock.index} має час з минулого.");
                    return false;
                }

             
                if (currentBlock.Timestamp > DateTime.UtcNow.AddHours(2))
                {
                    Console.WriteLine($" Блок #{currentBlock.index} занадто далеко в майбутньому.");
                    return false;
                }
        
            }
            return true;
        }

        public decimal GetBalance(string address, string tokenSymbol = "MAIN")
        {
            if (string.IsNullOrEmpty(address)) return 0;

            tokenSymbol = tokenSymbol?.ToUpper() ?? "MAIN";
            decimal balance = 0;

            foreach (var block in Chain)
            {
                foreach (var tx in block.Transactions)
                {

                    if (string.Equals(tx.From, "COINBASE", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(tx.To, address, StringComparison.OrdinalIgnoreCase) &&
                        tokenSymbol == "MAIN")
                    {
                        balance += tx.Amount;
                        continue;
                    }


                    if (string.Equals(tx.From, "MINT", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(tx.To, address, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(tx.TokenSymbol, tokenSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        balance += tx.Amount;
                        continue;
                    }


                    if (string.Equals(tx.TokenSymbol, tokenSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(tx.To, address, StringComparison.OrdinalIgnoreCase))
                        {
                            balance += tx.Amount;
                        }
                        if (string.Equals(tx.From, address, StringComparison.OrdinalIgnoreCase))
                        {
                            balance -= tx.Amount;
                        }
                    }


                    if (string.Equals(tx.From, address, StringComparison.OrdinalIgnoreCase) &&
                        tokenSymbol == "MAIN" &&
                        !string.Equals(tx.From, "COINBASE", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(tx.From, "MINT", StringComparison.OrdinalIgnoreCase))
                    {
                        balance -= tx.Fee;
                    }
                }
            }
            return balance;
        }
        public void AddTransaction(Transaction tx)
        {
            tx.TokenSymbol = tx.TokenSymbol?.ToUpper() ?? "MAIN";
            if (tx.From == "MINT")
            {
                if (tx.Amount <= 0) throw new Exception("Сума емісії повинна бути більшою за 0!");
                if (tx.TokenSymbol == "MAIN") throw new Exception("Заборонено вручну випускати базову монету MAIN!");

                PendingTransactions.Add(tx);
                return;
            }

            if (tx.Amount <= 0) throw new Exception("Сума переказу повинна бути більшою за 0");
            if (tx.Fee < 0) throw new Exception("Комісія не може бути від'ємною");
            decimal senderAssetBalance = GetBalance(tx.From, tx.TokenSymbol);
            if (senderAssetBalance < tx.Amount)
            {
                throw new Exception($"Недостатньо коштів токена {tx.TokenSymbol}! Баланс: {senderAssetBalance}");
            }

            decimal senderMainBalance = GetBalance(tx.From, "MAIN");


            decimal neededMain = (tx.TokenSymbol == "MAIN") ? (tx.Amount + tx.Fee) : tx.Fee;

            if (senderMainBalance < neededMain)
            {
                throw new Exception($"Недостатньо базової монети MAIN для оплати комісії! Потрібно: {neededMain}, Баланс: {senderMainBalance}");
            }

            PendingTransactions.Add(tx);
        }


        public List<string> GetUserTokens(string address)
        {
            var tokens = Chain.SelectMany(b => b.Transactions)
                              .Where(t => t.To == address || t.From == address)
                              .Select(t => t.TokenSymbol?.ToUpper())
                              .Concat(PendingTransactions.Where(t => t.To == address || t.From == address).Select(t => t.TokenSymbol?.ToUpper()))
                              .Where(t => !string.IsNullOrEmpty(t))
                              .Distinct()
                              .ToList();

            if (!tokens.Contains("MAIN")) tokens.Add("MAIN");
            return tokens;
        }
        public decimal GetPendingBalance(string address, string tokenSymbol = "MAIN")
        {
            if (string.IsNullOrEmpty(address)) return 0;

            tokenSymbol = tokenSymbol?.ToUpper() ?? "MAIN";
            decimal balance = GetBalance(address, tokenSymbol);

            foreach (var tx in PendingTransactions)
            {
                if (string.Equals(tx.TokenSymbol, tokenSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(tx.To, address, StringComparison.OrdinalIgnoreCase)) balance += tx.Amount;
                    if (string.Equals(tx.From, address, StringComparison.OrdinalIgnoreCase)) balance -= tx.Amount;
                }

                if (string.Equals(tx.From, address, StringComparison.OrdinalIgnoreCase) &&
                    tokenSymbol == "MAIN" &&
                    !string.Equals(tx.From, "MINT", StringComparison.OrdinalIgnoreCase))
                {
                    balance -= tx.Fee;
                }
            }

            return balance;
        }

        public int GetTransactionCinfirmations(string transactionId)
        {
            int confirmations = 0;
            foreach (var block in Chain)
            {
                if (block.Transactions.Any(t => t.Id == transactionId))
                {
                    confirmations = Chain.Count - block.index;
                    break;
                }
            }
            return confirmations;
        }

        public bool ResolveConsensus(List<Block> fChain)
        {
            if (!IsChainValid(fChain)) return false;

            if (fChain.Count > this.Chain.Count)
            {
                int forkPointIndex = -1;
                for (int i = 0; i < Chain.Count; i++)
                {
                    if (i >= fChain.Count || Chain[i].Hash != fChain[i].Hash)
                    {
                        forkPointIndex = i - 1;
                        break;
                    }
                }

                if (forkPointIndex == -1)
                    forkPointIndex = Chain.Count - 1;

                int reorgDepth = Chain.Count - 1 - forkPointIndex;

                if (reorgDepth > MaxReorgDepth)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nСпроба глибокої реорганізації. Глибина: {reorgDepth} (Макс: {MaxReorgDepth}). Локальні блоки вже фіналізовані.");
                    Console.ResetColor();
                    return false;
                }


                this.Chain = new List<Block>(fChain);
                _fileServices.SaveChain(Chain);
                return true;
            }
            return false;
        }

        public void PrintDifficultyHistory()
        {
            Console.WriteLine("Difficulty History:\n=================================================");
            foreach (var block in Chain)
            {
                Console.WriteLine($"Block Index: {block.index}, Difficulty: {block.Difficulty}");
            }
            Console.WriteLine("=================================================");
        }

        public bool TryAddBlockFromPeer(Block block)
        {
            var lastBlock = Chain.Last();

            if (block.PreviousHash != lastBlock.Hash)
            {
                return false;
            }
            if (block.Hash != _hashingService.ComputeHash(block))
            {
                return false;
            }
            if (!block.Hash.StartsWith(new string('0', block.Difficulty), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            foreach (var transaction in block.Transactions)
            {
                if (!_transactionServices.ValidateTransaction(transaction).isValid)
                {
                    return false;
                }
            }

            Chain.Add(block);

            foreach (var transaction in block.Transactions)
            {
                PendingTransactions.Remove(transaction);
            }

            _fileServices.SaveChain(Chain);

            return true;
        }
    }
}