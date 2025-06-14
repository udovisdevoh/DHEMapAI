using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Models.Dhemap;

namespace DGraphBuilder.Generation
{
    public class MapBuilder
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;
        private DhemapFile _dhemap;
        private Dictionary<Point, Vertex> _vertexNodeMap;
        private int _nextId;
        private const int CellSize = 128; // Des pièces et couloirs plus petits

        public MapBuilder(DGraphFile dgraph, Random random)
        {
            _dgraph = dgraph;
            _random = random;
        }

        public DhemapFile Build(GridCell[,] grid)
        {
            InitializeDhemap();

            var sectorGrid = CreateSectorsFromGrid(grid);
            GenerateLinedefsFromGrid(sectorGrid);
            PlaceAllThings(grid);

            return _dhemap;
        }

        private void InitializeDhemap() { _dhemap = new DhemapFile { MapInfo = new Models.Dhemap.MapInfo { Game = _dgraph.MapInfo.Game, Map = _dgraph.MapInfo.Map, Name = _dgraph.MapInfo.Name, Music = _dgraph.MapInfo.Music, SkyTexture = "SKY1" }, Vertices = new List<Vertex>(), Linedefs = new List<Linedef>(), Sidedefs = new List<Sidedef>(), Sectors = new List<Sector>(), Things = new List<Thing>() }; _vertexNodeMap = new Dictionary<Point, Vertex>(); _nextId = 0; }

