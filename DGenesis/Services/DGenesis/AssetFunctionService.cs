using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DGenesis.Services
{
    public class FunctionData
    {
        public List<string> Textures { get; set; } = new List<string>();
        public List<string> Flats { get; set; } = new List<string>();
        public List<int> Things { get; set; } = new List<int>();
    }

    public class AssetFunctionService
    {
        private readonly Dictionary<string, Dictionary<string, FunctionData>> _functionDatabase;
        private readonly Dictionary<string, Dictionary<string, string>> _functionCache;

        public AssetFunctionService()
        {
            _functionDatabase = new Dictionary<string, Dictionary<string, FunctionData>>();
            _functionCache = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "asset_function.json");
                if (File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _functionDatabase = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, FunctionData>>>(jsonString, options);

                    BuildFunctionCache();
                }
            }
            catch (Exception ex) { Console.WriteLine($"Erreur lors du chargement de asset_function.json: {ex.Message}"); }
        }

        private void BuildFunctionCache()
        {
            foreach (var gameEntry in _functionDatabase)
            {
                var gameName = gameEntry.Key;
                var assetToFunctionMap = new Dictionary<string, string>();
                foreach (var functionEntry in gameEntry.Value)
                {
                    var functionName = functionEntry.Key;
                    functionEntry.Value.Textures?.ForEach(asset => assetToFunctionMap[asset] = functionName);
                    functionEntry.Value.Flats?.ForEach(asset => assetToFunctionMap[asset] = functionName);
                }
                _functionCache[gameName] = assetToFunctionMap;
            }
        }

        public string GetAssetFunction(string game, string assetName)
        {
            if (_functionCache.TryGetValue(game, out var assetToFunctionMap) && assetToFunctionMap.TryGetValue(assetName, out var function))
            {
                return function;
            }
            return null;
        }

        public FunctionData GetFunctionData(string game, string functionName)
        {
            if (_functionDatabase.TryGetValue(game, out var functions) && functions.TryGetValue(functionName, out var functionData))
            {
                return functionData;
            }
            return new FunctionData();
        }
    }
}