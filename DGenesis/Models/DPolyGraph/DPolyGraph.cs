using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DGenesis.Models.DPolyGraph
{
    public class DPolyGraph
    {
        [JsonPropertyName("format")]
        public string Format { get; set; } = "DPolyGraph";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.2";

        [JsonPropertyName("sectors")]
        public List<DPolySector> Sectors { get; set; } = new List<DPolySector>();
    }

    public class DPolySector
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("polygon")]
        public List<DPolyVertex> Polygon { get; set; } = new List<DPolyVertex>();

        [JsonPropertyName("unlocksSector")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? UnlocksSector { get; set; }

        [JsonPropertyName("unlockedBySector")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? UnlockedBySector { get; set; }
    }

    public class DPolyVertex
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }
    }
}