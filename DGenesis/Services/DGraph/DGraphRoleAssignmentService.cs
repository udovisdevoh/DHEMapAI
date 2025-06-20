using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphRoleAssignmentService
    {
        private readonly DGraphPathfindingService _pathfinder;
        private readonly Random _random = new Random();

        public DGraphRoleAssignmentService(DGraphPathfindingService pathfinder)
        {
            _pathfinder = pathfinder;
        }

        public bool TryAssignRoles(DGraph graph, int requestedExitNodes, int requestedLockedPairs)
        {
            if (graph.Nodes.Count < 2) return false;

            // Réinitialiser tous les nœuds
            foreach (var node in graph.Nodes)
            {
                node.Type = "standard";
                node.Unlocks = null;
            }

            var availableNodes = graph.Nodes.ToList();
            var adjacencyList = BuildAdjacencyList(graph);

            // --- NOUVELLE LOGIQUE D'ASSIGNATION STRATÉGIQUE ---

            // 1. Assigner le départ au hasard
            var startNode = availableNodes[_random.Next(availableNodes.Count)];
            startNode.Type = "start";
            availableNodes.Remove(startNode);

            // 2. Assigner les sorties en se basant sur la distance du départ
            var distancesFromStart = _pathfinder.FindAllDistances(adjacencyList, startNode.Id);
            var exitNodes = new List<DGraphNode>();

            // On s'assure qu'il y a des nœuds atteignables pour y placer une sortie
            if (distancesFromStart.Count <= 1) return false;

            // La première sortie est la plus éloignée
            var furthestNodeId = distancesFromStart.OrderByDescending(kvp => kvp.Value).First().Key;
            var furthestNode = graph.Nodes.First(n => n.Id == furthestNodeId);
            furthestNode.Type = "exit";
            exitNodes.Add(furthestNode);
            availableNodes.Remove(furthestNode);

            // Les sorties suivantes sont choisies pour être également loin
            while (exitNodes.Count < requestedExitNodes && availableNodes.Any())
            {
                // On prend les 20% des nœuds les plus éloignés comme candidats potentiels
                int candidateCount = Math.Max(1, (int)(distancesFromStart.Count * 0.2));
                var potentialExitIds = distancesFromStart.OrderByDescending(kvp => kvp.Value)
                                                         .Select(kvp => kvp.Key)
                                                         .Where(id => availableNodes.Any(n => n.Id == id))
                                                         .Take(candidateCount)
                                                         .ToList();
                if (!potentialExitIds.Any()) break;

                var nextExitId = potentialExitIds[_random.Next(potentialExitIds.Count)];
                var nextExitNode = graph.Nodes.First(n => n.Id == nextExitId);
                nextExitNode.Type = "exit";
                exitNodes.Add(nextExitNode);
                availableNodes.Remove(nextExitNode);
            }

            // 3. Assigner les paires Clé/Serrure dans l'espace restant
            var keyNodes = new List<DGraphNode>();
            var lockNodes = new List<DGraphNode>();
            for (int i = 0; i < requestedLockedPairs && availableNodes.Count >= 2; i++)
            {
                var lockedNode = availableNodes[_random.Next(availableNodes.Count)];
                lockedNode.Type = "locked";
                lockNodes.Add(lockedNode);
                availableNodes.Remove(lockedNode);

                var unlockerNode = availableNodes[_random.Next(availableNodes.Count)];
                unlockerNode.Unlocks = new List<int> { lockedNode.Id };
                keyNodes.Add(unlockerNode);
                availableNodes.Remove(unlockerNode);
            }

            // --- FIN DE LA NOUVELLE LOGIQUE ---

            // Validation finale (inchangée)
            var primaryExit = exitNodes.First();
            var solvablePath = _pathfinder.FindSolvablePath(graph, startNode.Id, primaryExit.Id);
            if (!solvablePath.Any())
            {
                Console.WriteLine("Validation échouée : Le chemin vers la sortie est infaisable.");
                return false;
            }

            if (!IsGraphFullyExplorable(graph, startNode.Id))
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