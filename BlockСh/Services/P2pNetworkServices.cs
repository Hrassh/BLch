using BlockСh.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlockСh.Services
{
    public class P2pNetworkServices
    {
        private readonly int _port;
        private readonly List<PeerInfo> _poers;
        private readonly BlockChainServices _blockChainServices;

        private readonly ConcurrentDictionary<string, int> _peerStrikes = new ConcurrentDictionary<string, int>();

        private const int MaxStrikes = 3;
        // =================================================================

        public P2pNetworkServices(int port, List<PeerInfo> poers, BlockChainServices blockChainServices)
        {
            _port = port;
            _poers = poers;
            _blockChainServices = blockChainServices;
        }

        public void Start()
        {
            Task.Run(StartSeverAsync);
        }

        private async Task StartSeverAsync()
        {
            var listener = new TcpListener(System.Net.IPAddress.Any, _port);
            listener.Start();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n[P2P Сеть] Нода успішно запущена! Слушаю порт: {_port}");
            Console.ResetColor();

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandlePeerAsync(client));
            }
        }

      
        private async Task HandlePeerAsync(TcpClient client)
        {
            var remoteEndPoint = (System.Net.IPEndPoint)client.Client.RemoteEndPoint;
            string peerIp = remoteEndPoint.Address.ToString();

            if (_peerStrikes.TryGetValue(peerIp, out int strikes) && strikes >= MaxStrikes)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[Firewall] Заблоковано пакет від шкідливого піра: {peerIp}");
                Console.ResetColor();

                client.Close();
                return;
            }

            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[Узел {_port}] Нове підключення від піра: {peerIp}:{remoteEndPoint.Port}");
                Console.ResetColor();

                var json = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(json))
                {
                    AddStrike(peerIp, 1, "Error");
                    return;
                }

                try
                {
                    var p2pMessage = JsonSerializer.Deserialize<P2PMessage>(json);
                    if (p2pMessage != null)
                    {
                        await ComandExeuter(p2pMessage, peerIp);
                    }
                    else
                    {
        
                        AddStrike(peerIp, 1, "Невалідний формат повідомлення");
                    }
                }
                catch (JsonException)
                {
                  
                    AddStrike(peerIp, 1, "Порушення Зламаний JSON / JsonException");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Узел {_port}] Помилка при обробці піра {remoteEndPoint.Port}: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine($"[Узел {_port}] Пір з порта {remoteEndPoint.Port} відключився.");
            }
        }
        private async Task ComandExeuter(P2PMessage message, string peerIp)
        {
            try
            {
                if (message.Type == "NEW_BLOCK")
                {
                    var incomingBlock = JsonSerializer.Deserialize<Block>(message.Data);
                    if (incomingBlock != null)
                    {
                   
                        bool success = _blockChainServices.TryAddBlockFromPeer(incomingBlock);

                        if (success)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n[Узел {_port} <- СЕТЬ] УСПЕХ! Отримано валідний блок #{incomingBlock.index} від {peerIp}.");
                            Console.ResetColor();
                        }
                        else
                        {
                            
                            AddStrike(peerIp, 2, $"Порушення класу Б (Криптографічна атака / Фальшивий блок #{incomingBlock.index})");
                        }
                    }
                }
            }
            catch (JsonException)
            {
                AddStrike(peerIp, 1, "Порушення класу А (Зламаний JSON всередині поля Data)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Узел {_port}] Помилка виконання команди: {ex.Message}");
            }
        }


        private void AddStrike(string peerIp, int amount, string reason)
        {
     
            int currentStrikes = _peerStrikes.AddOrUpdate(peerIp, amount, (key, oldValue) => oldValue + amount);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ФАЙРВОЛ ⚠️] Пір {peerIp} отримав штраф! Причина: {reason}.");
            Console.WriteLine($"[ФАЙРВОЛ ⚠️] Нараховано балів: +{amount}. Усього страйків у цього IP: {currentStrikes} із {MaxStrikes}.");

            if (currentStrikes >= MaxStrikes)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[ФАЙРВОЛ ❌] IP {peerIp} перевищив ліміт порушень і занесений до ЧОРНОГО СПИСКУ!");
            }
            Console.ResetColor();
        }

        public async Task BroadcastBlockAsync(Block block)
        {
            var message = new P2PMessage("NEW_BLOCK", JsonSerializer.Serialize(block));
            var json = JsonSerializer.Serialize(message);

            foreach (var peer in _poers)
            {
               
                await SendMessageAsync(peer, json);
            }
        }
        private async Task SendMessageAsync(PeerInfo peer, string message)
        {
            try
            {
                using var client = new TcpClient(peer.Host, peer.Port);
                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                await writer.WriteLineAsync(message);

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[Вузел {_port} -> СЕТЬ] Блок успішно отправлен соседу на порт {peer.Port}.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[Вузел {_port} -> СЕТЬ] Не удалось відправити повідомлення на порт {peer.Port} .");
                Console.ResetColor();
            }
        }
        public Dictionary<string, int> GetBlacklist()
        {
            return new Dictionary<string, int>(_peerStrikes);
        }
    }
}