using System;
using System.Collections.Generic;
using System.Drawing;
using DGraphBuilder.Models.DGraph;

namespace DGraphBuilder.Generation
{
    public class ShapeGenerator
    {
        private readonly Random _random;
        // Pourrait être étendu avec un "thème géométrique" global.
        private const float BaseRoomSize = 512f; // Taille de base pour une pièce "moyenne".

        public ShapeGenerator(Random random)
        {
            _random = random;
        }

        public Polygon GenerateRoomShape(Room room)
        {
            int vertices = room.ShapeHint?.Vertices ?? 4;
            // Un peu de variété aléatoire basée sur le nombre de sommets demandé
            if (_random.Next(0, 5) == 0) // 20% de chance d'altérer le nombre de sommets
            {
                vertices += _random.Next(-1, 2);
                vertices = Math.Max(3, vertices);
            }

            // Logique de sélection de la forme
            if (vertices == 4 && _random.Next(0, 4) == 0)
                return CreateLShape();

            if (vertices >= 5 && _random.Next(0, 2) == 0)
                return CreateRegularPolygon(vertices);

            return CreateStretchedRectangle(vertices);
        }

        public Polygon CreateCorridor(PointF start, PointF end, float width)
        {
            // Crée un simple couloir rectangulaire. Pourrait être complexifié plus tard.
            var points = new List<PointF>
            {
                new PointF(start.X, start.Y - width / 2),
                new PointF(end.X, end.Y - width / 2),
                new PointF(end.X, end.Y + width / 2),
                new PointF(start.X, start.Y + width / 2),
            };
            return new Polygon(points);
        }

        private Polygon CreateRegularPolygon(int numVertices)
        {
            var vertices = new List<PointF>();
            float radius = BaseRoomSize * (float)(_random.NextDouble() * 0.4 + 0.8); // 80% to 120%
            float angleStep = 2 * (float)Math.PI / numVertices;

            for (int i = 0; i < numVertices; i++)
            {
                float angle = i * angleStep;
                float x = radius * (float)Math.Cos(angle);
                float y = radius * (float)Math.Sin(angle);
                vertices.Add(new PointF(x, y));
            }
            return new Polygon(vertices);
        }

        private Polygon CreateStretchedRectangle(int numVertices)
        {
            // Simplifié pour l'exemple, crée un rectangle étiré.
            float width = BaseRoomSize * (float)(_random.NextDouble() * 0.5 + 0.75); // 75% to 125%
            float height = BaseRoomSize * (float)(_random.NextDouble() * 0.5 + 0.75);
            var vertices = new List<PointF>
            {
                new PointF(-width / 2, -height / 2),
                new PointF(width / 2, -height / 2),
                new PointF(width / 2, height / 2),
                new PointF(-width / 2, height / 2)
            };
            // Pour plus de sommets, on pourrait ajouter des points sur les arêtes.
            return new Polygon(vertices);
        }

        private Polygon CreateLShape()
        {
            float w1 = BaseRoomSize * (float)(_random.NextDouble() * 0.6 + 0.7); // 70-130%
            float h1 = BaseRoomSize * (float)(_random.NextDouble() * 0.6 + 0.7);
            float w2 = w1 * (float)(_random.NextDouble() * 0.4 + 0.4); // 40-80% of w1
            float h2 = h1 * (float)(_random.NextDouble() * 0.4 + 0.4);

            var vertices = new List<PointF>
            {
                new PointF(0, 0),
                new PointF(w1, 0),
                new PointF(w1, h2),
                new PointF(w2, h2),
                new PointF(w2, h1),
                new PointF(0, h1)
            };

            // Centrer le polygone
            var center = new PointF(w1 / 2, h1 / 2);
            for (int i = 0; i < vertices.Count; i++)
                vertices[i] = new PointF(vertices[i].X - center.X, vertices[i].Y - center.Y);

            return new Polygon(vertices);
        }
    }
}