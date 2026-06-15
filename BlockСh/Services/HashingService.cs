using BlockСh.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
namespace BlockСh.Services
{
    public class MerkleProofElement
    {
        public string Hash { get; set; }
        public bool LeftNeighbor { get; set; } 
        public MerkleProofElement(string hash, bool leftNeighbor)
        {
            Hash = hash;
            LeftNeighbor = leftNeighbor;
        }
    }
    public class HashingService
    {
        public string ComputeHash(Block block)
        {
            var tree = BuildMerkleTree(block.Transactions);
            block.MerkleRoot = tree.Count > 0 ? tree.Last().FirstOrDefault() : string.Empty;

            var inputString = $"{block.index}{block.Timestamp}{block.MerkleRoot}{block.PreviousHash}{block.Author}{block.Nonce}";
            return ComputeSha256(inputString);

        }

        public string ComputeSha256(string data)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToHexString(hashBytes);
            }
        }

        public List<List<string>> BuildMerkleTree(List<Transaction> transactions)
        {
            var tree = new List<List<string>>();
            if (transactions == null || transactions.Count == 0) return tree;

           
            List<string> currentLevel = transactions.Select(t => ComputeSha256(t.ToRowString())).ToList();
            tree.Add(currentLevel);

            while (currentLevel.Count > 1)
            {
                if (currentLevel.Count % 2 != 0)
                {
                    currentLevel.Add(currentLevel.Last());
                }

                var nextLevel = new List<string>();
                for (int i = 0; i < currentLevel.Count; i += 2)
                {
                    string combinedHash = ComputeSha256(currentLevel[i] + currentLevel[i + 1]);
                    nextLevel.Add(combinedHash);
                }

                tree.Add(nextLevel);
                currentLevel = nextLevel;
            }

            return tree;
        }
        public List<MerkleProofElement> GetMerkleProof(List<List<string>> tree, string txHash)
        {
            var proof = new List<MerkleProofElement>();
            if (tree == null || tree.Count == 0) return proof;

            int index = tree[0].IndexOf(txHash);
            if (index == -1)
            {
                throw new Exception("Транзакцію не знайдено");
            }

            for (int level = 0; level < tree.Count - 1; level++)
            {
                var currentLevelHashes = tree[level];
                int neighborIndex;
                bool leftNeighbor;

                if (index % 2 == 0)
                {
                    neighborIndex = index + 1;
                    if (neighborIndex >= currentLevelHashes.Count) neighborIndex = index;
                    leftNeighbor = false; 
                }
                else
                {
                    neighborIndex = index - 1;
                    leftNeighbor = true; 
                }

                proof.Add(new MerkleProofElement(currentLevelHashes[neighborIndex], leftNeighbor));
                index /= 2;
            }

            return proof;
        }

        // =================================================================
        public bool MerkleProof(string txHash, List<MerkleProofElement> proof, string MerkleRoot)
        {
            if (string.IsNullOrEmpty(MerkleRoot)) return false;

            string currentHash = txHash;
            foreach (var element in proof)
            {
         
                if (element.LeftNeighbor)
                {
                   
                    currentHash = ComputeSha256(element.Hash + currentHash);
                }
                else
                {
                    currentHash = ComputeSha256(currentHash + element.Hash);
                }
            }
            return currentHash == MerkleRoot;
        }
    }
}
