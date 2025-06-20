using DGenesis.Models.DGraph;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphLayoutService
    {
        public void AssignLayout(DGraph graph, Dictionary<int, int> nodeDepths)
        {
            const double verticalSpacing = 150.0;
            const double horizontalSpacing = 120.0;

            graph.Nodes.First(n => n.Id == 0).Position = new Position { X = 0, Y = 0 };

            int maxDepth = nodeDepths.Values.Max();

            for (int depth = 1; depth <= maxDepth; depth++)
            {
                var nodesAtCurrentDepth = graph.Nodes.Where(n => nodeDepths.ContainsKey(n.Id) && nodeDepths[n.Id] == depth).ToList();
                if (!nodesAtCurrentDepth.Any()) continue;

                var nodeBarycenters = new Dictionary<int, double>();
                foreach (var node in nodesAtCurrentDepth)
                {
                    var upperLevelNeighbors = graph.Edges
                        .Where(e => e.Source == node.Id || e.Target == node.Id)
                        .Select(e => e.Source == node.Id ? graph.Nodes.FirstOrDefault(n => n.Id == e.Target) : graph.Nodes.FirstOrDefault(n => n.Id == e.Source))
                        .Where(neighborNode => neighborNode != null && nodeDepths.ContainsKey(neighborNode.Id) && nodeDepths[neighborNode.Id] < depth)
                        .ToList();

                    if (upperLevelNeighbors.Any())
                    {
                        nodeBarycenters[node.Id] = upperLevelNeighbors.Average(n => n.Position.X);
                    }
                    else
                    {
                        var parent = graph.Nodes.FirstOrDefault(n => n.Position != null && graph.Edges.Any(e => e.Source == n.Id && e.Target == node.Id));
                        nodeBarycenters[node.Id] = parent?.Position.X ?? 0;
                    }
                }

                var sortedNodes = nodesAtCurrentDepth.OrderBy(n => nodeBarycenters[n.Id]).ToList();
                double totalWidth = (sortedNodes.Count - 1) * horizontalSpacing;
                double startX = -totalWidth / 2.0;

                for (int i = 0; i < sortedNodes.Count; i++)
                {
                    var nodeToPlace = sortedNodes[i];
                    if (nodeToPlace.Position == null)
                    {
                        nodeToPlace.Position = new Position
                        {
                            X = startX + i * horizontalSpacing,
                            Y = depth * verticalSpacing
                        };
                    }
                }
            }
        }
    }
}