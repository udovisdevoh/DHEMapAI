using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DShapeGeneratorService
    {
        private readonly Random _random = new Random();

        public DShape Generate(int vertexCount, int symmetryAxes, double size, double irregularity, string symmetryType)
        {
            var shape = new DShape
            {
                Name = $"random_shape_{DateTime.Now.Ticks}",
                Description = $"Shape with {vertexCount} vertices, {symmetryAxes} symmetry axes ({symmetryType}), size {size}, and irregularity {irregularity}."
            };

            shape.Vertices = GenerateVertices(vertexCount, symmetryAxes, size, irregularity, symmetryType);

            if (IsPolygonSelfIntersecting(shape.Vertices))
            {
                shape.Description += " [WARNING: Self-intersecting]";
            }

            return shape;
        }

        private List<DShapeVertex> GenerateVertices(int vertexCount, int symmetryAxes, double size, double irregularity, string symmetryType)
        {
            if (vertexCount < 3) return new List<DShapeVertex>();

            symmetryAxes = Math.Max(0, symmetryAxes);
            List<DShapeVertex> resultVertices;

            if (symmetryAxes > 0 && symmetryType == "Axial")
            {
                resultVertices = GenerateAxialSymmetryRobust(vertexCount, symmetryAxes, size, irregularity);
            }
            else if (symmetryAxes > 0 && symmetryType == "Rotational")
            {
                resultVertices = GenerateRotationalSymmetry(vertexCount, symmetryAxes, size, irregularity);
            }
            else
            {
                resultVertices = GenerateNoSymmetry(vertexCount, size, irregularity);
            }

            return resultVertices.Select(v => new DShapeVertex { X = Math.Round(v.X, 2), Y = Math.Round(v.Y, 2) }).ToList();
        }

        // --- NOUVEL ALGORITHME DE SYMÉTRIE AXIALE, UNIQUE ET ROBUSTE ---
        private List<DShapeVertex> GenerateAxialSymmetryRobust(int vertexCount, int symmetryAxes, double size, double irregularity)
        {
            var vertices = new List<DShapeVertex>();
            var radii = new double[vertexCount];

            // 1. Déterminer le nombre de "pointes de tarte" (wedges) dans le polygone.
            int wedgeCount = 2 * symmetryAxes;
            // 2. Calculer le nombre de points uniques à générer pour la première moitié d'une pointe.
            int basePointsCount = (int)Math.Ceiling((double)vertexCount / wedgeCount);

            // 3. Générer les rayons (distances) pour ces points de base.
            var baseRadii = new List<double>();
            for (int i = 0; i < basePointsCount; i++)
            {
                baseRadii.Add(size * (1 - _random.NextDouble() * irregularity));
            }

            // 4. Construire la liste complète et symétrique de tous les rayons.
            for (int i = 0; i < vertexCount; i++)
            {
                double progress = (double)i / vertexCount;
                double wedgeProgress = progress * wedgeCount;
                int currentWedgeIndex = (int)Math.Floor(wedgeProgress);

                // Position relative dans la demi-pointe de tarte (entre 0.0 et 1.0)
                double posInWedge = wedgeProgress - currentWedgeIndex;

                // Si la pointe est une réflexion miroir, on lit les rayons de base en sens inverse.
                if (currentWedgeIndex % 2 == 1)
                {
                    posInWedge = 1 - posInWedge;
                }

                // Trouver l'index correspondant dans la liste de rayons de base.
                int baseRadiiIndex = (int)Math.Floor(posInWedge * (basePointsCount - 1));
                radii[i] = baseRadii[Math.Max(0, baseRadiiIndex)];
            }

            // 5. Créer les sommets en utilisant les rayons symétriques et des angles réguliers.
            double angleStep = 2 * Math.PI / vertexCount;
            for (int i = 0; i < vertexCount; i++)
            {
                double angle = i * angleStep;
                vertices.Add(new DShapeVertex { X = radii[i] * Math.Cos(angle), Y = radii[i] * Math.Sin(angle) });
            }

            return vertices;
        }

        private List<DShapeVertex> GenerateNoSymmetry(int vertexCount, double size, double irregularity)
        {
            var vertices = new List<DShapeVertex>();
            double angleStep = 2 * Math.PI / vertexCount;
            for (int i = 0; i < vertexCount; i++)
            {
                double radius = size * (1 - _random.NextDouble() * irregularity);
                double angle = i * angleStep + (angleStep * (_random.NextDouble() - 0.5) * irregularity);
                vertices.Add(new DShapeVertex { X = radius * Math.Cos(angle), Y = radius * Math.Sin(angle) });
            }
            return vertices;
        }

        private List<DShapeVertex> GenerateRotationalSymmetry(int vertexCount, int symmetryAxes, double size, double irregularity)
        {
            var vertices = new List<DShapeVertex>();
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
            return vertices;
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