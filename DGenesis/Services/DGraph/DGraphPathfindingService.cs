using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Collections.Immutable; // <-- NÉCESSAIRE pour ImmutableHashSet
using System.Linq;

namespace DGenesis.Services
{
    // Représente l'état complet d'un chemin à un instant T
    public class PathState
    {
        public int CurrentNodeId { get; }
        public ImmutableHashSet<int> CollectedKeys { get; }
        public List<int> Path { get; }
        public double Cost { get; } // g(n) - Coût pour arriver ici
        public double Heuristic { get; } // h(n) - Coût estimé pour arriver à la fin

        public double F => Cost + Heuristic; // f(n) - Coût total estimé

        public PathState(int nodeId, ImmutableHashSet<int> keys, List<int> path, double cost, double heuristic)
        {
            CurrentNodeId = nodeId;
            CollectedKeys = keys;
            Path = path;
            Cost = cost;
            Heuristic = heuristic;
        }
    }

    // Implémentation simple d'une file de priorité pour la compatibilité avec d'anciennes versions de .NET
    internal class SimplePriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        private readonly List<Tuple<TElement, TPriority>> _elements = new List<Tuple<TElement, TPriority>>();

        public int Count => _elements.Count;

        public void Enqueue(TElement item, TPriority priority)
        {
            _elements.Add(Tuple.Create(item, priority));
        }

        public TElement Dequeue()
        {
            if (_elements.Count == 0) throw new InvalidOperationException("The queue is empty.");

            int bestIndex = 0;
            for (int i = 1; i < _elements.Count; i++)
            {
                if (_elements[i].Item2.CompareTo(_elements[bestIndex].Item2) < 0)
                {
                    bestIndex = i;
                }
            }

            TElement bestItem = _elements[bestIndex].Item1;
            _elements.RemoveAt(bestIndex);
            return bestItem;
        }
    }

    public class DGraphPathfindingService
    {
        /// <summary>
        /// NOUVELLE MÉTHODE : Calcule la distance (en nombre d'arêtes) depuis un nœud de départ vers tous les autres nœuds accessibles.
        /// </summary>
        /// <returns>Un dictionnaire associant chaque ID de nœud à sa distance du départ.</returns>
        public Dictionary<int, int> FindAllDistances(IReadOnlyDictionary<int, List<int>> adjacencyList, int startNodeId)
        {
            var distances = new Dictionary<int, int>();
            if (!adjacencyList.ContainsKey(startNodeId)) return distances;

            var queue = new Queue<int>();
            queue.Enqueue(startNodeId);
            distances[startNodeId] = 0;

            while (queue.Count > 0)
            {
                int currentNodeId = queue.Dequeue();
                foreach (var neighborId in adjacencyList[currentNodeId])
                {
                    if (!distances.ContainsKey(neighborId))
                    {
                        distances[neighborId] = distances[currentNodeId] + 1;
                        queue.Enqueue(neighborId);
                    }
                }
            }
            return distances;
        }

        public List<int> FindSolvablePath(DGraph graph, int startNodeId, int endNodeId)
        {
            var nodeDict = graph.Nodes.ToDictionary(n => n.Id);
            if (!nodeDict.ContainsKey(startNodeId) || !nodeDict.ContainsKey(endNodeId)) return new List<int>();

            var adjacencyList = BuildAdjacencyList(graph);
            var endPosition = nodeDict[endNodeId].Position;

            var priorityQueue = new SimplePriorityQueue<PathState, double>();
            var visitedStates = new HashSet<Tuple<int, ImmutableHashSet<int>>>();
            var initialHeuristic = GetHeuristic(nodeDict[startNodeId].Position, endPosition);
            var initialState = new PathState(startNodeId, ImmutableHashSet<int>.Empty, new List<int> { startNodeId }, 0, initialHeuristic);

            priorityQueue.Enqueue(initialState, initialState.F);

            while (priorityQueue.Count > 0)
            {
                var currentState = priorityQueue.Dequeue();
                if (currentState.CurrentNodeId == endNodeId) return currentState.Path;

                var stateKey = Tuple.Create(currentState.CurrentNodeId, currentState.CollectedKeys);
                if (visitedStates.Contains(stateKey)) continue;
                visitedStates.Add(stateKey);

                var currentNode = nodeDict[currentState.CurrentNodeId];
                var newKeys = currentState.CollectedKeys;
                if (currentNode.Unlocks != null && currentNode.Unlocks.Any())
                {
                    newKeys = newKeys.Union(currentNode.Unlocks);
                }

                foreach (var neighborId in adjacencyList[currentState.CurrentNodeId])
                {
                    var neighborNode = nodeDict[neighborId];
                    if (neighborNode.Type == "locked" && !newKeys.Contains(neighborNode.Id)) continue;

                    var newPath = new List<int>(currentState.Path) { neighborId };
                    var newCost = currentState.Cost + 1;
                    var newHeuristic = GetHeuristic(neighborNode.Position, endPosition);
                    var nextState = new PathState(neighborId, newKeys, newPath, newCost, newHeuristic);

                    var nextStateKey = Tuple.Create(nextState.CurrentNodeId, nextState.CollectedKeys);
                    if (!visitedStates.Contains(nextStateKey))
                    {
                        priorityQueue.Enqueue(nextState, nextState.F);
                    }
                }
            }
            return new List<int>();
        }

        private double GetHeuristic(Position from, Position to)
        {
            return Math.Sqrt(Math.Pow(from.X - to.X, 2) + Math.Pow(from.Y - to.Y, 2));
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