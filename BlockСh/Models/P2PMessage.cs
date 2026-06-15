using System;
using System.Collections.Generic;
using System.Text;

namespace BlockСh.Models
{
    public class P2PMessage
    {
        public string Type { get; set; }
        public string Data { get; set; }
        public P2PMessage(string type, string data)
        {
            Type = type;
            Data = data;
        }
    }
}
