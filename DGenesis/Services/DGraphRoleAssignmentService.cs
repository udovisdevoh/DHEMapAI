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

            // 1. Choisir le nœud de départ de manière aléatoire
            var startNode = graph.Nodes[_random.Next(graph.Nodes.Count)];
            startNode.Type = "start";

            // --- NOUVELLE LOGIQUE DE SÉLECTION DES SORTIES ---

            // On s'assure de ne pas demander plus de sorties qu'il n'y a de nœuds disponibles
            int exitNodesToCreate = Math.Min(requestedExitNodes, graph.Nodes.Count - 1);
            var exitNodes = new List<DGraphNode>();

            // Calculer les distances de tous les nœuds par rapport au départ (une seule fois)
            var distancesFromStart = new Dictionary<int, int>();
            foreach (var node in graph.Nodes)
            {
                distancesFromStart[node.Id] = _pathfinder.FindShortestPath(graph, startNode.Id, node.Id).Count;
            }

            // Sélection de la première sortie : la plus éloignée du départ
            var firstExitId = distancesFromStart.Where(kvp => kvp.Key != startNode.Id)
                                                .OrderByDescending(kvp => kvp.Value)
                                                .First().Key;
            var firstExitNode = graph.Nodes.First(n => n.Id == firstExitId);
            firstExitNode.Type = "exit";
            exitNodes.Add(firstExitNode);

            // Sélection des sorties suivantes de manière itérative
            while (exitNodes.Count < exitNodesToCreate)
            {
                var standardNodes = graph.Nodes.Where(n => n.Type == "standard").ToList();
                if (!standardNodes.Any()) break; // Plus de candidats

                DGraphNode bestNextExit = null;
                double maxOfMinDistances = -1;

                // Pour chaque candidat, on calcule sa "qualité" (distance aux sorties existantes)
                foreach (var candidateNode in standardNodes)
                {
                    double minDistanceToAnyExit = double.MaxValue;

                    // Trouver la distance la plus courte entre ce candidat et n'importe quelle sortie déjà placée
                    foreach (var existingExit in exitNodes)
                    {
                        var pathToExit = _pathfinder.FindShortestPath(graph, candidateNode.Id, existingExit.Id);
                        minDistanceToAnyExit = Math.Min(minDistanceToAnyExit, pathToExit.Count);
                    }

                    // On cherche le candidat qui maximise cette distance minimale
                    if (minDistanceToAnyExit > maxOfMinDistances)
                    {
                        maxOfMinDistances = minDistanceToAnyExit;
                        bestNextExit = candidateNode;
                    }
                }

                if (bestNextExit != null)
                {
                    bestNextExit.Type = "exit";
                    exitNodes.Add(bestNextExit);
                }
                else
                {
                    // Ne devrait pas arriver s'il y a des candidats, mais c'est une sécurité
                    break;
                }
            }
            // --- FIN DE LA NOUVELLE LOGIQUE ---


            // 3. Assigner les paires verrouillées/déverrouillantes
            var remainingStandardNodes = graph.Nodes.Where(n => n.Type == "standard").ToList();
            var pairsCreated = 0;
            var usedAsKeyOrLock = new HashSet<int>();

            // On utilise la première sortie trouvée comme destination principale pour les tests de solvabilité
            var primaryExitId = exitNodes.First().Id;

            while (pairsCreated < requestedLockedPairs && remainingStandardNodes.Count >= 2)
            {
                // Choisir un nœud à verrouiller
                var lockedNode = remainingStandardNodes[_random.Next(remainingStandardNodes.Count)];
                remainingStandardNodes.Remove(lockedNode);

                // Vérifier si le verrouillage de ce nœud bloque le niveau
                var mainPath = _pathfinder.FindShortestPath(graph, startNode.Id, primaryExitId, new HashSet<int> { lockedNode.Id });

                if (mainPath.Count == 0)
                {
                    // Ce nœud est sur un chemin critique, on ne peut pas le verrouiller. On l'ignore.
                    continue;
                }

                // Le nœud peut être verrouillé. Maintenant, on trouve une "clé" sur le chemin principal.
                var potentialUnlockers = mainPath.Select(id => graph.Nodes.First(n => n.Id == id))
                                                 .Where(n => n.Type == "standard" && !usedAsKeyOrLock.Contains(n.Id))
                                                 .ToList();

                if (potentialUnlockers.Any())
                {
                    var unlockerNode = potentialUnlockers[_random.Next(potentialUnlockers.Count)];

                    lockedNode.Type = "locked";
                    unlockerNode.Unlocks = new List<int> { lockedNode.Id };

                    // On retire le nœud "clé" de la liste des candidats pour ne pas le réutiliser
                    remainingStandardNodes.Remove(unlockerNode);
                    usedAsKeyOrLock.Add(lockedNode.Id);
                    usedAsKeyOrLock.Add(unlockerNode.Id);

                    pairsCreated++;
                }
            }
        }
    }
}