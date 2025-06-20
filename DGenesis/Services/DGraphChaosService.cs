using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphChaosService
    {
        private class Vector { public double X { get; set; } public double Y { get; set; } }

        public void ApplyChaos(DGraph graph, int iterations = 50, double minDistance = 75.0)
        {
            if (graph.Nodes.Count < 2) return;
            var nodeDict = graph.Nodes.ToDictionary(n => n.Id);

            Console.WriteLine("... Application des forces (Relaxation Organique)");
            ApplyForceDirectedLayout(graph, nodeDict, iterations);

            Console.WriteLine("... Application de la normalisation des distances");
            EnforceMinimumDistance(graph, minDistance, 10);
        }

        private void ApplyForceDirectedLayout(DGraph graph, IReadOnlyDictionary<int, DGraphNode> nodeDict, int iterations)
        {
            double area = 500 * graph.Nodes.Count;
            double k = 1.2 * Math.Sqrt(area / graph.Nodes.Count);

            for (int i = 0; i < iterations; i++)
            {
                var displacements = graph.Nodes.ToDictionary(n => n.Id, n => new Vector());

                foreach (var v in graph.Nodes)
                {
                    foreach (var u in graph.Nodes)
                    {
                        if (v.Id == u.Id) continue;
                        double deltaX = v.Position.X - u.Position.X;
                        double deltaY = v.Position.Y - u.Position.Y;
                        double distanceSq = deltaX * deltaX + deltaY * deltaY;
                        double distance = Math.Max(0.1, Math.Sqrt(distanceSq));
                        double repulsiveForce = (k * k) / distance;
                        displacements[v.Id].X += (deltaX / distance) * repulsiveForce;
                        displacements[v.Id].Y += (deltaY / distance) * repulsiveForce;
                    }
                }

                foreach (var edge in graph.Edges)
                {
                    var source = nodeDict[edge.Source];
                    var target = nodeDict[edge.Target];
                    double deltaX = source.Position.X - target.Position.X;
                    double deltaY = source.Position.Y - target.Position.Y;
                    double distanceSq = deltaX * deltaX + deltaY * deltaY;
                    double attractiveForce = distanceSq / k;
                    double distance = Math.Max(1.0, Math.Sqrt(distanceSq));
                    displacements[source.Id].X -= (deltaX / distance) * attractiveForce;
                    displacements[source.Id].Y -= (deltaY / distance) * attractiveForce;
                    displacements[target.Id].X += (deltaX / distance) * attractiveForce;
                    displacements[target.Id].Y += (deltaY / distance) * attractiveForce;
                }

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

        private void EnforceMinimumDistance(DGraph graph, double minDistance, int iterations)
        {
            double minDistanceSq = minDistance * minDistance;

            for (int i = 0; i < iterations; i++)
            {
                foreach (var nodeA in graph.Nodes)
                {
                    foreach (var nodeB in graph.Nodes)
                    {
                        if (nodeA.Id >= nodeB.Id) continue;
                        double deltaX = nodeA.Position.X - nodeB.Position.X;
                        double deltaY = nodeA.Position.Y - nodeB.Position.Y;
                        double distanceSq = deltaX * deltaX + deltaY * deltaY;

                        if (distanceSq < minDistanceSq)
                        {
                            double distance = Math.Sqrt(distanceSq);
                            double pushFactor = (minDistance - distance) / distance * 0.5;
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