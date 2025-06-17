using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DGenesis.Services
{
    public class AssetTagService
    {
        // CHANGEMENT 1: Le type de la base de données est maintenant directement un dictionnaire.
        private readonly Dictionary<string, GameTags> _tagDatabase;

        public AssetTagService()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "asset_tags.json");
                if (File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // CHANGEMENT 2: On désérialise directement en dictionnaire.
                    _tagDatabase = JsonSerializer.Deserialize<Dictionary<string, GameTags>>(jsonString, options);
                }
                else
                {
                    _tagDatabase = new Dictionary<string, GameTags>();
                }
            }
            catch (Exception ex)
            {
                // L'exception JsonException sera beaucoup plus claire maintenant si le JSON est mal formé.
                Console.WriteLine($"Erreur lors du chargement de asset_tags.json: {ex.Message}");
                _tagDatabase = new Dictionary<string, GameTags>();
            }
        }

        public TaggedAssetCollection GetAssetsForTag(string game, string tag)
        {
            // CHANGEMENT 3: L'accès au dictionnaire est maintenant direct.
            if (_tagDatabase.TryGetValue(game, out var gameTags) &&
                gameTags.Tags.TryGetValue(tag, out var assetCollection))
            {
                return assetCollection;
            }
            return new TaggedAssetCollection(); // Retourne une collection vide si non trouvé
        }

        public List<string> GetAvailableThemeTags(string game)
        {
            // CHANGEMENT 4: L'accès au dictionnaire est maintenant direct ici aussi.
            if (_tagDatabase.TryGetValue(game, out var gameTags))
            {
                var functionalAndAttributeTags = new HashSet<string>
                {
                    "door", "switch", "light_source", "key_indicator", "exit", "secret", "sky",
                    "support", "panel", "animated", "grate", "window", "border", "signage",
                    "monster_prop", "gore", "corrupted", "decayed", "puzzle_item",
                    "class_specific", "tapestry", "glass_window"
                };

                return gameTags.Tags
                    .Where(t => !functionalAndAttributeTags.Contains(t.Key))
                    .Select(t => t.Key)
                    .ToList();
            }
            return new List<string>();
        }
    }
}