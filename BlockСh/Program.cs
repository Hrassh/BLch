using BlockCh.Services;
using BlockСh.Models;
using BlockСh.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var blockChain = new BlockChainServices();
var explorer = new BlockchainExplorerService(blockChain);
var walletServices = new WalletServices();
var keystore = new WalletKeystoreService();

Wallet userWallet = null;
string walletName = "";

Console.WriteLine("=============================================");
Console.WriteLine("=== ЛАСКАВО ПРОСИМО ДО BLOCKCHAIN WALLET ===");
Console.WriteLine("=============================================");
Console.WriteLine("1. Створити новий гаманець");
Console.WriteLine("2. Завантажити існуючий гаманець (з диска)");
Console.Write("\nВиберіть дію (1 або 2): ");
var startChoice = Console.ReadLine();

Console.Write("Введіть ім'я гаманця (наприклад, Alice або Bob): ");
walletName = Console.ReadLine();

Console.Write("Введіть пароль для захисту приватного ключа: ");
string password = Console.ReadLine();

if (startChoice == "1")
{
    userWallet = walletServices.CreatWall(walletName);

    keystore.SaveWallet(userWallet, password);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n[УСПІХ] Гаманець успішно створено та зашифровано у файл wallet_{walletName}.json!");
    Console.ResetColor();
}
else
{
    try
    {
        userWallet = keystore.LoadWallet(walletName, password);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[УСПІХ] Гаманець завантажено! Адреса: {userWallet.Address}");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[ПОМИЛКА АВТОРИЗАЦІЇ]: {ex.Message}");
        Console.ResetColor();
        Console.WriteLine("Натисніть Enter для виходу з програми...");
        Console.ReadLine();
        return;
    }
}

Console.WriteLine("\nНатисніть Enter, щоб налаштувати мережу...");
Console.ReadLine();
Console.Clear();

Console.WriteLine("=== НАЛАШТУВАННЯ МЕРЕЖЕВОГО ВУЗЛА (P2P) ===");
Console.Write("Введіть свій локальний порт P2P (наприклад, 8001): ");
int myport = int.Parse(Console.ReadLine() ?? "8001");

Console.Write("Введіть порт сусіда для синхронізації (наприклад, 8002): ");
int nodeport = int.Parse(Console.ReadLine() ?? "8002");

var p2pNetworkService = new P2pNetworkServices(myport, new List<PeerInfo> { new PeerInfo("localhost", nodeport) }, blockChain);
p2pNetworkService.Start();

Console.Clear();

