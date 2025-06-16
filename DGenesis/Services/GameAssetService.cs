using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DGenesis.Services
{
    // Les modèles pour désérialiser le fichier JSON
    public class GameAssetDatabase
    {
        public GameData Doom { get; set; }
        public GameData Doom2 { get; set; }
        public GameData Heretic { get; set; }
        public GameData Hexen { get; set; }
    }

    public class GameData
    {
        public List<string> Textures { get; set; }
        public List<string> Flats { get; set; }
        public List<GameAssetThing> Things { get; set; }
        public List<string> Music { get; set; }
    }

    public class GameAssetThing
    {
        [JsonPropertyName("type")]
        public int TypeId { get; set; }
        public string Name { get; set; }
    }

    public class GameAssetMusic
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }


    public class GameAssetService
    {
        private static readonly GameAssetDatabase _database;

        // Le constructeur statique est appelé une seule fois au démarrage de l'application.
        static GameAssetService()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "game_assets.json");
                if (System.IO.File.Exists(filePath))
                {
                    string jsonString = System.IO.File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    _database = JsonSerializer.Deserialize<GameAssetDatabase>(jsonString, options);
                }
                else
                {
                    // Si le fichier est introuvable, on initialise une base de données vide pour éviter les erreurs.
                    _database = new GameAssetDatabase();
                }
            }
            catch (Exception ex)
            {
                // Gérer l'erreur de lecture ou de parsing
                Console.WriteLine($"Erreur lors du chargement de game_assets.json: {ex.Message}");
                _database = new GameAssetDatabase();
            }
        }

        private GameData GetGameData(string game)
        {
            return game.ToLower() switch
            {
                "doom" => _database.Doom,
                "doom2" => _database.Doom2,
                "heretic" => _database.Heretic,
                "hexen" => _database.Hexen,
                _ => null
            };
        }

        public List<string> GetTexturesForGame(string game)
        {
            var gameData = GetGameData(game);
            return gameData?.Textures ?? new List<string>();
        }

        public List<string> GetFlatsForGame(string game)
        {
            var gameData = GetGameData(game);
            return gameData?.Flats ?? new List<string>();
        }

        public List<GameAssetThing> GetThingsForGame(string game)
        {
            var gameData = GetGameData(game);
            return gameData?.Things ?? new List<GameAssetThing>();
        }

        public List<string> GetMusicForGame(string game)
        {
            var gameData = GetGameData(game);
            return gameData?.Music ?? new List<string>();
        }
    }
}