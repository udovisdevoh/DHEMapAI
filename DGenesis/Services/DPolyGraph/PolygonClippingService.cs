using DGenesis.Models.DPolyGraph;
using DGenesis.Models.Geometry;
using System;
using System.Collections.Generic;

namespace DGenesis.Services.Geometric
{
    public class PolygonClippingService
    {
        // On conserve la structure logique correcte de la méthode Clip...
        public List<DPolyVertex> Clip(List<DPolyVertex> subjectPolygon, Line clipEdge)
        {
            var outputList = new List<DPolyVertex>();
            if (subjectPolygon == null || subjectPolygon.Count == 0)
            {
                return outputList;
            }

            var s = subjectPolygon[subjectPolygon.Count - 1];

            foreach (var e in subjectPolygon)
            {
                var s_is_inside = IsInside(clipEdge, s);
                var e_is_inside = IsInside(clipEdge, e);

                if (e_is_inside)
                {
                    if (!s_is_inside)
                    {
                        var intersection = Intersection(s, e, clipEdge.Point1, clipEdge.Point2);
                        if (intersection != null) outputList.Add(intersection);
                    }
                    outputList.Add(e);
                }
                else if (s_is_inside)
                {
                    var intersection = Intersection(s, e, clipEdge.Point1, clipEdge.Point2);
                    if (intersection != null) outputList.Add(intersection);
                }

                s = e;
            }

            return outputList;
        }

        private bool IsInside(Line edge, DPolyVertex p)
        {
            return (edge.Point2.X - edge.Point1.X) * (p.Y - edge.Point1.Y) - (edge.Point2.Y - edge.Point1.Y) * (p.X - edge.Point1.X) >= 0;
        }

        // ... mais on revient à l'implémentation cartésienne de l'intersection, plus stable.
        private DPolyVertex Intersection(DPolyVertex s, DPolyVertex e, DPolyVertex clipP1, DPolyVertex clipP2)
        {
            // Ligne de l'arête du polygone (s, e)
            double a1 = e.Y - s.Y;
            double b1 = s.X - e.X;
            double c1 = a1 * s.X + b1 * s.Y;

            // Ligne de coupe (clipP1, clipP2)
            double a2 = clipP2.Y - clipP1.Y;
            double b2 = clipP1.X - clipP2.X;
            double c2 = a2 * clipP1.X + b2 * clipP1.Y;

            double det = a1 * b2 - a2 * b1;
            if (Math.Abs(det) < 1e-9)
            {
                return null; // Lignes parallèles
            }

            double x = (b2 * c1 - b1 * c2) / det;
            double y = (a1 * c2 - a2 * c1) / det;

            return new DPolyVertex { X = x, Y = y };
        }
    }
}