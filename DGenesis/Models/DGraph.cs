using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DGenesis.Models.DGraph
{
    // Classe racine qui sera sérialisée en JSON
    public class DGraph
    {
        [JsonPropertyName("nodes")]
        public List<DGraphNode> Nodes { get; set; } = new List<DGraphNode>();

        [JsonPropertyName("edges")]
        public List<DGraphEdge> Edges { get; set; } = new List<DGraphEdge>();
    }

    // Représente un nœud dans le graphe
    public class DGraphNode
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // "start", "exit", "locked", "standard"

        [JsonPropertyName("position")]
        public Position Position { get; set; }

        [JsonPropertyName("unlocks")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? Unlocks { get; set; }
    }

    // Représente les coordonnées d'un nœud
    public class Position
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }
    }

    // Représente une arête (connexion) entre deux nœuds
    public class DGraphEdge
    {
        [JsonPropertyName("source")]
        public int Source { get; set; }

        [JsonPropertyName("target")]
        public int Target { get; set; }
    }
}