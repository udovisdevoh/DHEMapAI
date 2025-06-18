using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DGenesis.Services
{
    public class AssetThemeService
    {
        private readonly Dictionary<string, GameThemes> _themeDatabase;

        public AssetThemeService()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "asset_themes.json");
                if (File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    _themeDatabase = JsonSerializer.Deserialize<Dictionary<string, GameThemes>>(jsonString, options);
                }
                else
                {
                    _themeDatabase = new Dictionary<string, GameThemes>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement de asset_themes.json: {ex.Message}");
                _themeDatabase = new Dictionary<string, GameThemes>();
            }
        }

        public ThemedAssetCollection GetAssetsForTheme(string game, string theme)
        {
            if (_themeDatabase.TryGetValue(game, out var gameThemes) &&
                gameThemes.Themes.TryGetValue(theme, out var assetCollection))
            {
                return assetCollection;
            }
            return new ThemedAssetCollection();
        }

        public List<string> GetAvailableThemes(string game)
        {
            if (_themeDatabase.TryGetValue(game, out var gameThemes))
            {
                var functionalAndAttributeThemes = new HashSet<string>
                {
                    "door", "switch", "light_source", "exit", "secret", "sky",
                    "support", "panel", "animated", "grate", "window", "border", "signage",
                    "monster_prop", "gore", "corrupted", "decayed", "puzzle_item",
                    "class_specific", "tapestry", "glass_window",
                    "key_indicator_blue", "key_indicator_red", "key_indicator_yellow", "key_indicator_green",
                    "monster"
                };

                return gameThemes.Themes
                    .Where(t => !functionalAndAttributeThemes.Contains(t.Key))
                    .Select(t => t.Key)
                    .ToList();
            }
            return new List<string>();
        }
    }
}