using DGenesis.Models.DGraph;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class GraphAnalysisService
    {
        private List<int>[] _adjacencyList;
        private List<List<int>> _cycles;
        private HashSet<string> _foundCycleHashes;

        public List<List<int>> FindSimpleCycles(DGraph graph, int minLength, int maxLength)
        {
            _cycles = new List<List<int>>();
            _foundCycleHashes = new HashSet<string>();

            // 1. Construire une liste d'adjacence pour des recherches rapides
            int nodeCount = graph.Nodes.Max(n => n.Id) + 1;
            _adjacencyList = new List<int>[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                _adjacencyList[i] = new List<int>();
            }
            foreach (var edge in graph.Edges)
            {
                _adjacencyList[edge.Source].Add(edge.Target);
                _adjacencyList[edge.Target].Add(edge.Source);
            }

            // 2. Lancer la recherche de cycles depuis chaque nœud
            foreach (var node in graph.Nodes)
            {
                FindCyclesRecursive(node.Id, -1, new List<int>(), minLength, maxLength);
            }

            return _cycles;
        }

        private void FindCyclesRecursive(int currentNodeId, int parentNodeId, List<int> path, int minLength, int maxLength)
        {
            path.Add(currentNodeId);

            if (path.Count > maxLength)
            {
                path.RemoveAt(path.Count - 1);
                return;
            }

            foreach (var neighborId in _adjacencyList[currentNodeId])
            {
                if (neighborId == parentNodeId) continue; // Évite de revenir sur ses pas immédiatement

                int cycleStartIndex = path.IndexOf(neighborId);
                if (cycleStartIndex != -1) // Un cycle est trouvé !
                {
                    var cycle = path.GetRange(cycleStartIndex, path.Count - cycleStartIndex);
                    if (cycle.Count >= minLength)
                    {
                        // On normalise le cycle pour éviter les doublons (ex: A-B-C, B-C-A, C-A-B)
                        var sortedCycle = cycle.OrderBy(id => id).ToList();
                        string cycleHash = string.Join("-", sortedCycle);

                        if (!_foundCycleHashes.Contains(cycleHash))
                        {
                            _cycles.Add(cycle);
                            _foundCycleHashes.Add(cycleHash);
                        }
                    }
                }
                else
                {
                    // Continuer la recherche en profondeur
                    FindCyclesRecursive(neighborId, currentNodeId, path, minLength, maxLength);
                }
            }

            path.RemoveAt(path.Count - 1);
        }
    }
}