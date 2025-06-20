using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphChaosService
    {
        private class Vector { public double X { get; set; } public double Y { get; set; } }

        public void ApplyChaos(DGraph graph, int iterations = 100) // Augmentation des itérations pour un meilleur équilibre
        {
            if (graph.Nodes.Count < 3) return;

            var nodeDict = graph.Nodes.ToDictionary(n => n.Id);

            // --- NOUVELLE LOGIQUE D'ANCRAGE ---
            var anchorIds = new HashSet<int>();
            var firstNode = graph.Nodes.First();
            var lastNode = graph.Nodes.Last(); // Souvent topologiquement distants

            anchorIds.Add(firstNode.Id);
            anchorIds.Add(lastNode.Id);

            // Placer les ancres à des positions fixes et opposées
            double layoutWidth = graph.Nodes.Count * 40;
            firstNode.Position = new Position { X = -layoutWidth / 2, Y = 0 };
            lastNode.Position = new Position { X = layoutWidth / 2, Y = 0 };
            // --- FIN DE LA LOGIQUE D'ANCRAGE ---

            double area = layoutWidth * layoutWidth;
            double k = 1.5 * Math.Sqrt(area / graph.Nodes.Count); // 'k' plus grand pour plus d'espace

            for (int i = 0; i < iterations; i++)
            {
                var displacements = graph.Nodes.ToDictionary(n => n.Id, n => new Vector());

                // Répulsion
                foreach (var v in graph.Nodes)
                {
                    foreach (var u in graph.Nodes)
                    {
                        if (v.Id == u.Id) continue;
                        double deltaX = v.Position.X - u.Position.X;
                        double deltaY = v.Position.Y - u.Position.Y;
                        double distance = Math.Max(0.1, Math.Sqrt(deltaX * deltaX + deltaY * deltaY));
                        double repulsiveForce = (k * k) / distance;
                        displacements[v.Id].X += (deltaX / distance) * repulsiveForce;
                        displacements[v.Id].Y += (deltaY / distance) * repulsiveForce;
                    }
                }

                // Attraction
                foreach (var edge in graph.Edges)
                {
                    var source = nodeDict[edge.Source];
                    var target = nodeDict[edge.Target];
                    double deltaX = source.Position.X - target.Position.X;
                    double deltaY = source.Position.Y - target.Position.Y;
                    double distance = Math.Max(1.0, Math.Sqrt(deltaX * deltaX + deltaY * deltaY));
                    double attractiveForce = (distance * distance) / k;
                    displacements[source.Id].X -= (deltaX / distance) * attractiveForce;
                    displacements[source.Id].Y -= (deltaY / distance) * attractiveForce;
                    displacements[target.Id].X += (deltaX / distance) * attractiveForce;
                    displacements[target.Id].Y += (deltaY / distance) * attractiveForce;
                }

                // Appliquer les déplacements, SAUF POUR LES ANCRES
                foreach (var node in graph.Nodes)
                {
                    if (anchorIds.Contains(node.Id)) continue; // On ne déplace pas les ancres

                    var displacement = displacements[node.Id];
                    double displacementMagnitude = Math.Sqrt(displacement.X * displacement.X + displacement.Y * displacement.Y);
                    if (displacementMagnitude > 0)
                    {
                        // Utilisation d'une "température" qui diminue pour stabiliser le layout
                        double temperature = layoutWidth / 10.0 * (1.0 - (double)i / iterations);
                        double limitedForce = Math.Min(displacementMagnitude, temperature);
                        node.Position.X += (displacement.X / displacementMagnitude) * limitedForce;
                        node.Position.Y += (displacement.Y / displacementMagnitude) * limitedForce;
                    }
                }
            }
            // La normalisation des distances Nœud-Nœud est maintenant implicite dans l'algorithme
        }
    }
}