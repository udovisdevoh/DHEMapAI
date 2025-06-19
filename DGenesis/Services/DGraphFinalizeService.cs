using DGenesis.Models.DGraph;
using System;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphFinalizeService
    {
        /// <summary>
        /// Repositionne localement les nœuds trop proches des arêtes.
        /// C'est une opération de "polissage" qui peut créer des croisements, 
        /// elle doit donc être suivie par le UntanglerService.
        /// </summary>
        public void EnforceNodeEdgeSpacing(DGraph graph, double minPadding = 35.0, int iterations = 5)
        {
            if (graph.Nodes.Count < 3) return;

            for (int i = 0; i < iterations; i++)
            {
                // On doit stocker les ajustements pour les appliquer tous en même temps à la fin de l'itération
                var adjustments = graph.Nodes.ToDictionary(n => n.Id, n => new { dX = 0.0, dY = 0.0 });

                foreach (var node in graph.Nodes)
                {
                    foreach (var edge in graph.Edges)
                    {
                        if (edge.Source == node.Id || edge.Target == node.Id) continue;

                        var p1 = graph.Nodes.First(n => n.Id == edge.Source).Position;
                        var p2 = graph.Nodes.First(n => n.Id == edge.Target).Position;

                        double distance = PointToLineSegmentDistance(node.Position, p1, p2, out var projection);

                        if (distance < minPadding)
                        {
                            // Le nœud est trop proche. On calcule un vecteur de poussée.
                            double pushMagnitude = (minPadding - distance) * 0.5; // 0.5 pour un ajustement plus doux

                            double pushVectorX = node.Position.X - projection.X;
                            double pushVectorY = node.Position.Y - projection.Y;
                            double vectorLength = Math.Sqrt(pushVectorX * pushVectorX + pushVectorY * pushVectorY);

                            if (vectorLength > 0.01)
                            {
                                // On ajoute la force de poussée aux ajustements du nœud
                                var currentAdjustment = adjustments[node.Id];
                                adjustments[node.Id] = new
                                {
                                    dX = currentAdjustment.dX + (pushVectorX / vectorLength) * pushMagnitude,
                                    dY = currentAdjustment.dY + (pushVectorY / vectorLength) * pushMagnitude
                                };
                            }
                        }
                    }
                }

                // Appliquer tous les ajustements calculés pour cette itération
                foreach (var node in graph.Nodes)
                {
                    node.Position.X += adjustments[node.Id].dX;
                    node.Position.Y += adjustments[node.Id].dY;
                }
            }
        }

        private double PointToLineSegmentDistance(Position p, Position p1, Position p2, out Position closestPoint)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            if (dx == 0 && dy == 0) // Le segment est un point
            {
                closestPoint = p1;
                return Math.Sqrt(Math.Pow(p.X - p1.X, 2) + Math.Pow(p.Y - p1.Y, 2));
            }

            double t = ((p.X - p1.X) * dx + (p.Y - p1.Y) * dy) / (dx * dx + dy * dy);

            if (t < 0)
            {
                closestPoint = p1;
            }
            else if (t > 1)
            {
                closestPoint = p2;
            }
            else
            {
                closestPoint = new Position { X = p1.X + t * dx, Y = p1.Y + t * dy };
            }

            return Math.Sqrt(Math.Pow(p.X - closestPoint.X, 2) + Math.Pow(p.Y - closestPoint.Y, 2));
        }
    }
}