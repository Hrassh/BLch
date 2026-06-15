using BlockСh.Models;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockСh.Services
{
    public class MiningServices
    {
        private readonly object _lockObject = new object();

        public void MineBlock(Models.Block block, int difficulty)
        {
            bool isMined = false;

            var txBuilder = new StringBuilder();
            if (block.Transactions != null)
            {
                foreach (var tx in block.Transactions)
                {
                    txBuilder.Append(tx.ToRowString());
                }
            }
            string staticPart = $"{block.index}{txBuilder}{block.Timestamp}{block.PreviousHash}{block.Author}";
            byte[] staticBytes = Encoding.UTF8.GetBytes(staticPart);


            int threadCount = Environment.ProcessorCount;
            Task[] miningTasks = new Task[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t; 

                miningTasks[t] = Task.Run(() =>
                {

                    byte[] localBuffer = new byte[staticBytes.Length + 4];

                    // Копіюємо незмінний префікс на початок буфера
                    Buffer.BlockCopy(staticBytes, 0, localBuffer, 0, staticBytes.Length);

                    // Створюємо один масив на 32 байти під хеш
                    byte[] hashBytes = new byte[32];


                    for (int i = threadId; i < int.MaxValue; i += threadCount)
                    {
                        if (isMined) break;
                        BitConverter.TryWriteBytes(new Span<byte>(localBuffer, staticBytes.Length, 4), i);


                        SHA256.HashData(localBuffer, hashBytes);

                        // -----------------------------------------------------------------
                        if (CheckDiffBytes(hashBytes, difficulty))
                        {
                            lock (_lockObject)
                            {
                                if (!isMined)
                                {
                                    isMined = true;
                                    block.Nonce = i;
                                    block.Hash = Convert.ToHexString(hashBytes); 
                                }
                            }
                            break;
                        }
                    }
                });
            }

            Task.WaitAll(miningTasks);
        }

       
        private bool CheckDiffBytes(byte[] hash, int difficulty)
        {
            int zeroBytesNeeded = difficulty / 2;

            for (int i = 0; i < zeroBytesNeeded; i++)
            {
                if (hash[i] != 0) return false;
            }

            if (difficulty % 2 != 0)
            {
                if (hash[zeroBytesNeeded] >= 0x10) return false;
            }

            return true;
        }
    }

    
}