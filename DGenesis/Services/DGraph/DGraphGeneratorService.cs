using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphGeneratorService
    {
        private readonly DGraphLayoutService _layoutService;
        private readonly DGraphUntanglerService _untanglerService;
        private readonly DGraphRoleAssignmentService _roleAssignmentService;
        private readonly DGraphChaosService _chaosService;
        private readonly DGraphFinalizeService _finalizeService;

        private Random _random = new Random();
        private const double LocalMergeChance = 0.15;
        private const double GlobalMergeFactor = 0.10;
        private const int ATTEMPTS_PER_COMPLEXITY = 5;
        private const int MINIMUM_NODES = 4;
        private const double DEGENERATE_GRAPH_THRESHOLD = 0.60;

        public DGraphGeneratorService(DGraphLayoutService layoutService, DGraphUntanglerService untanglerService, DGraphRoleAssignmentService roleAssignmentService, DGraphChaosService chaosService, DGraphFinalizeService finalizeService)
        {
            _layoutService = layoutService;
            _untanglerService = untanglerService;
            _roleAssignmentService = roleAssignmentService;
            _chaosService = chaosService;
            _finalizeService = finalizeService;
        }

        public DGraph Generate(int requestedNodes, int lockedPairs, int exitNodes)
        {
            int currentNodesToGenerate = requestedNodes;
            while (currentNodesToGenerate >= MINIMUM_NODES)
            {
                Console.WriteLine($"--- Début de la génération pour une complexité de {currentNodesToGenerate} nœuds ---");
                for (int attempt = 1; attempt <= ATTEMPTS_PER_COMPLEXITY; attempt++)
                {
                    Console.WriteLine($"... Tentative #{attempt}/{ATTEMPTS_PER_COMPLEXITY} pour {currentNodesToGenerate} nœuds.");

                    var (graph, nodeDepths) = GenerateTopology(currentNodesToGenerate);
                    if (graph.Nodes.Count < 2) continue;

                    _layoutService.AssignLayout(graph, nodeDepths);
                    _chaosService.ApplyChaos(graph);

                    int refinementIterations = 0;
                    const int MAX_REFINEMENT_ITERATIONS = 10;
                    bool geometryChangedInLastPass;
                    do
                    {
                        geometryChangedInLastPass = false;
                        bool spacingChanged = _finalizeService.EnforceNodeEdgeSpacing(graph);
                        if (spacingChanged) geometryChangedInLastPass = true;

                        bool untangleSuccess = _untanglerService.TryUntangleGraph(graph);
                        if (!untangleSuccess) goto next_attempt;

                        // On considère que si l'espacement a changé, on doit revérifier
                        // car cela aurait pu créer un nouveau croisement résolu par l'untangler.
                        // C'est une condition simple mais efficace pour la convergence.

                        refinementIterations++;
                    } while (geometryChangedInLastPass && refinementIterations < MAX_REFINEMENT_ITERATIONS);

                    if (refinementIterations >= MAX_REFINEMENT_ITERATIONS)
                    {
                        Console.WriteLine("La boucle de raffinement n'a pas convergé. Nouvelle tentative...");
                        goto next_attempt;
                    }

                    Console.WriteLine("Raffinement géométrique terminé avec succès.");

                    // --- LOGIQUE FINALE D'ASSIGNATION ET VALIDATION ---
                    bool rolesAssignedSuccessfully = false;
                    for (int roleAttempt = 0; roleAttempt < 20; roleAttempt++) // 20 essais pour trouver une assignation de rôles valide
                    {
                        if (_roleAssignmentService.TryAssignRoles(graph, exitNodes, lockedPairs))
                        {
                            rolesAssignedSuccessfully = true;
                            Console.WriteLine($"Assignation des rôles réussie à la tentative #{roleAttempt + 1}.");
                            break;
                        }
                    }

                    if (rolesAssignedSuccessfully)
                    {
                        _finalizeService.NormalizeAndCenter(graph);
                        Console.WriteLine($"Génération terminée avec succès avec {graph.Nodes.Count} nœuds.");
                        return graph;
                    }
                    else
                    {
                        Console.WriteLine("Échec de l'assignation des rôles (aucune configuration faisable trouvée). Nouvelle tentative de génération...");
                        goto next_attempt;
                    }

                next_attempt:;
                }

                Console.WriteLine($"[AVERTISSEMENT] Impossible de générer un graphe stable avec {currentNodesToGenerate} nœuds. Essai avec {currentNodesToGenerate - 1}.");
                currentNodesToGenerate--;
            }

            Console.WriteLine($"[ERREUR] Échec de la génération du graphe après avoir essayé jusqu'à {MINIMUM_NODES} nœuds.");
            return new DGraph();
        }

        private (DGraph, Dictionary<int, int>) GenerateTopology(int totalNodes)
        {
            var graph = new DGraph();
            var nodeDepths = new Dictionary<int, int>();
            if (totalNodes < 1) return (graph, nodeDepths);
            var existingEdges = new HashSet<Tuple<int, int>>();
            var nodesToProcess = new Queue<DGraphNode>();
            var firstNode = new DGraphNode { Id = 0, Type = "standard" };
            graph.Nodes.Add(firstNode);
            nodesToProcess.Enqueue(firstNode);
            nodeDepths[firstNode.Id] = 0;
            int nextNodeId = 1;
            while (nodesToProcess.Count > 0 && nextNodeId < totalNodes)
            {
                var currentNode = nodesToProcess.Dequeue();
                int childrenCount = (nextNodeId < totalNodes - 2 && _random.NextDouble() > 0.3) ? _random.Next(1, 3) : 1;
                for (int i = 0; i < childrenCount && nextNodeId < totalNodes; i++)
                {
                    var newNode = new DGraphNode { Id = nextNodeId, Type = "standard" };
                    graph.Nodes.Add(newNode);
                    AddEdge(graph, existingEdges, currentNode.Id, newNode.Id);
                    nodeDepths[newNode.Id] = nodeDepths[currentNode.Id] + 1;
                    if (_random.NextDouble() < LocalMergeChance)
                    {
                        var potentialMergeNodes = graph.Nodes.Where(n => n.Id != newNode.Id && n.Id != currentNode.Id && Math.Abs(nodeDepths.GetValueOrDefault(n.Id, -99) - nodeDepths[newNode.Id]) <= 1 && !AreNodesConnected(existingEdges, newNode.Id, n.Id)).ToList();
                        if (potentialMergeNodes.Any()) AddEdge(graph, existingEdges, newNode.Id, potentialMergeNodes[_random.Next(potentialMergeNodes.Count)].Id);
                    }
                    nodesToProcess.Enqueue(newNode);
                    nextNodeId++;
                }
            }
            int globalMergesToAdd = (int)(totalNodes * GlobalMergeFactor);
            int safetyBreak = 0;
            for (int i = 0; i < globalMergesToAdd && safetyBreak < 100; i++)
            {
                var nodeA = graph.Nodes[_random.Next(graph.Nodes.Count)];
                var nodeB = graph.Nodes[_random.Next(graph.Nodes.Count)];
                if (nodeA.Id == nodeB.Id || AreNodesConnected(existingEdges, nodeA.Id, nodeB.Id)) { i--; safetyBreak++; continue; }
                AddEdge(graph, existingEdges, nodeA.Id, nodeB.Id);
            }
            return (graph, nodeDepths);
        }

        private void AddEdge(DGraph graph, HashSet<Tuple<int, int>> existingEdges, int id1, int id2)
        {
            graph.Edges.Add(new DGraphEdge { Source = id1, Target = id2 });
            existingEdges.Add(new Tuple<int, int>(Math.Min(id1, id2), Math.Max(id1, id2)));
        }

        private bool AreNodesConnected(HashSet<Tuple<int, int>> existingEdges, int id1, int id2)
        {
            return existingEdges.Contains(new Tuple<int, int>(Math.Min(id1, id2), Math.Max(id1, id2)));
        }
    }
}