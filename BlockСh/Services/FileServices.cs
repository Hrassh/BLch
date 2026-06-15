using BlockСh.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace BlockСh.Services
{
    public class FileService
    {
        private readonly string _chainFilePath = "blockchain.json"; 

        public void SaveChain(List<Block> chain)
        {
            var json = JsonSerializer.Serialize(chain); 
            File.WriteAllText(_chainFilePath, json); 
        }

        public List<Block> LoadChain()
        {
            if (File.Exists(_chainFilePath))
            {
                var json = File.ReadAllText(_chainFilePath);

   
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true 
                };


                var chain = JsonSerializer.Deserialize<List<Block>>(json, options);

                if (chain != null)
                {
                    return chain;
                }
            }

            return new List<Block>();
        }
    }
}
