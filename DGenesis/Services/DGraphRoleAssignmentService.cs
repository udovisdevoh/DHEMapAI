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

        public void AssignRoles(DGraph graph, int requestedExitNodes, int requestedLockedPairs)
        {
            if (graph.Nodes.Count < 2) return;

            // Pré-calculer la liste d'adjacence pour toutes les opérations de pathfinding
            var adjacencyList = new Dictionary<int, List<int>>();
            foreach (var node in graph.Nodes) { adjacencyList[node.Id] = new List<int>(); }
            foreach (var edge in graph.Edges)
            {
                adjacencyList[edge.Source].Add(edge.Target);
                adjacencyList[edge.Target].Add(edge.Source);
            }

            var startNode = graph.Nodes[_random.Next(graph.Nodes.Count)];
            startNode.Type = "start";

            int exitNodesToCreate = Math.Min(requestedExitNodes, graph.Nodes.Count - 1);
            var exitNodes = new List<DGraphNode>();

            var distancesFromStart = new Dictionary<int, int>();
            foreach (var node in graph.Nodes)
            {
                distancesFromStart[node.Id] = _pathfinder.FindShortestPath(graph, startNode.Id, node.Id, adjacencyList).Count;
            }

            var firstExitId = distancesFromStart.Where(kvp => kvp.Key != startNode.Id).OrderByDescending(kvp => kvp.Value).First().Key;
            var firstExitNode = graph.Nodes.First(n => n.Id == firstExitId);
            firstExitNode.Type = "exit";
            exitNodes.Add(firstExitNode);

            while (exitNodes.Count < exitNodesToCreate)
            {
                var standardNodes = graph.Nodes.Where(n => n.Type == "standard").ToList();
                if (!standardNodes.Any()) break;
                DGraphNode bestNextExit = null;
                double maxOfMinDistances = -1;

                foreach (var candidateNode in standardNodes)
                {
                    double minDistanceToAnyExit = double.MaxValue;
                    foreach (var existingExit in exitNodes)
                    {
                        minDistanceToAnyExit = Math.Min(minDistanceToAnyExit, _pathfinder.FindShortestPath(graph, candidateNode.Id, existingExit.Id, adjacencyList).Count);
                    }
                    if (minDistanceToAnyExit > maxOfMinDistances)
                    {
                        maxOfMinDistances = minDistanceToAnyExit;
                        bestNextExit = candidateNode;
                    }
                }
                if (bestNextExit != null) { bestNextExit.Type = "exit"; exitNodes.Add(bestNextExit); } else { break; }
            }

            var remainingStandardNodes = graph.Nodes.Where(n => n.Type == "standard").ToList();
            var pairsCreated = 0;
            var usedAsKeyOrLock = new HashSet<int>();
            var primaryExitId = exitNodes.First().Id;

            while (pairsCreated < requestedLockedPairs && remainingStandardNodes.Count >= 2)
            {
                var lockedNode = remainingStandardNodes[_random.Next(remainingStandardNodes.Count)];
                remainingStandardNodes.Remove(lockedNode);

                var mainPath = _pathfinder.FindShortestPath(graph, startNode.Id, primaryExitId, adjacencyList, new HashSet<int> { lockedNode.Id });
                if (mainPath.Count == 0) continue;

                var potentialUnlockers = mainPath.Select(id => graph.Nodes.First(n => n.Id == id)).Where(n => n.Type == "standard" && !usedAsKeyOrLock.Contains(n.Id)).ToList();
                if (potentialUnlockers.Any())
                {
                    var unlockerNode = potentialUnlockers[_random.Next(potentialUnlockers.Count)];
                    lockedNode.Type = "locked";
                    unlockerNode.Unlocks = new List<int> { lockedNode.Id };
                    remainingStandardNodes.Remove(unlockerNode);
                    usedAsKeyOrLock.Add(lockedNode.Id);
                    usedAsKeyOrLock.Add(unlockerNode.Id);
                    pairsCreated++;
                }
            }
        }
    }
}