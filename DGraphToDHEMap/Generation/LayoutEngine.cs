// Generation/LayoutEngine.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DGraphBuilder.Models.DGraph;

namespace DGraphBuilder.Generation
{
    public class LayoutEngine
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;
        private readonly ShapeGenerator _shapeGenerator;
        private readonly Pathfinder _pathfinder;

        public Dictionary<string, Polygon> PlacedPolygons { get; private set; }

        public LayoutEngine(DGraphFile dgraph, Random random)
        {
            _dgraph = dgraph;
            _random = random;
            _shapeGenerator = new ShapeGenerator(random);
            _pathfinder = new Pathfinder(random);
            PlacedPolygons = new Dictionary<string, Polygon>();
        }

        public bool GenerateLayout()
        {
            var roomsToPlace = _dgraph.Rooms.Where(r => r.ParentRoom == null).ToList();
            if (!roomsToPlace.Any()) return false;

            var queue = new Queue<Room>();

            var startRoom = roomsToPlace.First();
            var startPolygon = _shapeGenerator.GenerateRoomShape(startRoom);
            PlacedPolygons.Add(startRoom.Id, startPolygon);
            queue.Enqueue(startRoom);
            roomsToPlace.Remove(startRoom);

            while (queue.Count > 0)
            {
                var currentRoom = queue.Dequeue();
                var connections = _dgraph.Connections
                    .Where(c => c.FromRoom == currentRoom.Id || c.ToRoom == currentRoom.Id)
                    .ToList();

                foreach (var conn in connections)
                {
                    string neighborId = (conn.FromRoom == currentRoom.Id) ? conn.ToRoom : conn.FromRoom;
                    if (PlacedPolygons.ContainsKey(neighborId)) continue;

                    var neighborRoom = _dgraph.Rooms.FirstOrDefault(r => r.Id == neighborId);
                    if (neighborRoom == null) continue;

                    if (TryPlaceNeighbor(currentRoom, neighborRoom, out var newPolygon))
                    {
                        PlacedPolygons.Add(neighborId, newPolygon);
                        queue.Enqueue(neighborRoom);
                        roomsToPlace.Remove(neighborRoom);
                    }
                    else
                    {
                        Console.WriteLine($"AVERTISSEMENT: Impossible de placer la pièce '{neighborId}' connectée à '{currentRoom.Id}'.");
                    }
                }
            }

            var roomPolygons = PlacedPolygons.Values.ToList();
            foreach (var conn in _dgraph.Connections)
            {
                string corridorId = $"corridor_{conn.FromRoom}_{conn.ToRoom}";
                if (PlacedPolygons.ContainsKey(corridorId) || !PlacedPolygons.ContainsKey(conn.FromRoom) || !PlacedPolygons.ContainsKey(conn.ToRoom)) continue;

                Polygon fromPolygon = PlacedPolygons[conn.FromRoom];
                Polygon toPolygon = PlacedPolygons[conn.ToRoom];

                // On passe maintenant la liste des pièces comme obstacles
                var corridorPoly = _pathfinder.FindCorridorPath(fromPolygon, toPolygon, 128f, roomPolygons);

                if (corridorPoly != null)
                {
                    PlacedPolygons.Add(corridorId, corridorPoly);
                    // On ajoute le nouveau couloir à la liste des obstacles pour les prochains.
                    roomPolygons.Add(corridorPoly);
                }
                else
                {
                    Console.WriteLine($"AVERTISSEMENT: Impossible de trouver un chemin pour le couloir entre '{conn.FromRoom}' et '{conn.ToRoom}'.");
                }
            }

            return !roomsToPlace.Any();
        }

        private bool TryPlaceNeighbor(Room parentRoom, Room neighborRoom, out Polygon placedNeighborPolygon)
        {
            const int maxAttempts = 100;
            const float initialDistance = 800f;

            Polygon parentPolygon = PlacedPolygons[parentRoom.Id];
            PointF parentCenter = parentPolygon.GetCenter();

            for (int i = 0; i < maxAttempts; i++)
            {
                double angle = _random.NextDouble() * 2 * Math.PI;
                float distance = initialDistance + (i * 20);

                PointF neighborCenter = new PointF(
                    parentCenter.X + (float)(Math.Cos(angle) * distance),
                    parentCenter.Y + (float)(Math.Sin(angle) * distance)
                );

                Polygon candidatePolygon = _shapeGenerator.GenerateRoomShape(neighborRoom);
                candidatePolygon.Translate(neighborCenter.X, neighborCenter.Y);

                bool collision = false;
                foreach (var placedPolygon in PlacedPolygons.Values)
                {
                    if (CollisionDetector.ArePolygonsOverlapping(candidatePolygon, placedPolygon))
                    {
                        collision = true;
                        break;
                    }
                }

                if (!collision)
                {
                    placedNeighborPolygon = candidatePolygon;
                    return true;
                }
            }

            placedNeighborPolygon = null;
            return false;
        }
    }
}