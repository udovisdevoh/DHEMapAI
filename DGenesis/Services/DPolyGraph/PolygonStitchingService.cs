using DGenesis.Models.DPolyGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services.Geometric
{
    public class PolygonStitchingService
    {
        // Coud un polygone de corridor à un polygone de pièce
        public List<DPolyVertex> StitchCorridorToPolygon(List<DPolyVertex> polygon, List<DPolyVertex> corridor)
        {
            // 1. Trouver l'arête du polygone la plus proche du centre du corridor
            var corridorCenter = new DPolyVertex { X = corridor.Average(v => v.X), Y = corridor.Average(v => v.Y) };

            int closestEdgeIndex = -1;
            double minDistanceSq = double.MaxValue;

            for (int i = 0; i < polygon.Count; i++)
            {
                var p1 = polygon[i];
                var p2 = polygon[(i + 1) % polygon.Count];
                var edgeMidpoint = new DPolyVertex { X = (p1.X + p2.X) / 2, Y = (p1.Y + p2.Y) / 2 };

                double distSq = Math.Pow(edgeMidpoint.X - corridorCenter.X, 2) + Math.Pow(edgeMidpoint.Y - corridorCenter.Y, 2);
                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    closestEdgeIndex = i;
                }
            }

            if (closestEdgeIndex == -1) return polygon;

            // 2. Trouver les deux sommets du corridor les plus proches de cette arête
            var edgeP1 = polygon[closestEdgeIndex];
            var edgeP2 = polygon[(closestEdgeIndex + 1) % polygon.Count];

            var corridorPointsSorted = corridor.OrderBy(cp =>
                PointToLineSegmentDistance(cp, edgeP1, edgeP2)
            ).Take(2).ToList();

            // 3. Insérer les deux points du corridor dans le polygone
            var newPolygon = new List<DPolyVertex>(polygon);

            // Il faut s'assurer que les points sont insérés dans le bon ordre (horaire/anti-horaire)
            var insertPoint1 = corridorPointsSorted[0];
            var insertPoint2 = corridorPointsSorted[1];

            // Heuristique simple pour l'ordre: on garde l'ordre de tri par angle autour du centre de l'arête
            var edgeCenter = new DPolyVertex { X = (edgeP1.X + edgeP2.X) / 2, Y = (edgeP1.Y + edgeP2.Y) / 2 };
            var sortedInsertPoints = corridorPointsSorted.OrderBy(p => Math.Atan2(p.Y - edgeCenter.Y, p.X - edgeCenter.X)).ToList();

            newPolygon.InsertRange(closestEdgeIndex + 1, sortedInsertPoints);

            // 4. Trier le polygone final pour s'assurer qu'il est toujours convexe et bien ordonné
            var finalCentroidX = newPolygon.Average(v => v.X);
            var finalCentroidY = newPolygon.Average(v => v.Y);

            return newPolygon.OrderBy(v => Math.Atan2(v.Y - finalCentroidY, v.X - finalCentroidX)).ToList();
        }

        private double PointToLineSegmentDistance(DPolyVertex p, DPolyVertex a, DPolyVertex b)
        {
            double l2 = Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2);
            if (l2 == 0.0) return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));
            double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y)) / l2));
            double projX = a.X + t * (b.X - a.X);
            double projY = a.Y + t * (b.Y - a.Y);
            return Math.Sqrt(Math.Pow(p.X - projX, 2) + Math.Pow(p.Y - projY, 2));
        }
    }
}