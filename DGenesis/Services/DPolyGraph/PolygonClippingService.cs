using DGenesis.Models.DPolyGraph;
using DGenesis.Models.Geometry;
using System;
using System.Collections.Generic;

namespace DGenesis.Services.Geometric
{
    public class PolygonClippingService
    {
        // Version finale corrigée de la méthode Clip
        public List<DPolyVertex> Clip(List<DPolyVertex> subjectPolygon, Line clipEdge)
        {
            var outputList = new List<DPolyVertex>();
            if (subjectPolygon == null || subjectPolygon.Count == 0)
            {
                return outputList;
            }

            var s = subjectPolygon[subjectPolygon.Count - 1]; // Le point de départ de l'arête

            foreach (var e in subjectPolygon) // Le point final de l'arête
            {
                var s_is_inside = IsInside(clipEdge, s);
                var e_is_inside = IsInside(clipEdge, e);

                // La structure if/else suivante est une implémentation correcte de l'algorithme.
                if (e_is_inside)
                {
                    if (!s_is_inside)
                    {
                        // L'arête entre dans la zone de clipping : on ajoute l'intersection
                        var intersection = Intersection(s, e, clipEdge.Point1, clipEdge.Point2);
                        if (intersection != null) outputList.Add(intersection);
                    }
                    // On ajoute le point final 'e' car il est à l'intérieur
                    outputList.Add(e);
                }
                else if (s_is_inside)
                {
                    // L'arête quitte la zone de clipping : on ajoute seulement l'intersection
                    var intersection = Intersection(s, e, clipEdge.Point1, clipEdge.Point2);
                    if (intersection != null) outputList.Add(intersection);
                }
                // Si s et e sont tous les deux à l'extérieur, on n'ajoute rien.

                s = e; // On passe à la prochaine arête
            }

            return outputList;
        }

        private bool IsInside(Line edge, DPolyVertex p)
        {
            return (edge.Point2.X - edge.Point1.X) * (p.Y - edge.Point1.Y) - (edge.Point2.Y - edge.Point1.Y) * (p.X - edge.Point1.X) >= 0;
        }

        private DPolyVertex Intersection(DPolyVertex p1, DPolyVertex p2, DPolyVertex p3, DPolyVertex p4)
        {
            // Ligne 1 (p1, p2), Ligne 2 (p3, p4)
            double det = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);

            if (Math.Abs(det) < 1e-9)
            {
                return null; // Lignes parallèles
            }

            double t_num = (p1.X - p3.X) * (p3.Y - p4.Y) - (p1.Y - p3.Y) * (p3.X - p4.X);
            double t = t_num / det;

            return new DPolyVertex
            {
                X = p1.X + t * (p2.X - p1.X),
                Y = p1.Y + t * (p2.Y - p1.Y)
            };
        }
    }
}