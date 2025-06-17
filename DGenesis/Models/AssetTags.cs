using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DGenesis.Models
{
    public class GameTags
    {
        [JsonPropertyName("tags")]
        public Dictionary<string, TaggedAssetCollection> Tags { get; set; }
    }

    public class TaggedAssetCollection
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