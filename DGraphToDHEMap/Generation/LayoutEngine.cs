using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DGraphBuilder.Models.DGraph;

namespace DGraphBuilder.Generation
{
    public class GridCell
    {
        public string RoomId { get; set; } = null;
        public bool IsCorridor => RoomId != null && RoomId.StartsWith("corridor_");
    }

    public class LayoutEngine
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;
        private const int GridSize = 64;

        public LayoutEngine(DGraphFile dgraph, Random random)
        {
            _dgraph = dgraph;
            _random = random;
        }

        public GridCell[,] CalculateGridLayout()
        {
            var grid = new GridCell[GridSize, GridSize];
            for (int i = 0; i < GridSize; i++) for (int j = 0; j < GridSize; j++) grid[i, j] = new GridCell();

            var roomsToPlace = _dgraph.Rooms.Where(r => r.ParentRoom == null).ToList();
            if (!roomsToPlace.Any()) return grid;

            var roomPositions = new Dictionary<string, Rectangle>();
            var queue = new Queue<Room>();

            var startRoom = roomsToPlace.First();
            var startRect = new Rectangle(GridSize / 2, GridSize / 2, _random.Next(2, 5), _random.Next(2, 5));
            PlaceRoomOnGrid(grid, startRoom.Id, startRect);
            roomPositions[startRoom.Id] = startRect;
            queue.Enqueue(startRoom);
            roomsToPlace.Remove(startRoom);

            while (queue.Count > 0 && roomsToPlace.Any())
            {
                var currentRoomData = queue.Dequeue();
                var connections = _dgraph.Connections.Where(c => (c.FromRoom == currentRoomData.Id && !roomPositions.ContainsKey(c.ToRoom)) || (c.ToRoom == currentRoomData.Id && !roomPositions.ContainsKey(c.FromRoom)));

                foreach (var conn in connections)
                {
                    string neighborId = conn.FromRoom == currentRoomData.Id ? conn.ToRoom : conn.FromRoom;
                    if (roomPositions.ContainsKey(neighborId)) continue;

                    var neighborData = _dgraph.Rooms.FirstOrDefault(r => r.Id == neighborId);
                    if (neighborData == null) continue;

                    if (TryPlaceNeighbor(grid, roomPositions[currentRoomData.Id], neighborData, out var newRoomRect, out var corridorPath))
                    {
                        PlaceRoomOnGrid(grid, neighborData.Id, newRoomRect);
                        PlaceCorridorOnGrid(grid, corridorPath, $"{conn.FromRoom}<->{conn.ToRoom}");
                        roomPositions.Add(neighborData.Id, newRoomRect);
                        queue.Enqueue(neighborData);
                        roomsToPlace.Remove(neighborData);
                    }
                    else
                    {
                        Console.WriteLine($"Avertissement: Impossible de placer '{neighborData.Id}' près de '{currentRoomData.Id}'.");
                    }
                }
            }

            return grid;
        }

        private bool TryPlaceNeighbor(GridCell[,] grid, Rectangle parentRect, Room neighbor, out Rectangle roomRect, out List<Point> corridorPath)
        {
            var directions = new List<int> { 0, 1, 2, 3 }.OrderBy(d => _random.Next()).ToList();
            foreach (var dir in directions)
            {
                int corridorLength = _random.Next(2, 6);
                var startPoint = GetRandomPointOnEdge(parentRect, (Direction)dir);
                var endPoint = GetPointInDirection(startPoint, (Direction)dir, corridorLength);

                int roomWidth = _random.Next(2, 5);
                int roomHeight = _random.Next(2, 5);
                roomRect = new Rectangle(endPoint.X - roomWidth / 2, endPoint.Y - roomHeight / 2, roomWidth, roomHeight);

                var path = FindPath(grid, startPoint, endPoint);
                if (path != null && IsRegionFree(grid, roomRect, path))
                {
                    corridorPath = path;
                    return true;
                }
            }
            roomRect = Rectangle.Empty;
            corridorPath = null;
            return false;
        }

        private List<Point> FindPath(GridCell[,] grid, Point start, Point end) { /* ... A* Pathfinding Logic ... */ return new List<Point> { start, end }; } // Placeholder
        private void PlaceRoomOnGrid(GridCell[,] grid, string roomId, Rectangle rect) { for (int x = rect.Left; x < rect.Right; x++) for (int y = rect.Top; y < rect.Bottom; y++) if (IsInBounds(x, y)) grid[x, y].RoomId = roomId; }
        private void PlaceCorridorOnGrid(GridCell[,] grid, List<Point> path, string id) { foreach (var p in path) if (IsInBounds(p.X, p.Y) && grid[p.X, p.Y].RoomId == null) grid[p.X, p.Y].RoomId = $"corridor_{id}"; }
        private bool IsRegionFree(GridCell[,] grid, Rectangle rect, List<Point> excludePath) { for (int x = rect.Left; x < rect.Right; x++) for (int y = rect.Top; y < rect.Bottom; y++) if (!IsInBounds(x, y) || (grid[x, y].RoomId != null && !excludePath.Contains(new Point(x, y)))) return false; return true; }
        private bool IsInBounds(int x, int y) => x >= 0 && x < GridSize && y >= 0 && y < GridSize;
        private Point GetRandomPointOnEdge(Rectangle rect, Direction dir) { switch (dir) { case Direction.North: return new Point(_random.Next(rect.Left, rect.Right), rect.Top); case Direction.South: return new Point(_random.Next(rect.Left, rect.Right), rect.Bottom - 1); case Direction.West: return new Point(rect.Left, _random.Next(rect.Top, rect.Bottom)); default: return new Point(rect.Right - 1, _random.Next(rect.Top, rect.Bottom)); } }
        private Point GetPointInDirection(Point p, Direction d, int dist) { switch (d) { case Direction.North: return new Point(p.X, p.Y - dist); case Direction.South: return new Point(p.X, p.Y + dist); case Direction.West: return new Point(p.X - dist, p.Y); default: return new Point(p.X + dist, p.Y); } }
        private enum Direction { North, East, South, West }
    }
}