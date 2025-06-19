using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphChaosService
    {
        // Classe interne pour stocker les vecteurs de déplacement
        private class Vector
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        /// <summary>
        /// Applique un algorithme à forces dirigées pour rendre la disposition plus organique,
        /// suivi d'une passe de normalisation pour garantir une distance minimale entre les nœuds.
        /// </summary>
        public void ApplyChaos(DGraph graph, int iterations = 50, double minDistance = 75.0)
        {
            if (graph.Nodes.Count < 2) return;

            // --- PHASE 1: RELAXATION ORGANIQUE ---
            Console.WriteLine("... Application des forces (Relaxation Organique)");
            ApplyForceDirectedLayout(graph, iterations);

            // --- PHASE 2: NORMALISATION DES DISTANCES ---
            Console.WriteLine("... Application de la normalisation des distances");
            EnforceMinimumDistance(graph, minDistance, 10); // 10 itérations de polissage devraient suffire
        }

        private void ApplyForceDirectedLayout(DGraph graph, int iterations)
        {
            double area = 500 * graph.Nodes.Count;
            // On augmente la valeur de 'k' pour forcer un espacement plus grand.
            // Un facteur de 1.2 donne 20% d'espacement en plus.
            double k = 1.2 * Math.Sqrt(area / graph.Nodes.Count);

            for (int i = 0; i < iterations; i++)
            {
                var displacements = new Dictionary<int, Vector>();
                foreach (var node in graph.Nodes) { displacements[node.Id] = new Vector(); }

                // Calcul des forces de répulsion
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

                // Calcul des forces d'attraction
                foreach (var edge in graph.Edges)
                {
                    var source = graph.Nodes.First(n => n.Id == edge.Source);
                    var target = graph.Nodes.First(n => n.Id == edge.Target);
                    double deltaX = source.Position.X - target.Position.X;
                    double deltaY = source.Position.Y - target.Position.Y;
                    double distance = Math.Max(1.0, Math.Sqrt(deltaX * deltaX + deltaY * deltaY));
                    double attractiveForce = (distance * distance) / k;
                    displacements[source.Id].X -= (deltaX / distance) * attractiveForce;
                    displacements[source.Id].Y -= (deltaY / distance) * attractiveForce;
                    displacements[target.Id].X += (deltaX / distance) * attractiveForce;
                    displacements[target.Id].Y += (deltaY / distance) * attractiveForce;
                }

                // Appliquer les déplacements
                foreach (var node in graph.Nodes)
                {
                    var displacement = displacements[node.Id];
                    double displacementMagnitude = Math.Sqrt(displacement.X * displacement.X + displacement.Y * displacement.Y);
                    if (displacementMagnitude > 0)
                    {
                        double limitedForce = Math.Min(displacementMagnitude, 50.0);
                        node.Position.X += (displacement.X / displacementMagnitude) * limitedForce;
                        node.Position.Y += (displacement.Y / displacementMagnitude) * limitedForce;
                    }
                }
            }
        }

        /// <summary>
        /// Nouvelle méthode qui pousse les nœuds trop proches les uns des autres.
        /// </summary>
        private void EnforceMinimumDistance(DGraph graph, double minDistance, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                foreach (var nodeA in graph.Nodes)
                {
                    foreach (var nodeB in graph.Nodes)
                    {
                        if (nodeA.Id >= nodeB.Id) continue; // Évite de traiter chaque paire deux fois

                        double deltaX = nodeA.Position.X - nodeB.Position.X;
                        double deltaY = nodeA.Position.Y - nodeB.Position.Y;
                        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                        if (distance < minDistance)
                        {
                            // Les nœuds sont trop proches, on les pousse
                            double pushFactor = (minDistance - distance) / distance * 0.5; // 0.5 pour un mouvement plus doux

                            double pushX = deltaX * pushFactor;
                            double pushY = deltaY * pushFactor;

                            nodeA.Position.X += pushX;
                            nodeA.Position.Y += pushY;
                            nodeB.Position.X -= pushX;
                            nodeB.Position.Y -= pushY;
                        }
                    }
                }
            }
        }
    }
}