using DGenesis.Models.DGraph;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphPathfindingService
    {
        /// <summary>
        /// Trouve le chemin le plus court entre deux nœuds en utilisant l'algorithme Breadth-First Search (BFS).
        /// </summary>
        /// <param name="graph">Le graphe sur lequel chercher.</param>
        /// <param name="startNodeId">L'ID du nœud de départ.</param>
        /// <param name="endNodeId">L'ID du nœud d'arrivée.</param>
        /// <param name="nodesToIgnore">Une liste d'IDs de nœuds à ignorer durant la recherche.</param>
        /// <returns>Une liste d'IDs représentant le chemin, ou une liste vide si aucun chemin n'est trouvé.</returns>
        public List<int> FindShortestPath(DGraph graph, int startNodeId, int endNodeId, HashSet<int> nodesToIgnore = null)
        {
            nodesToIgnore ??= new HashSet<int>();
            var queue = new Queue<List<int>>();
            queue.Enqueue(new List<int> { startNodeId });

            var visited = new HashSet<int> { startNodeId };

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                int currentNodeId = path.Last();

                if (currentNodeId == endNodeId)
                {
                    return path; // Chemin trouvé !
                }

                var neighbors = GetNeighbors(graph, currentNodeId);
                foreach (var neighborId in neighbors)
                {
                    if (!visited.Contains(neighborId) && !nodesToIgnore.Contains(neighborId))
                    {
                        visited.Add(neighborId);
                        var newPath = new List<int>(path) { neighborId };
                        queue.Enqueue(newPath);
                    }
                }
            }

            return new List<int>(); // Aucun chemin trouvé
        }

        private IEnumerable<int> GetNeighbors(DGraph graph, int nodeId)
        {
            return graph.Edges
                .Where(e => e.Source == nodeId || e.Target == nodeId)
                .Select(e => e.Source == nodeId ? e.Target : e.Source);
        }
    }
}