using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DGraphBuilder.Models.DGraph
{
    public class DGraphFile
    {
        public string Format { get; set; }
        public string Version { get; set; }
        public MapInfo MapInfo { get; set; }
        public Dictionary<string, List<WeightedTexture>> ThemePalette { get; set; }
        public List<Room> Rooms { get; set; }
        public List<Connection> Connections { get; set; }
    }

    public class MapInfo
    {
        public string Game { get; set; }
        public int Map { get; set; }
        public string Name { get; set; }
        public string Music { get; set; }
    }

    public class WeightedTexture
    {
        public string Name { get; set; }
        public int Weight { get; set; }
    }

    public class Room
    {
        public string Id { get; set; }
        public string ParentRoom { get; set; }
        public ShapeHint ShapeHint { get; set; }
        public RoomProperties Properties { get; set; }
        public Contents Contents { get; set; }
        public List<Feature> Features { get; set; }
    }

    public class ShapeHint
    {
        public int Vertices { get; set; }
        public string Description { get; set; }
    }

    public class RoomProperties
    {
        public string Floor { get; set; }
        public string Ceiling { get; set; }
        public string LightLevel { get; set; }
        public string WallTexture { get; set; }
        public string FloorFlat { get; set; }
        public string CeilingFlat { get; set; }
        public int? Tag { get; set; }
    }

    public class Contents
    {
        public List<ContentItem> Monsters { get; set; }
        public List<ContentItem> Items { get; set; }
        public List<ContentItem> Decorations { get; set; }
    }

    public class ContentItem
    {
        public string Name { get; set; }
        [JsonPropertyName("typeId")]
        public int TypeId { get; set; }
        public int Count { get; set; }
    }

    public class Feature
    {
        public string Name { get; set; }
        [JsonPropertyName("actionId")]
        public int ActionId { get; set; }
        public int Count { get; set; }
        public FeatureProperties Properties { get; set; }
    }

    public class FeatureProperties
    {
        public string Texture { get; set; }
        public int? TargetTag { get; set; }
    }

    public class Connection
    {
        public string FromRoom { get; set; }
        public string ToRoom { get; set; }
        public string Type { get; set; }
        public ConnectionProperties Properties { get; set; }
    }

    public class ConnectionProperties
    {
        public string Description { get; set; }
        public string Texture { get; set; }
        public string Key { get; set; }
    }
}