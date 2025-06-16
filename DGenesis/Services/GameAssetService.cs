// Services/GameAssetService.cs

using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class GameAsset
    {
        public int TypeId { get; set; }
        public string Name { get; set; }
    }

    public class GameAssetService
    {
        private static readonly Dictionary<string, List<string>> Textures = new Dictionary<string, List<string>>
        {
            // todo: lire game_assets.json
        };

        private static readonly Dictionary<string, List<string>> Flats = new Dictionary<string, List<string>>
        {
            // todo: lire game_assets.json
        };

        private static readonly Dictionary<string, List<GameAsset>> Things = new Dictionary<string, List<GameAsset>>
        {
            // todo: lire game_assets.json
        };

        public List<string> GetTexturesForGame(string game) => Textures.GetValueOrDefault(game.ToLower(), new List<string>());
        public List<string> GetFlatsForGame(string game) => Flats.GetValueOrDefault(game.ToLower(), new List<string>());
        public List<GameAsset> GetThingsForGame(string game) => Things.GetValueOrDefault(game.ToLower(), new List<GameAsset>());
    }

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
        public List<GameAssetMusic> Music { get; set; }
    }

    public class GameAssetThing
    {
        public int Type { get; set; }
        public string Name { get; set; }
    }

    public class GameAssetMusic
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}