using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphUntanglerService
    {
        public bool TryUntangleGraph(DGraph graph)
        {
            bool intersectionFoundInPass;
            int maxIterations = graph.Edges.Count * 2;
            int iterations = 0;

            do
            {
                if (iterations >= maxIterations)
                {
                    Console.WriteLine($"[ATTENTION] L'algorithme de désenchevêtrement a atteint la limite de {maxIterations} itérations. Abandon.");
                    return false;
                }

                // --- CORRECTION ---
                // Le dictionnaire est maintenant recréé à chaque passe.
                // Cela garantit qu'il inclut les nœuds ajoutés lors de la passe précédente.
                var nodeDict = graph.Nodes.ToDictionary(n => n.Id);

                intersectionFoundInPass = FindAndFixFirstIntersection(graph, nodeDict);
                iterations++;
            }
            while (intersectionFoundInPass);

            return true;
        }

        private bool FindAndFixFirstIntersection(DGraph graph, IReadOnlyDictionary<int, DGraphNode> nodeDict)
        {
            var edges = graph.Edges.ToList();
            for (int i = 0; i < edges.Count; i++)
            {
                for (int j = i + 1; j < edges.Count; j++)
                {
                    var edge1 = edges[i];
                    var edge2 = edges[j];

                    // Vérifier si les IDs existent avant de les utiliser, par sécurité
                    if (!nodeDict.ContainsKey(edge1.Source) || !nodeDict.ContainsKey(edge1.Target) ||
                        !nodeDict.ContainsKey(edge2.Source) || !nodeDict.ContainsKey(edge2.Target))
                    {
                        continue;
                    }

                    var p1 = nodeDict[edge1.Source].Position;
                    var q1 = nodeDict[edge1.Target].Position;
                    var p2 = nodeDict[edge2.Source].Position;
                    var q2 = nodeDict[edge2.Target].Position;

                    if (edge1.Source == edge2.Source || edge1.Source == edge2.Target || edge1.Target == edge2.Source || edge1.Target == edge2.Target)
                    {
                        continue;
                    }

                    Position intersectionPoint;
                    if (DoLineSegmentsIntersect(p1, q1, p2, q2, out intersectionPoint))
                    {
                        int newId = graph.Nodes.Max(n => n.Id) + 1;
                        var newNode = new DGraphNode
                        {
                            Id = newId,
                            Type = "standard",
                            Position = intersectionPoint
                        };
                        graph.Nodes.Add(newNode);

                        graph.Edges.Remove(edge2);
                        graph.Edges.Add(new DGraphEdge { Source = edge2.Source, Target = newId });
                        graph.Edges.Add(new DGraphEdge { Source = newId, Target = edge2.Target });

                        return true;
                    }
                }
            }
            return false;
        }

        private bool DoLineSegmentsIntersect(Position p1, Position q1, Position p2, Position q2, out Position intersectionPoint)
        {
            intersectionPoint = null;
            double a1 = q1.Y - p1.Y;
            double b1 = p1.X - q1.X;
            double c1 = a1 * p1.X + b1 * p1.Y;
            double a2 = q2.Y - p2.Y;
            double b2 = p2.X - q2.X;
            double c2 = a2 * p2.X + b2 * p2.Y;
            double determinant = a1 * b2 - a2 * b1;

            if (Math.Abs(determinant) < 1e-9) return false;

            double x = (b2 * c1 - b1 * c2) / determinant;
            double y = (a1 * c2 - a2 * c1) / determinant;
            double epsilon = 1e-9;

            bool onSegment1 = (x >= Math.Min(p1.X, q1.X) - epsilon && x <= Math.Max(p1.X, q1.X) + epsilon) && (y >= Math.Min(p1.Y, q1.Y) - epsilon && y <= Math.Max(p1.Y, q1.Y) + epsilon);
            bool onSegment2 = (x >= Math.Min(p2.X, q2.X) - epsilon && x <= Math.Max(p2.X, q2.X) + epsilon) && (y >= Math.Min(p2.Y, q2.Y) - epsilon && y <= Math.Max(p2.Y, q2.Y) + epsilon);

            if (onSegment1 && onSegment2)
            {
                intersectionPoint = new Position { X = x, Y = y };
                return true;
            }
            return false;
        }
    }
}