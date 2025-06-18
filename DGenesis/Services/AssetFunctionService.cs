using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DGenesis.Services
{
    public class AssetFunctionService
    {
        // Cache inversé pour des recherches rapides: Map<game, Map<assetName, functionName>>
        private static readonly Dictionary<string, Dictionary<string, string>> _functionCache = new Dictionary<string, Dictionary<string, string>>();

        static AssetFunctionService()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "asset_function.json");
                if (File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var functionDatabase = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(jsonString, options);

                    // Construire le cache inversé
                    foreach (var gameEntry in functionDatabase)
                    {
                        var gameName = gameEntry.Key;
                        var assetToFunctionMap = new Dictionary<string, string>();
                        foreach (var functionEntry in gameEntry.Value)
                        {
                            var functionName = functionEntry.Key;
                            foreach (var assetName in functionEntry.Value)
                            {
                                assetToFunctionMap[assetName] = functionName;
                            }
                        }
                        _functionCache[gameName] = assetToFunctionMap;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement de asset_function.json: {ex.Message}");
            }
        }

        public string GetAssetFunction(string game, string assetName)
        {
            if (_functionCache.TryGetValue(game, out var assetToFunctionMap) &&
                assetToFunctionMap.TryGetValue(assetName, out var function))
            {
                return function;
            }
            return null; // Retourne null si aucune fonction spécifique n'est trouvée
        }
    }
}