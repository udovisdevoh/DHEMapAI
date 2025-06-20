using DGenesis.Models.DGraph;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphPathfindingService
    {
        public List<int> FindShortestPath(DGraph graph, int startNodeId, int endNodeId, IReadOnlyDictionary<int, List<int>> adjacencyList, HashSet<int> nodesToIgnore = null)
        {
            nodesToIgnore ??= new HashSet<int>();
            if (!adjacencyList.ContainsKey(startNodeId) || !adjacencyList.ContainsKey(endNodeId))
            {
                return new List<int>(); // Nœud de départ ou d'arrivée non valide
            }

            var queue = new Queue<List<int>>();
            queue.Enqueue(new List<int> { startNodeId });
            var visited = new HashSet<int> { startNodeId };

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                int currentNodeId = path.Last();
                if (currentNodeId == endNodeId) return path;

                foreach (var neighborId in adjacencyList[currentNodeId])
                {
                    if (!visited.Contains(neighborId) && !nodesToIgnore.Contains(neighborId))
                    {
                        visited.Add(neighborId);
                        var newPath = new List<int>(path) { neighborId };
                        queue.Enqueue(newPath);
                    }
                }
            }
            return new List<int>();
        }
    }
}