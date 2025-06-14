// Generation/Pathfinder.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DGraphBuilder.Generation
{
    public class Pathfinder
    {
        private readonly Random _random;
        private const int NavGridCellSize = 128; // La résolution de notre grille de pathfinding

        private class Node
        {
            public Point Position { get; }
            public Node Parent { get; set; }
            public int GCost { get; set; } // Coût du début au nœud actuel
            public int HCost { get; set; } // Coût heuristique du nœud actuel à la fin
            public int FCost => GCost + HCost; // Coût total

            public Node(Point position) { Position = position; }
        }

        public Pathfinder(Random random)
        {
            _random = random;
        }

        public Polygon FindCorridorPath(Polygon from, Polygon to, float width, List<Polygon> obstacles)
        {
            FindClosestPoints(from, to, out var startPoint, out var endPoint);

            var bounds = GetTotalBounds(obstacles);
            int gridWidth = (int)Math.Ceiling(bounds.Width / NavGridCellSize);
            int gridHeight = (int)Math.Ceiling(bounds.Height / NavGridCellSize);

            if (gridWidth <= 0 || gridHeight <= 0) return null;

            bool[,] navGrid = CreateNavigationGrid(gridWidth, gridHeight, bounds.Location, obstacles);

            Point startNodePos = WorldToGrid(startPoint, bounds.Location);
            Point endNodePos = WorldToGrid(endPoint, bounds.Location);

            // S'assurer que les points de départ et d'arrivée sont marchables
            MakeCellWalkable(navGrid, startNodePos);
            MakeCellWalkable(navGrid, endNodePos);

            var path = FindPathAStar(navGrid, startNodePos, endNodePos);

            if (path == null) return null;

            var simplifiedPath = SimplifyPath(path);

            return CreatePolygonFromPath(simplifiedPath, width, bounds.Location);
        }

        private List<Point> FindPathAStar(bool[,] grid, Point start, Point end)
        {
            var openSet = new List<Node>();
            var closedSet = new HashSet<Point>();
            var startNode = new Node(start) { GCost = 0, HCost = GetHeuristic(start, end) };
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                var currentNode = openSet.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();
                openSet.Remove(currentNode);
                closedSet.Add(currentNode.Position);

                if (currentNode.Position == end) return ReconstructPath(currentNode);

                foreach (var neighborPos in GetNeighbors(currentNode.Position, grid))
                {
                    if (closedSet.Contains(neighborPos)) continue;

                    int newGCost = currentNode.GCost + GetHeuristic(currentNode.Position, neighborPos);

                    var neighborNode = openSet.FirstOrDefault(n => n.Position == neighborPos);
                    if (neighborNode == null || newGCost < neighborNode.GCost)
                    {
                        if (neighborNode == null)
                        {
                            neighborNode = new Node(neighborPos);
                            openSet.Add(neighborNode);
                        }
                        neighborNode.Parent = currentNode;
                        neighborNode.GCost = newGCost;
                        neighborNode.HCost = GetHeuristic(neighborPos, end);
                    }
                }
            }
            return null; // Pas de chemin trouvé
        }

        private List<Point> SimplifyPath(List<Point> path)
        {
            if (path.Count < 3) return path;
            var simplified = new List<Point> { path[0] };
            for (int i = 1; i < path.Count - 1; i++)
            {
                var prev = path[i - 1];
                var current = path[i];
                var next = path[i + 1];
                if ((current.X - prev.X) != (next.X - current.X) || (current.Y - prev.Y) != (next.Y - current.Y))
                {
                    simplified.Add(current);
                }
            }
            simplified.Add(path.Last());
            return simplified;
        }

        private Polygon CreatePolygonFromPath(List<Point> path, float width, PointF gridOrigin)
        {
            var vertices = new List<PointF>();
            if (path.Count < 2) return null;
            float halfWidth = width / 2;

            for (int i = 0; i < path.Count; i++)
            {
                Point currentGrid = path[i];
                PointF current = GridToWorld(currentGrid, gridOrigin);

                PointF dir;
                if (i < path.Count - 1)
                    dir = new PointF(path[i + 1].X - currentGrid.X, path[i + 1].Y - currentGrid.Y);
                else
                    dir = new PointF(currentGrid.X - path[i - 1].X, currentGrid.Y - path[i - 1].Y);

                var (p1, p2) = GetPerpendicularPoints(dir, current, halfWidth);
                vertices.Add(p1);
            }
            for (int i = path.Count - 1; i >= 0; i--)
            {
                Point currentGrid = path[i];
                PointF current = GridToWorld(currentGrid, gridOrigin);

                PointF dir;
                if (i > 0)
                    dir = new PointF(currentGrid.X - path[i - 1].X, currentGrid.Y - path[i - 1].Y);
                else
                    dir = new PointF(path[i + 1].X - currentGrid.X, path[i + 1].Y - currentGrid.Y);

                var (p1, p2) = GetPerpendicularPoints(dir, current, halfWidth);
                vertices.Add(p2);
            }

            return new Polygon(vertices);
        }

        private (PointF, PointF) GetPerpendicularPoints(PointF direction, PointF center, float halfWidth)
        {
            float len = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
            PointF normDir = new PointF(direction.X / len, direction.Y / len);
            PointF perpDir = new PointF(-normDir.Y, normDir.X);

            return (
                new PointF(center.X + perpDir.X * halfWidth, center.Y + perpDir.Y * halfWidth),
                new PointF(center.X - perpDir.X * halfWidth, center.Y - perpDir.Y * halfWidth)
            );
        }

        private List<Point> ReconstructPath(Node node) { var path = new List<Point>(); while (node != null) { path.Add(node.Position); node = node.Parent; } path.Reverse(); return path; }
        private int GetHeuristic(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        private IEnumerable<Point> GetNeighbors(Point p, bool[,] grid) { var neighbors = new List<Point>(); int w = grid.GetLength(0); int h = grid.GetLength(1); if (p.X > 0 && grid[p.X - 1, p.Y]) neighbors.Add(new Point(p.X - 1, p.Y)); if (p.X < w - 1 && grid[p.X + 1, p.Y]) neighbors.Add(new Point(p.X + 1, p.Y)); if (p.Y > 0 && grid[p.X, p.Y - 1]) neighbors.Add(new Point(p.X, p.Y - 1)); if (p.Y < h - 1 && grid[p.X, p.Y + 1]) neighbors.Add(new Point(p.X, p.Y + 1)); return neighbors; }
        private bool[,] CreateNavigationGrid(int w, int h, PointF origin, List<Polygon> obstacles) { var grid = new bool[w, h]; for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) { var worldPoint = GridToWorld(new Point(x, y), origin); grid[x, y] = !obstacles.Any(o => IsPointInPolygon(o, worldPoint)); } return grid; }
        private void MakeCellWalkable(bool[,] grid, Point cell) { if (cell.X >= 0 && cell.X < grid.GetLength(0) && cell.Y >= 0 && cell.Y < grid.GetLength(1)) grid[cell.X, cell.Y] = true; }
        private RectangleF GetTotalBounds(List<Polygon> polygons) { var allVertices = polygons.SelectMany(p => p.Vertices).ToList(); if (!allVertices.Any()) return RectangleF.Empty; float minX = allVertices.Min(v => v.X); float minY = allVertices.Min(v => v.Y); float maxX = allVertices.Max(v => v.X); float maxY = allVertices.Max(v => v.Y); return new RectangleF(minX - 512, minY - 512, (maxX - minX) + 1024, (maxY - minY) + 1024); }
        private Point WorldToGrid(PointF world, PointF origin) => new Point((int)((world.X - origin.X) / NavGridCellSize), (int)((world.Y - origin.Y) / NavGridCellSize));
        private PointF GridToWorld(Point grid, PointF origin) => new PointF(grid.X * NavGridCellSize + origin.X + NavGridCellSize / 2, grid.Y * NavGridCellSize + origin.Y + NavGridCellSize / 2);
        private void FindClosestPoints(Polygon p1, Polygon p2, out PointF c1, out PointF c2) { c1 = PointF.Empty; c2 = PointF.Empty; float minDst = float.MaxValue; foreach (var v1 in p1.Vertices) foreach (var v2 in p2.Vertices) { float d = (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y); if (d < minDst) { minDst = d; c1 = v1; c2 = v2; } } }
        private bool IsPointInPolygon(Polygon polygon, PointF testPoint) { bool result = false; int j = polygon.Vertices.Count - 1; for (int i = 0; i < polygon.Vertices.Count; i++) { if (polygon.Vertices[i].Y < testPoint.Y && polygon.Vertices[j].Y >= testPoint.Y || polygon.Vertices[j].Y < testPoint.Y && polygon.Vertices[i].Y >= testPoint.Y) { if (polygon.Vertices[i].X + (testPoint.Y - polygon.Vertices[i].Y) / (polygon.Vertices[j].Y - polygon.Vertices[i].Y) * (polygon.Vertices[j].X - polygon.Vertices[i].X) < testPoint.X) { result = !result; } } j = i; } return result; }
    }
}