        private int[,] CreateSectorsFromGrid(GridCell[,] grid)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var sectorGrid = new int[width, height];
            for (int i = 0; i < width; i++) for (int j = 0; j < height; j++) sectorGrid[i, j] = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y].RoomId != null && sectorGrid[x, y] == -1)
                    {
                        var roomData = _dgraph.Rooms.FirstOrDefault(r => r.Id == grid[x, y].RoomId);
                        var props = roomData?.Properties ?? new RoomProperties { Floor = "normal", Ceiling = "normal", LightLevel = "normal", WallTexture = "wall_accent", FloorFlat = "floor_primary", CeilingFlat = "ceiling_primary" };
                        var sector = CreateSectorFromRoom(props);
                        _dhemap.Sectors.Add(sector);

                        FloodFill(grid, sectorGrid, new Point(x, y), sector.Id, grid[x, y].RoomId);
                    }
                }
            }
            return sectorGrid;
        }

        private void FloodFill(GridCell[,] grid, int[,] sectorMap, Point start, int fillId, string targetRoomId) { var q = new Queue<Point>(); q.Enqueue(start); while (q.Count > 0) { var p = q.Dequeue(); if (p.X < 0 || p.X >= grid.GetLength(0) || p.Y < 0 || p.Y >= grid.GetLength(1) || sectorMap[p.X, p.Y] != -1 || grid[p.X, p.Y].RoomId != targetRoomId) continue; sectorMap[p.X, p.Y] = fillId; q.Enqueue(new Point(p.X + 1, p.Y)); q.Enqueue(new Point(p.X - 1, p.Y)); q.Enqueue(new Point(p.X, p.Y + 1)); q.Enqueue(new Point(p.X, p.Y - 1)); } }

        private void GenerateLinedefsFromGrid(int[,] sectorGrid)
        {
            Console.WriteLine("  -> Traçage des frontières de secteurs...");
            int width = sectorGrid.GetLength(0);
            int height = sectorGrid.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentId = sectorGrid[x, y];

                    int southId = (y > 0) ? sectorGrid[x, y - 1] : -1;
                    if (currentId != southId) AddEdge(new Point(x, y), new Point(x + 1, y), currentId, southId);

                    int westId = (x > 0) ? sectorGrid[x - 1, y] : -1;
                    if (currentId != westId) AddEdge(new Point(x, y + 1), new Point(x, y), currentId, westId);
                }
            }
        }

        private void AddEdge(Point p1, Point p2, int s1, int s2)
        {
            if (s1 == -1 && s2 == -1) return;
            var v1 = GetOrCreateVertex(p1);
            var v2 = GetOrCreateVertex(p2);

            int frontSectorId = Math.Max(s1, s2);
            int backSectorId = Math.Min(s1, s2);

            string texture = GetTexture("wall_primary");
            AddLinedef(v2, v1, frontSectorId, backSectorId, texture); // Assurer un enroulement CCW
        }

        private Vertex GetOrCreateVertex(Point gridPoint)
        {
            if (_vertexNodeMap.TryGetValue(gridPoint, out var vertex)) return vertex;
            var newVertex = AddVertex(gridPoint.X * CellSize, gridPoint.Y * CellSize);
            _vertexNodeMap[gridPoint] = newVertex;
            return newVertex;
        }

        private void PlaceAllThings(GridCell[,] grid)
        {
            var roomBounds = new Dictionary<string, List<RectangleF>>();
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    if (grid[x, y].RoomId != null)
                    {
                        if (!roomBounds.ContainsKey(grid[x, y].RoomId)) roomBounds[grid[x, y].RoomId] = new List<RectangleF>();
                        roomBounds[grid[x, y].RoomId].Add(new RectangleF(x * CellSize, y * CellSize, CellSize, CellSize));
                    }
                }
            }
            foreach (var roomData in _dgraph.Rooms)
            {
                if (roomBounds.TryGetValue(roomData.Id, out var rects))
                {
                    var randomRect = rects[_random.Next(rects.Count)];
                    PlaceThings(roomData, randomRect);
                }
            }
        }

        // --- Méthodes Utilitaires ---
        private Vertex AddVertex(float x, float y) { var v = new Vertex { Id = _nextId++, X = x, Y = y }; _dhemap.Vertices.Add(v); return v; }
        private Linedef AddLinedef(Vertex v1, Vertex v2, int frontSectorId, int backSectorId, string texture) { var frontSideId = AddSidedef(frontSectorId, texture); var line = new Linedef { Id = _nextId++, StartVertex = v1.Id, EndVertex = v2.Id, FrontSidedef = frontSideId }; if (backSectorId != -1) { line.BackSidedef = AddSidedef(backSectorId, texture); line.Flags.Add("twoSided"); } else { line.Flags.Add("impassable"); } _dhemap.Linedefs.Add(line); return line; }
        private int AddSidedef(int sectorId, string texture) { var side = new Sidedef { Id = _nextId++, Sector = sectorId, TextureMiddle = texture, TextureTop = "-", TextureBottom = "-" }; _dhemap.Sidedefs.Add(side); return side.Id; }
        private Sector CreateSectorFromRoom(RoomProperties props) { return new Sector { Id = _nextId++, Tag = props.Tag ?? 0, FloorTexture = GetTexture(props.FloorFlat), CeilingTexture = (props.Ceiling == "sky" ? "F_SKY1" : GetTexture(props.CeilingFlat)), LightLevel = GetLight(props.LightLevel), FloorHeight = GetHeight(props.Floor), CeilingHeight = GetHeight(props.Ceiling, 128) }; }
        private void PlaceThings(Room roomData, RectangleF bounds) { var items = roomData.Contents?.Items ?? new List<ContentItem>(); var monsters = roomData.Contents?.Monsters ?? new List<ContentItem>(); foreach (var item in items.Concat(monsters)) for (int i = 0; i < item.Count; i++) _dhemap.Things.Add(new Thing { Id = _nextId++, X = bounds.X + (float)(_random.NextDouble() * bounds.Width), Y = bounds.Y + (float)(_random.NextDouble() * bounds.Height), Angle = _random.Next(8) * 45, Type = item.TypeId, Flags = new List<string> { "skillEasy", "skillNormal", "skillHard" } }); }
        private string GetTexture(string concept) { if (string.IsNullOrEmpty(concept)) return "STARG1"; if (_dgraph.ThemePalette.TryGetValue(concept, out var textures) && textures.Any()) { int totalWeight = textures.Sum(t => t.Weight); int r = _random.Next(0, totalWeight); foreach (var texture in textures) { if (r < texture.Weight) return texture.Name; r -= texture.Weight; } } return "STARG1"; }
        private int GetHeight(string height, int defaultHeight = 0) => height switch { "very_low" => -32, "low" => 0, "normal" => defaultHeight, "high" => 128, "very_high" => 256, _ => defaultHeight };
        private int GetLight(string light) => light switch { "dark" => 80, "dim" => 120, "normal" => 160, "bright" => 220, "flickering" => 160, _ => 160 };
    }
}