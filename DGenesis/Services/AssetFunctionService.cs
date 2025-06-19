using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DGenesis.Services
{
    public class AssetFunctionService
    {
        // Cache pour la recherche inversée (rapide)
        private static readonly Dictionary<string, Dictionary<string, string>> _functionCache = new Dictionary<string, Dictionary<string, string>>();
        // CORRECTION : Ajout d'une variable pour stocker la base de données originale
        private static readonly Dictionary<string, Dictionary<string, List<string>>> _functionDatabase = new Dictionary<string, Dictionary<string, List<string>>>();

        static AssetFunctionService()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "asset_function.json");
                if (File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var database = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(jsonString, options);

                    // On stocke la base de données pour la nouvelle méthode
                    _functionDatabase = database;

                    // On construit le cache inversé pour l'ancienne méthode
                    foreach (var gameEntry in database)
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
            return null;
        }

        // CORRECTION : Ajout de la méthode manquante
        public IEnumerable<string> GetAssetsForFunction(string game, string functionName)
        {
            if (_functionDatabase.TryGetValue(game, out var functions) &&
                functions.TryGetValue(functionName, out var assetList))
            {
                return assetList;
            }
            return Enumerable.Empty<string>();
        }
    }
}