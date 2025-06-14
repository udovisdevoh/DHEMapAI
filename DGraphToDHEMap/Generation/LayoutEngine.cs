using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using DGraphBuilder.Models.DGraph;

namespace DGraphBuilder.Generation
{
    public class PhysicalRoom
    {
        public Room DGraphRoom { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public Vector2 Velocity { get; set; }
        public RectangleF GetBoundingBox() => new RectangleF(Position.X - Size.X / 2, Position.Y - Size.Y / 2, Size.X, Size.Y);
    }

    public class LayoutEngine
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;

        public LayoutEngine(DGraphFile dgraph, Random random)
        {
            _dgraph = dgraph;
            _random = random;
        }

        public List<PhysicalRoom> CalculateLayout()
        {
            var physicalRooms = InitializePhysicalRooms();
            int iterations = 2000;

            Console.WriteLine($"  -> Début de la simulation de placement pour {iterations} itérations...");
            for (int i = 0; i < iterations; i++)
            {
                ApplyRepulsionForces(physicalRooms);
                ApplyAttractionForces(physicalRooms);
                UpdatePositions(physicalRooms);
            }

            Console.WriteLine("  -> Simulation de placement terminée.");
            NormalizeLayoutPositions(physicalRooms);
            return physicalRooms;
        }

        private List<PhysicalRoom> InitializePhysicalRooms()
        {
            var rooms = new List<PhysicalRoom>();
            foreach (var roomData in _dgraph.Rooms.Where(r => r.ParentRoom == null))
            {
                rooms.Add(new PhysicalRoom
                {
                    DGraphRoom = roomData,
                    Position = new Vector2((float)(_random.NextDouble() - 0.5) * 500, (float)(_random.NextDouble() - 0.5) * 500),
                    Size = new Vector2(_random.Next(384, 769), _random.Next(384, 769)),
                    Velocity = Vector2.Zero
                });
            }
            return rooms;
        }

        private void ApplyRepulsionForces(List<PhysicalRoom> rooms)
        {
            float repulsionStrength = 25.0f;
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    var roomA = rooms[i];
                    var roomB = rooms[j];

                    var rectA = roomA.GetBoundingBox();
                    var rectB = roomB.GetBoundingBox();
                    rectA.Inflate(64, 64);

                    if (rectA.IntersectsWith(rectB))
                    {
                        var pushVector = roomA.Position - roomB.Position;
                        if (pushVector.LengthSquared() == 0) pushVector = new Vector2((float)_random.NextDouble(), (float)_random.NextDouble());
                        var force = Vector2.Normalize(pushVector) * repulsionStrength;
                        roomA.Velocity += force;
                        roomB.Velocity -= force;
                    }
                }
            }
        }

        private void ApplyAttractionForces(List<PhysicalRoom> rooms)
        {
            float attractionStrength = 0.008f;
            var roomDict = rooms.ToDictionary(r => r.DGraphRoom.Id);
            foreach (var connection in _dgraph.Connections)
            {
                if (roomDict.TryGetValue(connection.FromRoom, out var roomA) && roomDict.TryGetValue(connection.ToRoom, out var roomB))
                {
                    var pullVector = roomB.Position - roomA.Position;
                    var distance = pullVector.Length();
                    if (distance > 1) { var force = pullVector * attractionStrength; roomA.Velocity += force; roomB.Velocity -= force; }
                }
            }
        }

        private void UpdatePositions(List<PhysicalRoom> rooms)
        {
            float damping = 0.85f;
            foreach (var room in rooms)
            {
                room.Position += room.Velocity;
                room.Velocity *= damping;
            }
        }

        private void NormalizeLayoutPositions(List<PhysicalRoom> layout)
        {
            if (!layout.Any()) return;
            float minX = float.MaxValue, minY = float.MaxValue;
            foreach (var room in layout) { var box = room.GetBoundingBox(); if (box.Left < minX) minX = box.Left; if (box.Top < minY) minY = box.Top; }
            var offsetX = -minX + 256;
            var offsetY = -minY + 256;
            foreach (var room in layout) { room.Position = new Vector2(room.Position.X + offsetX, room.Position.Y + offsetY); }
        }
    }
}