using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DShapeGeneratorService
    {
        private readonly Random _random = new Random();

        public DShape Generate(DShapeGenerationParameters genParams)
        {
            var shape = new DShape
            {
                Name = $"random_shape_{DateTime.Now.Ticks}",
                Description = $"Forme avec {genParams.VertexCount} sommets, {genParams.SymmetryAxes} {(genParams.SymmetryAxes == 1 ? "axe" : "axes")} de symétrie ({genParams.SymmetryType}), taille {genParams.Size}, et irrégularité {genParams.Irregularity}."
            };

            shape.Vertices = GenerateVertices(genParams);

            if (IsPolygonSelfIntersecting(shape.Vertices))
            {
                shape.Description += " [AVERTISSEMENT: Auto-intersection]";
            }

            return shape;
        }

        /// <summary>
        /// Aiguille vers la méthode de génération appropriée en fonction des paramètres.
        /// </summary>
        private List<DShapeVertex> GenerateVertices(DShapeGenerationParameters genParams)
        {
            if (genParams.VertexCount < 3) return new List<DShapeVertex>();

            if (genParams.SymmetryAxes > 0 && genParams.SymmetryType == "Axial")
            {
                return GenerateAxialSymmetryRobust(genParams.VertexCount, genParams.SymmetryAxes, genParams.Size, genParams.Irregularity);
            }
            else if (genParams.SymmetryAxes > 0 && genParams.SymmetryType == "Rotational")
            {
                return GenerateRotationalSymmetry(genParams.VertexCount, genParams.SymmetryAxes, genParams.Size, genParams.Irregularity);
            }
            else
            {
                return GenerateNoSymmetry(genParams.VertexCount, genParams.Size, genParams.Irregularity);
            }
        }

        /// <summary>
        /// ALGORITHME FINAL : Génère une forme avec N axes de symétrie miroir de manière robuste.
        /// </summary>
        private List<DShapeVertex> GenerateAxialSymmetryRobust(int vertexCount, int symmetryAxes, double size, double irregularity)
        {
            var vertices = new List<DShapeVertex>();
            var radii = new double[vertexCount];

            int wedgeCount = 2 * symmetryAxes;
            int basePointsCount = (int)Math.Ceiling((double)vertexCount / wedgeCount);
            if (basePointsCount < 1) basePointsCount = 1;

            var baseRadii = new List<double>();
            for (int i = 0; i < basePointsCount; i++)
            {
                baseRadii.Add(size * (1 - _random.NextDouble() * irregularity));
            }

            for (int i = 0; i < vertexCount; i++)
            {
                double progress = (double)i / vertexCount;
                double wedgeProgress = progress * wedgeCount;
                int currentWedgeIndex = (int)Math.Floor(wedgeProgress);

                double posInWedge = wedgeProgress - currentWedgeIndex;

                if (currentWedgeIndex % 2 == 1)
                {
                    posInWedge = 1 - posInWedge;
                }

                int baseRadiiIndex = (int)Math.Round(posInWedge * (basePointsCount - 1));
                radii[i] = baseRadii[Math.Max(0, Math.Min(basePointsCount - 1, baseRadiiIndex))];
            }

            double angleStep = 2 * Math.PI / vertexCount;
            for (int i = 0; i < vertexCount; i++)
            {
                double angle = i * angleStep;
                vertices.Add(new DShapeVertex { X = radii[i] * Math.Cos(angle), Y = radii[i] * Math.Sin(angle) });
            }

            return vertices;
        }

        /// <summary>
        /// Génère une forme sans aucune symétrie.
        /// </summary>
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

        /// <summary>
        /// Génère une forme avec une symétrie de rotation (pivote autour du centre).
        /// </summary>
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

        /// <summary>
        /// Vérifie si le polygone se croise lui-même.
        /// </summary>
        private bool IsPolygonSelfIntersecting(List<DShapeVertex> vertices)
        {
            if (vertices.Count <= 3) return false;
            for (int i = 0; i < vertices.Count; i++)
            {
                var p1 = vertices[i];
                var q1 = vertices[(i + 1) % vertices.Count];
                for (int j = i + 1; j < vertices.Count; j++)
                {
                    // Ne pas tester les arêtes adjacentes
                    if ((i == 0 && j == vertices.Count - 1) || (j + 1) % vertices.Count == i) continue;

                    var p2 = vertices[j];
                    var q2 = vertices[(j + 1) % vertices.Count];
                    if (DoLineSegmentsIntersect(p1, q1, p2, q2)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Vérifie si deux segments de droite [p1, q1] et [p2, q2] s'intersectent.
        /// </summary>
        private bool DoLineSegmentsIntersect(DShapeVertex p1, DShapeVertex q1, DShapeVertex p2, DShapeVertex q2)
        {
            static int Orientation(DShapeVertex p, DShapeVertex q, DShapeVertex r)
            {
                double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
                if (Math.Abs(val) < 1e-10) return 0; // Colinéaire
                return (val > 0) ? 1 : 2; // Horaire ou Anti-horaire
            }

            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);

            // Cas général où les segments se croisent
            if (o1 != 0 && o2 != 0 && o3 != 0 && o4 != 0)
            {
                if (o1 != o2 && o3 != o4) return true;
            }

            // Les cas spéciaux (colinéaires) sont ignorés pour la simplicité,
            // car ils sont rares avec des coordonnées en double.
            return false;
        }
    }
}