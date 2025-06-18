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

                    _tagDatabase = JsonSerializer.Deserialize<Dictionary<string, GameTags>>(jsonString, options);
                }
                else
                {
                    _tagDatabase = new Dictionary<string, GameTags>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement de asset_tags.json: {ex.Message}");
                _tagDatabase = new Dictionary<string, GameTags>();
            }
        }

        public TaggedAssetCollection GetAssetsForTag(string game, string tag)
        {
            if (_tagDatabase.TryGetValue(game, out var gameTags) &&
                gameTags.Tags.TryGetValue(tag, out var assetCollection))
            {
                return assetCollection;
            }
            return new TaggedAssetCollection();
        }

        public List<string> GetAvailableThemeTags(string game)
        {
            if (_tagDatabase.TryGetValue(game, out var gameTags))
            {
                var functionalAndAttributeTags = new HashSet<string>
                {
                    "door", "switch", "light_source", "exit", "secret", "sky",
                    "support", "panel", "animated", "grate", "window", "border", "signage",
                    "monster_prop", "gore", "corrupted", "decayed", "puzzle_item",
                    "class_specific", "tapestry", "glass_window",
                    "key_indicator_blue", "key_indicator_red", "key_indicator_yellow", "key_indicator_green",
                    "monster"
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