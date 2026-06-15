using System;
using System.Collections.Generic;
using System.Text;

namespace BlockСh.Models
{
    public class Transaction
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Fee { get; set; }
        public byte[] SenderPublicKey { get; set; }

        public string ReplacesTxId { get; set; }
        public byte[] Signature { get; set; }

        public string TokenSymbol { get; set; } = "MAIN";

        public int Size
        {
            get
            {
                return GeDataToString().Length;
            }
        }


        public Transaction()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
            TokenSymbol = "MAIN";
        }

        public Transaction(string from, string to, decimal amount, decimal fee)
        {
            Id = Guid.NewGuid().ToString();
            From = from;
            To = to;
            Amount = amount;
            Timestamp = DateTime.UtcNow;
            Fee = fee;
            TokenSymbol = "MAIN";
        }

        public Transaction(string from, string to, decimal amount, decimal fee, string tokenSymbol)
            : this(from, to, amount, fee)
        {
            TokenSymbol = string.IsNullOrEmpty(tokenSymbol) ? "MAIN" : tokenSymbol.ToUpper();
        }


        public byte[] GeDataToString()
        {

            var dataString = $"{From}:{To}:{Amount}:{Timestamp:o}:{Fee}:{ReplacesTxId}:{TokenSymbol}";
            return Encoding.UTF8.GetBytes(dataString);
        }


        public string ToRowString()
        {
            return $"{Id}\t{From}\t{To}\t{Amount}\t{Timestamp}\t{TokenSymbol}";
        }

        public override string ToString()
        {
            return $"Transaction ID: {Id}\nFrom: {From}\nTo: {To}\nAmount: {Amount} {TokenSymbol}\nFee: {Fee} MAIN\nTimestamp: {Timestamp}";
        }
    }
}