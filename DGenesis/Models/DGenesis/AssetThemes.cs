using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DGenesis.Models
{
    public class GameThemes
    {
        [JsonPropertyName("themes")]
        public Dictionary<string, ThemedAssetCollection> Themes { get; set; }
    }

    public class ThemedAssetCollection
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("textures")]
        public List<string> Textures { get; set; } = new List<string>();

        [JsonPropertyName("flats")]
        public List<string> Flats { get; set; } = new List<string>();

        [JsonPropertyName("things")]
        public List<int> Things { get; set; } = new List<int>();

        [JsonPropertyName("music")]
        public List<string> Music { get; set; } = new List<string>();
    }
}