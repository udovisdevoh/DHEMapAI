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

        public LayoutEngine(DGraphFile dgraph, Random random)
        {
            _dgraph = dgraph;
            _random = random;
        }

        public GridCell[,] GenerateLayout()
        {
            var grid = new GridCell[GenerationConfig.GridSize, GenerationConfig.GridSize];
            for (int i = 0; i < GenerationConfig.GridSize; i++) for (int j = 0; j < GenerationConfig.GridSize; j++) grid[i, j] = new GridCell();

            var roomsToPlace = _dgraph.Rooms.Where(r => r.ParentRoom == null).ToList();
            if (!roomsToPlace.Any()) return grid;

            var roomGridRects = new Dictionary<string, Rectangle>();
            var queue = new Queue<Room>();

            var startRoom = roomsToPlace.First();
            var startRect = new Rectangle(GenerationConfig.GridSize / 2, GenerationConfig.GridSize / 2, _random.Next(3, 6), _random.Next(3, 6));
            PlaceOnGrid(grid, startRoom.Id, startRect);
            roomGridRects[startRoom.Id] = startRect;
            queue.Enqueue(startRoom);
            roomsToPlace.Remove(startRoom);

            while (queue.Count > 0 && roomsToPlace.Any())
            {
                var currentRoomData = queue.Dequeue();
                var connections = _dgraph.Connections.Where(c => (c.FromRoom == currentRoomData.Id && !roomGridRects.ContainsKey(c.ToRoom)) || (c.ToRoom == currentRoomData.Id && !roomGridRects.ContainsKey(c.FromRoom)));

                foreach (var conn in connections)
                {
                    string neighborId = conn.FromRoom == currentRoomData.Id ? conn.ToRoom : conn.FromRoom;
                    if (roomGridRects.ContainsKey(neighborId)) continue;

                    var neighborData = _dgraph.Rooms.FirstOrDefault(r => r.Id == neighborId);
                    if (neighborData == null) continue;

                    if (TryPlaceNeighbor(grid, roomGridRects[currentRoomData.Id], out var newRoomRect, out var corridorPath))
                    {
                        PlaceOnGrid(grid, neighborData.Id, newRoomRect);
                        PlaceOnGrid(grid, $"corridor_{conn.FromRoom}_{conn.ToRoom}", corridorPath);
                        roomGridRects.Add(neighborData.Id, newRoomRect);
                        queue.Enqueue(neighborData);
                        roomsToPlace.Remove(neighborData);
                    }
                }
            }
            return grid;
        }

        private bool TryPlaceNeighbor(GridCell[,] grid, Rectangle parentRect, out Rectangle roomRect, out List<Point> corridorPath)
        {
            for (int i = 0; i < 30; i++)
            {
                var dir = (Direction)_random.Next(4);
                var startPoint = GetRandomPointOnEdge(parentRect, dir);
                var endPoint = new Point(startPoint.X + GetDelta(dir).X * _random.Next(3, 8), startPoint.Y + GetDelta(dir).Y * _random.Next(3, 8));

                var path = FindPath(grid, GetPointInDirection(startPoint, dir, 1), endPoint);
                if (path != null)
                {
                    int roomWidth = _random.Next(2, 5);
                    int roomHeight = _random.Next(2, 5);
                    var finalPos = path.Last();
                    roomRect = new Rectangle(finalPos.X - roomWidth / 2, finalPos.Y - roomHeight / 2, roomWidth, roomHeight);

                    if (IsRegionFree(grid, roomRect, path))
                    {
                        corridorPath = path;
                        return true;
                    }
                }
            }
            roomRect = Rectangle.Empty;
            corridorPath = null;
            return false;
        }

        private List<Point> FindPath(GridCell[,] grid, Point start, Point end)
        {
            var openSet = new List<Node>();
            var closedSet = new HashSet<Point>();
            openSet.Add(new Node(start, null, 0, GetHeuristic(start, end)));

            while (openSet.Count > 0)
            {
                var current = openSet.OrderBy(n => n.FScore).First();
                if (current.Position == end) return ReconstructPath(current);

                openSet.Remove(current);
                closedSet.Add(current.Position);

                foreach (var dir in new[] { new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0) })
                {
                    var neighborPos = new Point(current.Position.X + dir.X, current.Position.Y + dir.Y);
                    if (!IsInBounds(neighborPos) || closedSet.Contains(neighborPos) || grid[neighborPos.X, neighborPos.Y].RoomId != null) continue;

                    var tentativeGScore = current.GScore + 1;
                    var existingNode = openSet.FirstOrDefault(n => n.Position == neighborPos);
                    if (existingNode == null || tentativeGScore < existingNode.GScore)
                    {
                        if (existingNode != null) openSet.Remove(existingNode);
                        openSet.Add(new Node(neighborPos, current, tentativeGScore, GetHeuristic(neighborPos, end)));
                    }
                }
            }
            return null;
        }

        private List<Point> ReconstructPath(Node current) { var path = new List<Point>(); while (current != null) { path.Add(current.Position); current = current.Parent; } path.Reverse(); return path; }
        private class Node { public Point Position; public Node Parent; public float GScore; public float HScore; public float FScore => GScore + HScore; public Node(Point p, Node pa, float g, float h) { Position = p; Parent = pa; GScore = g; HScore = h; } }
        private float GetHeuristic(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        private void PlaceOnGrid(GridCell[,] grid, string roomId, Rectangle rect) { for (int x = rect.Left; x < rect.Right; x++) for (int y = rect.Top; y < rect.Bottom; y++) if (IsInBounds(new Point(x, y))) grid[x, y].RoomId = roomId; }
        private void PlaceOnGrid(GridCell[,] grid, string roomId, List<Point> path) { foreach (var p in path) if (IsInBounds(p) && grid[p.X, p.Y].RoomId == null) grid[p.X, p.Y].RoomId = $"corridor_{roomId}"; }
        private bool IsRegionFree(GridCell[,] grid, Rectangle rect, List<Point> excludePath) { for (int x = rect.Left; x < rect.Right; x++) for (int y = rect.Top; y < rect.Bottom; y++) if (!IsInBounds(new Point(x, y)) || (grid[x, y].RoomId != null && !(excludePath?.Contains(new Point(x, y)) ?? false))) return false; return true; }
        private bool IsInBounds(Point p) => p.X >= 0 && p.X < GenerationConfig.GridSize && p.Y >= 0 && p.Y < GenerationConfig.GridSize;
        private Point GetRandomPointOnEdge(Rectangle rect, Direction dir) { switch (dir) { case Direction.North: return new Point(_random.Next(rect.Left, rect.Right), rect.Top); case Direction.South: return new Point(_random.Next(rect.Left, rect.Right), rect.Bottom - 1); case Direction.West: return new Point(rect.Left, _random.Next(rect.Top, rect.Bottom)); default: return new Point(rect.Right - 1, _random.Next(rect.Top, rect.Bottom)); } }
        private Point GetDelta(Direction d) { switch (d) { case Direction.North: return new Point(0, -1); case Direction.South: return new Point(0, 1); case Direction.West: return new Point(-1, 0); default: return new Point(1, 0); } }
        private Point GetPointInDirection(Point p, Direction d, int dist) { var delta = GetDelta(d); return new Point(p.X + delta.X * dist, p.Y + delta.Y * dist); }
        private enum Direction { North, East, South, West }
    }
}