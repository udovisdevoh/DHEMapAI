using DGenesis.Models.DGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGraphFinalizeService
    {
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