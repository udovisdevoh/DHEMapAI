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
                Description = $"Forme avec {genParams.VertexCount} sommets, {genParams.SymmetryAxes} {(genParams.SymmetryAxes == 1 ? "axe" : "axes")} de symétrie ({genParams.SymmetryType}) à {genParams.SymmetryAngle}°, etc."
            };

            shape.Vertices = GenerateVertices(genParams);

            if (IsPolygonSelfIntersecting(shape.Vertices))
            {
                shape.Description += " [AVERTISSEMENT: Auto-intersection]";
            }

            return shape;
        }

        private List<DShapeVertex> GenerateVertices(DShapeGenerationParameters genParams)
        {
            if (genParams.VertexCount < 3) return new List<DShapeVertex>();

            if (genParams.SymmetryAxes > 0 && genParams.SymmetryType == "Axial")
            {
                return GenerateAxialSymmetryRobust(genParams);
            }
            else if (genParams.SymmetryAxes > 0 && genParams.SymmetryType == "Rotational")
            {
                return GenerateRotationalSymmetry(genParams);
            }
            else
            {
                return GenerateNoSymmetry(genParams);
            }
        }

        private List<DShapeVertex> GenerateAxialSymmetryRobust(DShapeGenerationParameters genParams)
        {
            var vertices = new List<DShapeVertex>();
            var radii = new double[genParams.VertexCount];
            var angularOffsets = new double[genParams.VertexCount];
            double symmetryAngleRad = genParams.SymmetryAngle * Math.PI / 180.0;

            int wedgeCount = 2 * genParams.SymmetryAxes;
            int basePointsCount = (int)Math.Ceiling((double)genParams.VertexCount / wedgeCount);
            if (basePointsCount < 1) basePointsCount = 1;

            // Création des gabarits symétriques pour les rayons ET les décalages angulaires
            var baseRadii = new List<double>();
            var baseAngularOffsets = new List<double>();
            for (int i = 0; i < basePointsCount; i++)
            {
                baseRadii.Add(genParams.Size * (1 - _random.NextDouble() * genParams.RadialVariation));
                // Le décalage est une petite valeur aléatoire, contrôlée par AngularVariation
                baseAngularOffsets.Add((_random.NextDouble() - 0.5) * genParams.AngularVariation);
            }

            // Construction des listes complètes et symétriques
            for (int i = 0; i < genParams.VertexCount; i++)
            {
                double progress = (double)i / genParams.VertexCount;
                double wedgeProgress = progress * wedgeCount;
                int currentWedgeIndex = (int)Math.Floor(wedgeProgress);
                double posInWedge = wedgeProgress - currentWedgeIndex;
                if (currentWedgeIndex % 2 == 1) posInWedge = 1 - posInWedge;

                int baseIndex = (int)Math.Round(posInWedge * (basePointsCount - 1));
                baseIndex = Math.Max(0, Math.Min(basePointsCount - 1, baseIndex));

                radii[i] = baseRadii[baseIndex];
                angularOffsets[i] = baseAngularOffsets[baseIndex];
            }

            double angleStep = 2 * Math.PI / genParams.VertexCount;
            for (int i = 0; i < genParams.VertexCount; i++)
            {
                // Application du décalage angulaire (multiplié par l'intervalle pour le rendre proportionnel)
                double angle = i * angleStep + (angularOffsets[i] * angleStep) + symmetryAngleRad;
                vertices.Add(new DShapeVertex { X = radii[i] * Math.Cos(angle), Y = radii[i] * Math.Sin(angle) });
            }
            return vertices;
        }

        private List<DShapeVertex> GenerateNoSymmetry(DShapeGenerationParameters genParams)
        {
            var vertices = new List<DShapeVertex>();
            double angleStep = 2 * Math.PI / genParams.VertexCount;
            for (int i = 0; i < genParams.VertexCount; i++)
            {
                double radius = genParams.Size * (1 - _random.NextDouble() * genParams.RadialVariation);
                double angle = i * angleStep + (angleStep * (_random.NextDouble() - 0.5) * genParams.AngularVariation);
                vertices.Add(new DShapeVertex { X = radius * Math.Cos(angle), Y = radius * Math.Sin(angle) });
            }
            return vertices;
        }

        private List<DShapeVertex> GenerateRotationalSymmetry(DShapeGenerationParameters genParams)
        {
            var vertices = new List<DShapeVertex>();
            double symmetryAngleRad = genParams.SymmetryAngle * Math.PI / 180.0;
            int baseVerticesCount = (int)Math.Ceiling((double)genParams.VertexCount / genParams.SymmetryAxes);
            var baseVertices = new List<DShapeVertex>();
            double angleStep = (2 * Math.PI / genParams.SymmetryAxes) / baseVerticesCount;

            for (int i = 0; i < baseVerticesCount; i++)
            {
                double radius = genParams.Size * (1 - _random.NextDouble() * genParams.RadialVariation);
                // Le décalage angulaire est appliqué directement sur les points de base
                double angle = i * angleStep + (angleStep * (_random.NextDouble() - 0.5) * genParams.AngularVariation);
                baseVertices.Add(new DShapeVertex { X = radius * Math.Cos(angle), Y = radius * Math.Sin(angle) });
            }

            for (int i = 0; i < genParams.SymmetryAxes; i++)
            {
                double rotationAngle = i * (2 * Math.PI / genParams.SymmetryAxes) + symmetryAngleRad;
                foreach (var baseVertex in baseVertices)
                {
                    if (vertices.Count >= genParams.VertexCount) break;
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