using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BlockСh.Models
{
    public class Block
    {

        public int index { get; set; }
        public DateTime Timestamp { get; set; }
        public List<Transaction> Transactions { get; set; }
        public string Hash { get; set; }
        public string PreviousHash { get; set; }
        public long Nonce { get; set; }
        public string MerkleRoot { get; set; }
        public string Author { get; set; }
        public int Difficulty { get; set; }
        public string Data { get; set; }

       
        [JsonConstructor]
        public Block()
        {
        }

       
        public Block(int index, DateTime timestap, List<Transaction> transactions, string data, string hash, string previousHash, string author)
        {
            this.index = index;
            this.Timestamp = timestap;
            this.Transactions = transactions;
            this.Data = data;
            this.Hash = hash;
            this.PreviousHash = previousHash;
            this.Author = author;
            this.MerkleRoot = "";
        }
    }
}