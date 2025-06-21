using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphStrategicPlacementService
    {
        private readonly DGraphPathfindingService _pathfinder;
        private readonly Random _random = new Random();

        public DGraphStrategicPlacementService(DGraphPathfindingService pathfinder)
        {
            _pathfinder = pathfinder;
        }

        /// <summary>
        /// Trouve un ensemble de points (entrée et sorties) maximalement espacés les uns des autres.
        /// </summary>
        public (int startNodeId, List<int> exitNodeIds) FindStrategicPlacements(DGraph graph, int numExits)
        {
            var adjacencyList = BuildAdjacencyList(graph);
            var allNodes = graph.Nodes.Select(n => n.Id).ToList();
            if (allNodes.Count < numExits + 1)
            {
                // Pas assez de nœuds, on retourne une configuration basique
                return (allNodes.FirstOrDefault(), allNodes.Skip(1).Take(numExits).ToList());
            }

            // 1. Calculer toutes les distances entre toutes les paires de nœuds
            var allDistances = new Dictionary<int, Dictionary<int, int>>();
            foreach (var nodeId in allNodes)
            {
                allDistances[nodeId] = _pathfinder.FindAllDistances(adjacencyList, nodeId);
            }

            // 2. Trouver le "diamètre" du graphe (les deux points les plus éloignés)
            int bestA = -1, bestB = -1;
            int maxDist = -1;
            foreach (var startId in allNodes)
            {
                foreach (var endId in allNodes)
                {
                    if (allDistances[startId].TryGetValue(endId, out int dist) && dist > maxDist)
                    {
                        maxDist = dist;
                        bestA = startId;
                        bestB = endId;
                    }
                }
            }

            var strategicNodeIds = new List<int> { bestA, bestB };

            // 3. Trouver les autres points stratégiques en maximisant la distance au groupe existant
            while (strategicNodeIds.Count < numExits + 1)
            {
                double bestScore = -1;
                int bestCandidateId = -1;

                foreach (var candidateId in allNodes)
                {
                    if (strategicNodeIds.Contains(candidateId)) continue;

                    // Le score d'un candidat est sa distance à son plus proche voisin stratégique
                    double score = strategicNodeIds.Min(id => (double)allDistances[candidateId].GetValueOrDefault(id, 0));

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCandidateId = candidateId;
                    }
                }
                if (bestCandidateId != -1)
                {
                    strategicNodeIds.Add(bestCandidateId);
                }
                else
                {
                    break; // Plus de candidats possibles
                }
            }

            // 4. Assigner aléatoirement le rôle de départ à l'un des points stratégiques
            int startIndex = _random.Next(strategicNodeIds.Count);
            int startNodeId = strategicNodeIds[startIndex];
            strategicNodeIds.RemoveAt(startIndex);

            return (startNodeId, strategicNodeIds);
        }

        private Dictionary<int, List<int>> BuildAdjacencyList(DGraph graph)
        {
            var adjacencyList = new Dictionary<int, List<int>>();
            foreach (var node in graph.Nodes) { adjacencyList[node.Id] = new List<int>(); }
            foreach (var edge in graph.Edges)
            {
                adjacencyList[edge.Source].Add(edge.Target);
                adjacencyList[edge.Target].Add(edge.Source);
            }
            return adjacencyList;
        }
    }
}