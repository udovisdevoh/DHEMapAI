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

        public ThemedAssetCollection GetAssetsForTheme(string game, string themeKey)
        {
            if (_themeDatabase.TryGetValue(game, out var gameThemes) &&
                gameThemes.Themes.TryGetValue(themeKey, out var assetCollection))
            {
                return assetCollection;
            }
            return new ThemedAssetCollection();
        }

        public List<string> GetAvailableThemeKeys(string game)
        {
            if (_themeDatabase.TryGetValue(game, out var gameThemes))
            {
                // CORRECTION FINALE : La liste d'exclusion est mise à jour selon votre décision.
                // Seuls les tags purement fonctionnels ou techniques sont exclus.
                var functionalAndAttributeTags = new HashSet<string>
                {
                    // Tags fonctionnels de base retirés par l'utilisateur
                    "door",
                    "switch",
                    "key_indicator_blue",
                    "key_indicator_red",
                    "key_indicator_yellow",
                    "key_indicator_green",
                    "monster",

                    // Tags retirés suite à votre dernière décision
                    "animated",
                    "signage",

                    // Autres tags purement fonctionnels que nous gardons exclus
                    "exit",
                    "secret",
                    "puzzle_item",
                    "class_specific"
                };

                return gameThemes.Themes
                    .Where(t => !functionalAndAttributeTags.Contains(t.Key))
                    .Select(t => t.Key)
                    .ToList();
            }
            return new List<string>();
        }
    }
}