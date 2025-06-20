using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DGenesis.Models
{
    public class DShape
    {
        [JsonPropertyName("format")]
        public string Format { get; set; } = "DShape";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("vertices")]
        public List<DShapeVertex> Vertices { get; set; } = new List<DShapeVertex>();
    }

    public class DShapeVertex
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }
    }
}