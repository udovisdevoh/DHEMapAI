using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DShapeGeneratorService
    {
        private readonly Random _random = new Random();

        public DShape Generate(int vertexCount, int symmetryAxes, double size, double irregularity)
        {
            var shape = new DShape
            {
                Name = $"random_shape_{DateTime.Now.Ticks}",
                Description = $"Shape with {vertexCount} vertices, {symmetryAxes} symmetry axes, size {size}, and irregularity {irregularity}."
            };

            shape.Vertices = GenerateVertices(vertexCount, symmetryAxes, size, irregularity);

            if (IsPolygonSelfIntersecting(shape.Vertices))
            {
                shape.Description += " [WARNING: Self-intersecting]";
            }

            return shape;
        }

        private List<DShapeVertex> GenerateVertices(int vertexCount, int symmetryAxes, double size, double irregularity)
        {
            var vertices = new List<DShapeVertex>();
            if (vertexCount < 3) return vertices;

            symmetryAxes = Math.Max(0, symmetryAxes);

            // --- NOUVELLE LOGIQUE DÉDIÉE POUR 1 AXE DE SYMÉTRIE ---
            if (symmetryAxes == 1)
            {
                var rightSidePoints = new List<DShapeVertex>();
                // Le nombre de points à générer sur un côté (sans compter les ancrages sur l'axe)
                int pointsPerSide = (vertexCount - (vertexCount % 2 == 0 ? 2 : 1)) / 2;

                // Point d'ancrage supérieur sur l'axe Y
                vertices.Add(new DShapeVertex { X = 0, Y = size * (1 - _random.NextDouble() * irregularity * 0.5) });

                // Générer les points du côté droit, du haut vers le bas
                if (pointsPerSide > 0)
                {
                    double angleStep = Math.PI / (pointsPerSide + 1);
                    for (int i = 1; i <= pointsPerSide; i++)
                    {
                        double angle = (Math.PI / 2) - (i * angleStep) + (angleStep * (_random.NextDouble() - 0.5) * irregularity);
                        double radius = size * (1 - _random.NextDouble() * irregularity);
                        rightSidePoints.Add(new DShapeVertex { X = radius * Math.Cos(angle), Y = radius * Math.Sin(angle) });
                    }
                    vertices.AddRange(rightSidePoints);
                }

                // Point d'ancrage inférieur (seulement si le nombre de sommets est pair)
                if (vertexCount % 2 == 0)
                {
                    vertices.Add(new DShapeVertex { X = 0, Y = -size * (1 - _random.NextDouble() * irregularity * 0.5) });
                }

                // Ajouter les points du côté gauche (miroirs des points du côté droit, en ordre INVERSE)
                for (int i = rightSidePoints.Count - 1; i >= 0; i--)
                {
                    vertices.Add(new DShapeVertex { X = -rightSidePoints[i].X, Y = rightSidePoints[i].Y });
                }
            }
            // --- FIN DE LA NOUVELLE LOGIQUE ---
            else if (symmetryAxes > 1)
            {
                // Génération symétrique radiale (inchangée)
                int baseVerticesCount = (int)Math.Ceiling((double)vertexCount / symmetryAxes);
                var baseVertices = new List<DShapeVertex>();
                double angleStep = (2 * Math.PI / symmetryAxes) / baseVerticesCount;

                for (int i = 0; i < baseVerticesCount; i++)
                {
                    double radius = size * (1 - _random.NextDouble() * irregularity);
                    double angle = i * angleStep;
                    baseVertices.Add(new DShapeVertex { X = radius * Math.Cos(angle), Y = radius * Math.Sin(angle) });
                }

                for (int i = 0; i < symmetryAxes; i++)
                {
                    double rotationAngle = i * (2 * Math.PI / symmetryAxes);
                    foreach (var baseVertex in baseVertices)
                    {
                        if (vertices.Count >= vertexCount) break;
                        double x = baseVertex.X * Math.Cos(rotationAngle) - baseVertex.Y * Math.Sin(rotationAngle);
                        double y = baseVertex.X * Math.Sin(rotationAngle) + baseVertex.Y * Math.Cos(rotationAngle);
                        vertices.Add(new DShapeVertex { X = x, Y = y });
                    }
                }
            }
            else // Génération non-symétrique (0 axe)
            {
                double angleStep = 2 * Math.PI / vertexCount;
                for (int i = 0; i < vertexCount; i++)
                {
                    double radius = size * (1 - _random.NextDouble() * irregularity);
                    double angle = i * angleStep + (angleStep * (_random.NextDouble() - 0.5) * irregularity);
                    vertices.Add(new DShapeVertex { X = radius * Math.Cos(angle), Y = radius * Math.Sin(angle) });
                }
            }

            return vertices.Select(v => new DShapeVertex { X = Math.Round(v.X, 2), Y = Math.Round(v.Y, 2) }).ToList();
        }

        private bool IsPolygonSelfIntersecting(List<DShapeVertex> vertices)
        {
            if (vertices.Count <= 3) return false;
            for (int i = 0; i < vertices.Count; i++)
            {
                var p1 = vertices[i];
                var q1 = vertices[(i + 1) % vertices.Count];
                for (int j = i + 1; j < vertices.Count; j++)
                {
                    if ((i == 0 && j == vertices.Count - 1) || (j + 1) % vertices.Count == i) continue;

                    var p2 = vertices[j];
                    var q2 = vertices[(j + 1) % vertices.Count];
                    if (DoLineSegmentsIntersect(p1, q1, p2, q2)) return true;
                }
            }
            return false;
        }

        private bool DoLineSegmentsIntersect(DShapeVertex p1, DShapeVertex q1, DShapeVertex p2, DShapeVertex q2)
        {
            static int Orientation(DShapeVertex p, DShapeVertex q, DShapeVertex r)
            {
                double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
                if (Math.Abs(val) < 1e-10) return 0;
                return (val > 0) ? 1 : 2;
            }

            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);

            if (o1 != 0 && o2 != 0 && o3 != 0 && o4 != 0)
            {
                if (o1 != o2 && o3 != o4) return true;
            }

            return false;
        }
    }
}