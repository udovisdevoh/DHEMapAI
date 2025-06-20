using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphFinalizeService
    {
        /// <summary>
        /// Repositionne localement les nœuds trop proches des arêtes.
        /// </summary>
        public void EnforceNodeEdgeSpacing(DGraph graph, double minPadding = 35.0, int iterations = 5)
        {
            if (graph.Nodes.Count < 3) return;
            var nodeDict = graph.Nodes.ToDictionary(n => n.Id);

            for (int i = 0; i < iterations; i++)
            {
                var adjustments = graph.Nodes.ToDictionary(n => n.Id, n => new { dX = 0.0, dY = 0.0 });

                foreach (var node in graph.Nodes)
                {
                    foreach (var edge in graph.Edges)
                    {
                        if (edge.Source == node.Id || edge.Target == node.Id) continue;
                        var p1 = nodeDict[edge.Source].Position;
                        var p2 = nodeDict[edge.Target].Position;
                        double distance = PointToLineSegmentDistance(node.Position, p1, p2, out var projection);

                        if (distance < minPadding)
                        {
                            double pushMagnitude = (minPadding - distance) * 0.5;
                            double pushVectorX = node.Position.X - projection.X;
                            double pushVectorY = node.Position.Y - projection.Y;
                            double vectorLength = Math.Max(0.01, Math.Sqrt(pushVectorX * pushVectorX + pushVectorY * pushVectorY));

                            var currentAdjustment = adjustments[node.Id];
                            adjustments[node.Id] = new
                            {
                                dX = currentAdjustment.dX + (pushVectorX / vectorLength) * pushMagnitude,
                                dY = currentAdjustment.dY + (pushVectorY / vectorLength) * pushMagnitude
                            };
                        }
                    }
                }
                foreach (var node in graph.Nodes)
                {
                    node.Position.X += adjustments[node.Id].dX;
                    node.Position.Y += adjustments[node.Id].dY;
                }
            }
        }

        /// <summary>
        /// NOUVELLE MÉTHODE : Centre et redimensionne le graphe pour une taille de vue cohérente.
        /// </summary>
        public void NormalizeAndCenter(DGraph graph, double targetAverageEdgeLength = 120.0)
        {
            if (graph.Edges.Count == 0) return;

            var nodeDict = graph.Nodes.ToDictionary(n => n.Id);

            // 1. Calculer la longueur moyenne actuelle des arêtes
            double currentAverageLength = graph.Edges.Average(edge => {
                var p1 = nodeDict[edge.Source].Position;
                var p2 = nodeDict[edge.Target].Position;
                return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
            });

            if (currentAverageLength < 1.0) return; // Éviter la division par zéro

            // 2. Calculer le facteur de mise à l'échelle
            double scaleFactor = targetAverageEdgeLength / currentAverageLength;

            // 3. Calculer le centre de masse actuel
            double avgX = graph.Nodes.Average(n => n.Position.X);
            double avgY = graph.Nodes.Average(n => n.Position.Y);

            // 4. Appliquer la transformation à chaque nœud
            foreach (var node in graph.Nodes)
            {
                // D'abord, centrer sur l'origine (0,0)
                double centeredX = node.Position.X - avgX;
                double centeredY = node.Position.Y - avgY;

                // Ensuite, appliquer la mise à l'échelle
                node.Position.X = centeredX * scaleFactor;
                node.Position.Y = centeredY * scaleFactor;
            }
        }

        private double PointToLineSegmentDistance(Position p, Position p1, Position p2, out Position closestPoint)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            if (dx == 0 && dy == 0)
            {
                closestPoint = p1;
                return Math.Sqrt(Math.Pow(p.X - p1.X, 2) + Math.Pow(p.Y - p1.Y, 2));
            }
            double t = ((p.X - p1.X) * dx + (p.Y - p1.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            closestPoint = new Position { X = p1.X + t * dx, Y = p1.Y + t * dy };
            return Math.Sqrt(Math.Pow(p.X - closestPoint.X, 2) + Math.Pow(p.Y - closestPoint.Y, 2));
        }
    }
}