using System;
using System.Collections.Generic;
using System.Drawing;

namespace DGraphBuilder.Generation
{
    /// <summary>
    /// Détecte les collisions entre polygones convexes en utilisant le Théorème de l'Axe de Séparation (SAT).
    /// </summary>
    public static class CollisionDetector
    {
        private class Projection
        {
            public float Min { get; }
            public float Max { get; }
            public Projection(float min, float max) { Min = min; Max = max; }
            public bool Overlaps(Projection other) => Max > other.Min && other.Max > Min;
        }

        public static bool ArePolygonsOverlapping(Polygon p1, Polygon p2)
        {
            var axes = new List<PointF>();
            axes.AddRange(GetAxes(p1));
            axes.AddRange(GetAxes(p2));

            foreach (var axis in axes)
            {
                var p1Projection = Project(p1, axis);
                var p2Projection = Project(p2, axis);

                if (!p1Projection.Overlaps(p2Projection))
                {
                    return false; // Un axe de séparation a été trouvé, pas de collision.
                }
            }

            return true; // Aucun axe de séparation trouvé, les polygones se chevauchent.
        }

        private static List<PointF> GetAxes(Polygon polygon)
        {
            var axes = new List<PointF>();
            for (int i = 0; i < polygon.Vertices.Count; i++)
            {
                PointF p1 = polygon.Vertices[i];
                PointF p2 = polygon.Vertices[i + 1 == polygon.Vertices.Count ? 0 : i + 1];

                PointF edge = new PointF(p1.X - p2.X, p1.Y - p2.Y);
                PointF normal = new PointF(-edge.Y, edge.X); // Axe perpendiculaire

                // Normaliser le vecteur (le rendre de longueur 1)
                float length = (float)Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
                if (length > 0)
                {
                    axes.Add(new PointF(normal.X / length, normal.Y / length));
                }
            }
            return axes;
        }

        private static Projection Project(Polygon polygon, PointF axis)
        {
            float min = DotProduct(axis, polygon.Vertices[0]);
            float max = min;

            for (int i = 1; i < polygon.Vertices.Count; i++)
            {
                float p = DotProduct(axis, polygon.Vertices[i]);
                if (p < min)
                    min = p;
                else if (p > max)
                    max = p;
            }
            return new Projection(min, max);
        }

        private static float DotProduct(PointF p1, PointF p2) => (p1.X * p2.X) + (p1.Y * p2.Y);
    }
}