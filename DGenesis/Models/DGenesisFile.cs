using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DGenesis.Models
{
    public class DGenesisFile
    {
        [JsonPropertyName("format")]
        public string Format { get; set; } = "D-Genesis";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("mapInfo")]
        public MapInfo MapInfo { get; set; }

        [JsonPropertyName("generationParams")]
        public GenerationParams GenerationParams { get; set; }

        [JsonPropertyName("sectorBehaviorPalette")]
        public Dictionary<string, SectorBehavior> SectorBehaviorPalette { get; set; }

        [JsonPropertyName("featurePalette")]
        public Dictionary<string, Feature> FeaturePalette { get; set; }

        [JsonPropertyName("themePalette")]
        public Dictionary<string, List<WeightedAsset>> ThemePalette { get; set; }

        [JsonPropertyName("thematicTokens")]
        public List<ThematicToken> ThematicTokens { get; set; }
    }

    public class MapInfo
    {
        [JsonPropertyName("game")]
        public string Game { get; set; }

        [JsonPropertyName("mapLumpName")]
        public string MapLumpName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("music")]
        public string Music { get; set; }
    }

    public class GenerationParams
    {
        [JsonPropertyName("roomCount")]
        public int RoomCount { get; set; }

        [JsonPropertyName("avgConnectivity")]
        public double AvgConnectivity { get; set; }

        [JsonPropertyName("avgFloorHeightDelta")]
        public int AvgFloorHeightDelta { get; set; }

        [JsonPropertyName("avgHeadroom")]
        public int AvgHeadroom { get; set; }

        [JsonPropertyName("totalVerticalSpan")]
        public int TotalVerticalSpan { get; set; }

        [JsonPropertyName("verticalTransitionProfile")]
        public List<VerticalTransition> VerticalTransitionProfile { get; set; }
    }

    public class VerticalTransition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }
    }

    public class SectorBehavior
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("sectorSpecial")]
        public int SectorSpecial { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }
    }

    public class Feature
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }
    }

    public class WeightedAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }
    }

    public class ThematicToken
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("typeId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TypeId { get; set; }

        [JsonPropertyName("adjacencyRules")]
        public List<AdjacencyRule> AdjacencyRules { get; set; }
    }

    public class AdjacencyRule
    {
        [JsonPropertyName("modifier")]
        public double Modifier { get; set; }

        [JsonPropertyName("adjacentTo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string AdjacentTo { get; set; }

        [JsonPropertyName("adjacentToTypeId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AdjacentToTypeId { get; set; }
    }
}