// ==========================================
while (true)
{
    Console.WriteLine("====================================================================");
    Console.WriteLine($"[КОРИСТУВАЧ]: {walletName} | [АДРЕСА]: {userWallet.Address}");

    Console.WriteLine("[ВАШІ АКТИВНІ БАЛАНСИ В МЕРЕЖІ]:");
    var userTokens = blockChain.GetUserTokens(userWallet.Address);
    foreach (var token in userTokens)
    {
        decimal confirmed = blockChain.GetBalance(userWallet.Address, token);
        decimal pending = blockChain.GetPendingBalance(userWallet.Address, token);

        Console.Write($"    -> {token}: {confirmed}");
        if (pending != confirmed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($" (В мемпулі: {pending})");
            Console.ResetColor();
        }
        Console.WriteLine();
    }
    Console.WriteLine("====================================================================");

    Console.WriteLine("Blockchain Головне Меню:");
    Console.WriteLine("1. Надіслати переказ (MAIN або кастомні токени)");
    Console.WriteLine("2. Випустити власний токен (Mint / Емісія)");
    Console.WriteLine("3. Запустити майнінг транзакцій (Mine Pending Transactions)");
    Console.WriteLine("4. Переглянути історію гаманця (Blockchain Explorer)");
    Console.WriteLine("5. Знайти блок за Transaction ID");
    Console.WriteLine("6. Симуляція хакерської Атаки 51% (Тестування)");
    Console.WriteLine("7. Вихід з програми");
    Console.Write("\nВиберіть опцію (1-7): ");

    var choice = Console.ReadLine();
    Console.Clear();

    switch (choice)
    {
        case "1":
            Console.WriteLine("=== НАДСИЛАННЯ ТРАНЗАКЦІЇ ===");
            Console.Write("Введіть адресу отримувача: ");
            string toAddress = Console.ReadLine();

            Console.Write("Введіть символ токена (наприклад MAIN, USDT, COIN): ");
            string tokenSym = Console.ReadLine().ToUpper();

            Console.Write("Введіть сумму переказу: ");
            decimal amount = decimal.Parse(Console.ReadLine() ?? "0");

            Console.Write("Введіть комісію майнеру (сплачується виключно в MAIN): ");
            decimal fee = decimal.Parse(Console.ReadLine() ?? "0");

            try
            {
                var tx = new Transaction(userWallet.Address, toAddress, amount, fee, tokenSym);

                tx.SenderPublicKey = userWallet.PublicKey;
                tx.Signature = userWallet.Sign(tx.GeDataToString());

                blockChain.AddTransaction(tx);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[УСПІХ] Транзакцію успішно підписано та трансльовано в мемпул!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ВІДХИЛЕНО СИСТЕМОЮ ЗАХИСТУ]: {ex.Message}");
                Console.ResetColor();
            }
            Console.WriteLine("\nНатисніть Enter для повернення в меню...");
            Console.ReadLine();
            break;

        case "2":
            Console.WriteLine("=== ЕМІСІЯ НОВОГО АКТИВУ (MINT) ===");
            Console.Write("Введіть назву (символ) нового токена (наприклад, ITSTEP, GAME): ");
            string newToken = Console.ReadLine().ToUpper();

            Console.Write("Скільки монет згенерувати на ваш баланс: ");
            decimal mintAmount = decimal.Parse(Console.ReadLine() ?? "0");

            try
            {
                var mintTx = new Transaction("MINT", userWallet.Address, mintAmount, 0, newToken);

                blockChain.AddTransaction(mintTx);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[УСПІХ] Заявку на випуск {mintAmount} {newToken} додано в пул незатверджених транзакцій!");
                Console.WriteLine("Щоб токени остаточно зарахувалися на баланс, запустіть майнінг (Пункт 3).");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ПОМИЛКА ЕМІСІЇ]: {ex.Message}");
                Console.ResetColor();
            }
            Console.WriteLine("\nНатисніть Enter для повернення в меню...");
            Console.ReadLine();
            break;

        case "3":
            Console.WriteLine("=== ЗАПУСК ОБЧИСЛЕННЯ ХЕШУ БЛОКУ (Proof of Work) ===");
            Console.WriteLine("Система збирає найвигідніші транзакції за рівнем Fee...");

            blockChain.MinePendingTransactions(userWallet.Address);

            Console.WriteLine("\nНатисніть Enter для повернення в меню...");
            Console.ReadLine();
            break;

        case "4":
            Console.WriteLine("=== АНАЛІТИКА: ПОВНА ІСТОРІЯ ТРАНЗАКЦІЙ ЧЕРЕЗ LINQ ===");
            var history = explorer.GetTransactionHistory(userWallet.Address);

            if (history.Count == 0)
            {
                Console.WriteLine("Транзакцій по цій адресі в блокчейні ще не зафіксовано.");
            }
            else
            {
                Console.WriteLine($"Знайдено {history.Count} транзакцій (від найновіших):");
                Console.WriteLine("--------------------------------------------------------------------");
                foreach (var hTx in history)
                {
                    string direction = "";

                    if (string.Equals(hTx.From, "COINBASE", StringComparison.OrdinalIgnoreCase))
                        direction = "НАГОРОДА МАЙНЕРА ⛏️";
                    else if (string.Equals(hTx.From, "MINT", StringComparison.OrdinalIgnoreCase))
                        direction = "ЕМІСІЯ 🪙";
                    else if (string.Equals(hTx.To, userWallet.Address, StringComparison.OrdinalIgnoreCase))
                        direction = "ВХІДНА ↙️";
                    else
                        direction = "ВИХІДНА ↗️";

                    Console.WriteLine($"[{direction}] ID: {hTx.Id.Substring(0, 8)}... | {hTx.Amount} {hTx.TokenSymbol} | Комісія: {hTx.Fee} MAIN | Час: {hTx.Timestamp.ToLocalTime()}");
                }
                Console.WriteLine("--------------------------------------------------------------------");
            }
            Console.WriteLine("\nНатисніть Enter для повернення в меню...");
            Console.ReadLine();
            break;

        case "5":
            Console.WriteLine("=== АНАЛІТИКА: ПОШУК БЛОКУ ЗА TRANSACTION ID ===");
            Console.Write("Введіть повний унікальний TxID для пошуку: ");
            string searchId = Console.ReadLine();

            var targetBlock = explorer.FindBlockByTransactionId(searchId);

            if (targetBlock != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[УСПІХ] Транзакцію знайдено!");
                Console.ResetColor();
                Console.WriteLine($"Вона успішно запакована в Блок №{targetBlock.index}");
                Console.WriteLine($"Хеш цього блоку в ланцюгу: {targetBlock.Hash}");
                Console.WriteLine($"Попередній хеш (PreviousHash): {targetBlock.PreviousHash}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[ІНФО] Транзакцію не знайдено в замайнених блоках.");
                Console.WriteLine("Можливо, вона ще знаходиться в Мемпулі й очікує підтвердження майнерами.");
                Console.ResetColor();
            }
            Console.WriteLine("\nНатисніть Enter для повернення в меню...");
            Console.ReadLine();
            break;

        case "6": 
            Console.WriteLine("=== СИМУЛЯЦІЯ АТАКИ 51% (ФІНАЛІЗАЦІЯ БЛОКІВ) ===");

            var genesisBlock = new Block(0, DateTime.UtcNow, new List<Transaction>(), "Genesis Data", "GENESIS_HASH", "0", "System");

            var nodeA = new BlockChainServices();
            nodeA.Chain = new List<Block> { genesisBlock };

            var nodeB = new BlockChainServices();
            nodeB.Chain = new List<Block> { genesisBlock };

            Console.WriteLine("\n[1] Чесна нода NodeA майнить 6 нових блоків (Index 1..6)...");
            string lastHashA = genesisBlock.Hash;
            for (int i = 1; i <= 6; i++)
            {
                var block = new Block(i, DateTime.UtcNow.AddMinutes(i), new List<Transaction>(), $"Block {i} Data", "HASH_A_" + i, lastHashA, "MinerA");
                nodeA.Chain.Add(block);
                lastHashA = block.Hash;
            }
            Console.WriteLine($"Поточна довжина ланцюга NodeA: {nodeA.Chain.Count} блоків.");

            Console.WriteLine("\n[2] Хакерська нода NodeB відгалужується від генезису і таємно майнить 8 блоків (Index 1..8)...");
            string lastHashB = genesisBlock.Hash;
            for (int i = 1; i <= 8; i++)
            {
                var block = new Block(i, DateTime.UtcNow.AddMinutes(i), new List<Transaction>(), $"Hacker Block {i} Data", "HASH_B_" + i, lastHashB, "HackerB");
                nodeB.Chain.Add(block);
                lastHashB = block.Hash;
            }
            Console.WriteLine($"Поточна довжина ланцюга хакера NodeB: {nodeB.Chain.Count} блоків (Ланцюг довший).");

            Console.WriteLine("\n[3] Хакер ініціює консенсус: NodeA.ResolveConsensus(NodeB.Chain)...");
            bool isAttackSuccessful = nodeA.ResolveConsensus(nodeB.Chain);

            Console.WriteLine("\n--------------------------------------------------------------------");
            if (!isAttackSuccessful)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[РЕЗУЛЬТАТ ТЕСТУ: УСПІХ] Захист MaxReorgDepth спрацював! Атаку 51% успішно відбито.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[РЕЗУЛЬТАТ ТЕСТУ: ПРОВАЛ] Мережа прийняла довший ланцюг і переписала історію.");
                Console.ResetColor();
            }
            Console.WriteLine("--------------------------------------------------------------------");


            Console.WriteLine("\n=== ВІЗУАЛЬНИЙ СТАН ЛАНЦЮГІВ ПІСЛЯ АТАКЫ ===");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n ЛАНЦЮГ ЧЕСНОЇ НОДИ (NodeA) — ЗАХИЩЕНИЙ:");
            Console.ResetColor();
            foreach (var b in nodeA.Chain)
            {
                Console.Write($"[Блок #{b.index} | Автор: {b.Author} | Hash: {b.Hash.Substring(0, Math.Min(10, b.Hash.Length))}...] ---> ");
            }
            Console.WriteLine("КІНЕЦЬ");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n ЛАНЦЮГ ХАКЕРА (NodeB) — ВІДХИЛЕНИЙ:");
            Console.ResetColor();
            foreach (var b in nodeB.Chain)
            {
                Console.Write($"[Блок #{b.index} | Автор: {b.Author} | Hash: {b.Hash.Substring(0, Math.Min(10, b.Hash.Length))}...] ---> ");
            }
            Console.WriteLine("КІНЕЦЬ");
            Console.WriteLine("====================================================================");


            Console.WriteLine("\nНатисніть Enter для повернення в меню...");
            Console.ReadLine();
            break;

        case "7":
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Завершення сесії вузла блокчейну. Збереження даних... Бувайте!");
            Console.ResetColor();
            return;

        default:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Некоректний вибір. Спробуйте ще раз.");
            Console.ResetColor();
            System.Threading.Thread.Sleep(1000);
            break;
    }
    Console.Clear();
}