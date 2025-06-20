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

            // On s'assure que le polygone n'est pas auto-intersectant, une étape importante.
            if (IsPolygonSelfIntersecting(shape.Vertices))
            {
                // Pour l'instant, on signale le problème. Une version future pourrait tenter de le corriger.
                shape.Description += " [WARNING: Self-intersecting]";
            }

            return shape;
        }

        private List<DShapeVertex> GenerateVertices(int vertexCount, int symmetryAxes, double size, double irregularity)
        {
            var vertices = new List<DShapeVertex>();
            if (vertexCount < 3) return vertices;

            symmetryAxes = Math.Max(0, symmetryAxes);

            if (symmetryAxes > 1)
            {
                // Génération symétrique
                int baseVerticesCount = (int)Math.Ceiling((double)vertexCount / symmetryAxes);
                var baseVertices = new List<DShapeVertex>();
                double angleStep = (2 * Math.PI / symmetryAxes) / baseVerticesCount;

                for (int i = 0; i < baseVerticesCount; i++)
                {
                    double radius = size * (1 - _random.NextDouble() * irregularity);
                    double angle = i * angleStep;
                    baseVertices.Add(new DShapeVertex { X = radius * Math.Cos(angle), Y = radius * Math.Sin(angle) });
                }

                // Appliquer la symétrie par rotation
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
            else // Génération non-symétrique ou avec 1 axe de symétrie (cas plus simple)
            {
                double angleStep = 2 * Math.PI / vertexCount;
                for (int i = 0; i < vertexCount; i++)
                {
                    double radius = size * (1 - _random.NextDouble() * irregularity);
                    double angle = i * angleStep + (angleStep * (_random.NextDouble() - 0.5) * irregularity);
                    vertices.Add(new DShapeVertex { X = radius * Math.Cos(angle), Y = radius * Math.Sin(angle) });
                }

                if (symmetryAxes == 1) // Appliquer une symétrie axiale simple sur l'axe Y
                {
                    var halfCount = (int)Math.Ceiling(vertexCount / 2.0);
                    for (int i = 1; i < halfCount; i++)
                    {
                        var mirroredIndex = vertexCount - i;
                        if (mirroredIndex > 0 && mirroredIndex < vertices.Count)
                        {
                            vertices[mirroredIndex].X = -vertices[i].X;
                            vertices[mirroredIndex].Y = vertices[i].Y;
                        }
                    }
                }
            }

            // Arrondir les valeurs pour un format plus propre
            return vertices.Select(v => new DShapeVertex { X = Math.Round(v.X, 2), Y = Math.Round(v.Y, 2) }).ToList();
        }

        // Fonction de base pour détecter les intersections (peut être améliorée)
        private bool IsPolygonSelfIntersecting(List<DShapeVertex> vertices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                var p1 = vertices[i];
                var q1 = vertices[(i + 1) % vertices.Count];
                for (int j = i + 1; j < vertices.Count; j++)
                {
                    // On ne teste pas les arêtes adjacentes
                    if ((j + 1) % vertices.Count == i) continue;

                    var p2 = vertices[j];
                    var q2 = vertices[(j + 1) % vertices.Count];
                    if (DoLineSegmentsIntersect(p1, q1, p2, q2)) return true;
                }
            }
            return false;
        }

        private bool DoLineSegmentsIntersect(DShapeVertex p1, DShapeVertex q1, DShapeVertex p2, DShapeVertex q2)
        {
            double o1 = Orientation(p1, q1, p2);
            double o2 = Orientation(p1, q1, q2);
            double o3 = Orientation(p2, q2, p1);
            double o4 = Orientation(p2, q2, q1);
            if (o1 != o2 && o3 != o4) return true;
            return false; // Ne gère pas les cas colinéaires pour rester simple
        }

        private int Orientation(DShapeVertex p, DShapeVertex q, DShapeVertex r)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            if (val == 0) return 0;  // Colinéaire
            return (val > 0) ? 1 : 2; // Horaire ou Anti-horaire
        }
    }
}