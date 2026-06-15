using System;
using System.Collections.Generic;
using System.Text;

namespace BlockСh.Models
{
    public class PeerInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }

        public PeerInfo(string host, int port)
        {
            Host = host;
            Port = port;
        }
    }
}