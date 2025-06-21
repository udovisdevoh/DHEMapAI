using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphRoleAssignmentService
    {
        private readonly DGraphPathfindingService _pathfinder;
        private readonly DGraphStrategicPlacementService _placementService; // <-- Injection
        private readonly Random _random = new Random();

        public DGraphRoleAssignmentService(DGraphPathfindingService pathfinder, DGraphStrategicPlacementService placementService)
        {
            _pathfinder = pathfinder;
            _placementService = placementService; // <-- Injection
        }

        public bool TryAssignRoles(DGraph graph, int requestedExitNodes, int requestedLockedPairs)
        {
            if (graph.Nodes.Count < 2) return false;

            foreach (var node in graph.Nodes)
            {
                node.Type = "standard";
                node.Unlocks = null;
            }

            // --- LOGIQUE ENTIÈREMENT REVUE ---
            // 1. Obtenir les placements stratégiques garantis d'être espacés
            var (startNodeId, exitNodeIds) = _placementService.FindStrategicPlacements(graph, requestedExitNodes);

            if (startNodeId == -1 || !exitNodeIds.Any()) return false; // Placement impossible

            var nodeDict = graph.Nodes.ToDictionary(n => n.Id);

            // 2. Assigner les rôles de départ et de sorties
            nodeDict[startNodeId].Type = "start";
            foreach (var exitId in exitNodeIds)
            {
                nodeDict[exitId].Type = "exit";
            }

            // 3. Assigner les clés/serrures aux nœuds restants
            var availableNodes = graph.Nodes
                                    .Where(n => n.Type == "standard")
                                    .ToList();

            for (int i = 0; i < requestedLockedPairs && availableNodes.Count >= 2; i++)
            {
                var lockedNode = availableNodes[_random.Next(availableNodes.Count)];
                lockedNode.Type = "locked";
                availableNodes.Remove(lockedNode);

                var unlockerNode = availableNodes[_random.Next(availableNodes.Count)];
                unlockerNode.Unlocks = new List<int> { lockedNode.Id };
                availableNodes.Remove(unlockerNode);
            }
            // --- FIN DE LA NOUVELLE LOGIQUE ---

            // 4. Validation finale (inchangée)
            var primaryExit = nodeDict[exitNodeIds.First()];
            var solvablePath = _pathfinder.FindSolvablePath(graph, startNodeId, primaryExit.Id);
            if (!solvablePath.Any())
            {
                Console.WriteLine("Validation échouée : Le chemin vers la sortie est infaisable.");
                return false;
            }

            if (!IsGraphFullyExplorable(graph, startNodeId))
            {
                Console.WriteLine("Validation échouée : Tous les nœuds ne sont pas accessibles.");
                return false;
            }

            return true;
        }

        private bool IsGraphFullyExplorable(DGraph graph, int startNodeId)
        {
            var nodeDict = graph.Nodes.ToDictionary(n => n.Id);
            var adjacencyList = BuildAdjacencyList(graph);

            var reachableNodes = new HashSet<int> { startNodeId };
            var collectedKeys = new HashSet<int>();

            bool newItemsFoundInPass;
            do
            {
                newItemsFoundInPass = false;
                foreach (var nodeId in reachableNodes.ToList())
                {
                    var node = nodeDict[nodeId];
                    if (node.Unlocks != null)
                    {
                        foreach (var key in node.Unlocks)
                        {
                            if (collectedKeys.Add(key)) newItemsFoundInPass = true;
                        }
                    }
                }

                bool nodesAdded;
                do
                {
                    nodesAdded = false;
                    foreach (var nodeId in reachableNodes.ToList())
                    {
                        foreach (var neighborId in adjacencyList[nodeId])
                        {
                            if (reachableNodes.Contains(neighborId)) continue;
                            var neighborNode = nodeDict[neighborId];
                            if (neighborNode.Type != "locked" || collectedKeys.Contains(neighborNode.Id))
                            {
                                reachableNodes.Add(neighborId);
                                nodesAdded = true;
                                newItemsFoundInPass = true;
                            }
                        }
                    }
                } while (nodesAdded);

            } while (newItemsFoundInPass);

            return reachableNodes.Count == graph.Nodes.Count;
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