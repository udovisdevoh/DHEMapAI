// Generation/MapBuilder.cs
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

        private class BuiltSectorInfo
        {
            public Sector DhemapSector { get; set; }
            public Rectangle GridRect { get; set; }
        }

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

        private void InitializeDhemap()
        {
            _dhemap = new DhemapFile
            {
                MapInfo = new Models.Dhemap.MapInfo { Game = _dgraph.MapInfo.Game, Map = _dgraph.MapInfo.Map, Name = _dgraph.MapInfo.Name, Music = _dgraph.MapInfo.Music, SkyTexture = "SKY1" },
                Vertices = new List<Vertex>(),
                Linedefs = new List<Linedef>(),
                Sidedefs = new List<Sidedef>(),
                Sectors = new List<Sector>(),
                Things = new List<Thing>()
            };
            _vertexNodeMap = new Dictionary<Point, Vertex>();
            _nextId = 0;
        }

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

        private void FloodFill(GridCell[,] grid, int[,] sectorMap, Point start, int fillId, string targetRoomId)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var q = new Queue<Point>();
            q.Enqueue(start);

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (p.X < 0 || p.X >= width || p.Y < 0 || p.Y >= height || sectorMap[p.X, p.Y] != -1 || grid[p.X, p.Y]?.RoomId != targetRoomId)
                    continue;

                sectorMap[p.X, p.Y] = fillId;
                q.Enqueue(new Point(p.X + 1, p.Y));
                q.Enqueue(new Point(p.X - 1, p.Y));
                q.Enqueue(new Point(p.X, p.Y + 1));
                q.Enqueue(new Point(p.X, p.Y - 1));
            }
        }

        private void GenerateLinedefsFromGrid(int[,] sectorGrid)
        {
            Console.WriteLine("  -> Traçage des frontières de secteurs...");
            int width = sectorGrid.GetLength(0);
            int height = sectorGrid.GetLength(1);

            // Création des Linedefs VERTICAUX
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x <= width; x++)
                {
                    int leftSector = (x > 0) ? sectorGrid[x - 1, y] : -1;
                    int rightSector = (x < width) ? sectorGrid[x, y] : -1;

                    if (leftSector != rightSector)
                    {
                        // Le vecteur de la ligne pointe vers le HAUT (y+1 -> y).
                        // Le côté droit du vecteur est `rightSector`, qui devient la face avant.
                        AddEdge(new Point(x, y + 1), new Point(x, y), rightSector, leftSector);
                    }
                }
            }

            // Création des Linedefs HORIZONTAUX
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    int topSector = (y > 0) ? sectorGrid[x, y - 1] : -1;
                    int bottomSector = (y < height) ? sectorGrid[x, y] : -1;

                    if (topSector != bottomSector)
                    {
                        // Le vecteur de la ligne pointe vers la DROITE (x -> x+1).
                        // Le côté droit du vecteur est `bottomSector`, qui devient la face avant.
                        AddEdge(new Point(x, y), new Point(x + 1, y), bottomSector, topSector);
                    }
                }
            }
        }

        private void AddEdge(Point p1, Point p2, int frontSectorId, int backSectorId)
        {
            // Convention Doom : la face "front" est à droite du vecteur p1 -> p2.
            if (frontSectorId == -1 && backSectorId == -1) return;

            var v1 = GetOrCreateVertex(p1);
            var v2 = GetOrCreateVertex(p2);

            // Si la face avant est le vide (-1), on doit inverser la ligne
            // pour que la face valide (le secteur existant) soit toujours la face avant.
            if (frontSectorId == -1)
            {
                AddLinedef(v2, v1, backSectorId, -1, GetTexture("wall_primary"));
            }
            else
            {
                AddLinedef(v1, v2, frontSectorId, backSectorId, GetTexture("wall_primary"));
            }
        }

        private Vertex GetOrCreateVertex(Point gridPoint)
        {
            if (_vertexNodeMap.TryGetValue(gridPoint, out var vertex)) return vertex;
            var newVertex = AddVertex(gridPoint.X * GenerationConfig.CellSize, gridPoint.Y * GenerationConfig.CellSize);
            _vertexNodeMap[gridPoint] = newVertex;
            return newVertex;
        }

        private void PlaceAllThings(GridCell[,] grid)
        {
            var roomCellGroups = new Dictionary<string, List<Point>>();
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    if (grid[x, y].RoomId != null && !grid[x, y].IsCorridor)
                    {
                        if (!roomCellGroups.ContainsKey(grid[x, y].RoomId)) roomCellGroups[grid[x, y].RoomId] = new List<Point>();
                        roomCellGroups[grid[x, y].RoomId].Add(new Point(x, y));
                    }
                }
            }
            foreach (var roomData in _dgraph.Rooms)
            {
                if (roomCellGroups.TryGetValue(roomData.Id, out var cells))
                {
                    var randomCell = cells[_random.Next(cells.Count)];
                    var bounds = new RectangleF(randomCell.X * GenerationConfig.CellSize, randomCell.Y * GenerationConfig.CellSize, GenerationConfig.CellSize, GenerationConfig.CellSize);
                    PlaceThings(roomData, bounds);
                }
            }
        }

        private Vertex AddVertex(float x, float y) { var v = new Vertex { Id = _nextId++, X = x, Y = y }; _dhemap.Vertices.Add(v); return v; }
        private void AddLinedef(Vertex v1, Vertex v2, int frontSectorId, int backSectorId, string texture) { var frontSideId = AddSidedef(frontSectorId, texture); var line = new Linedef { Id = _nextId++, StartVertex = v1.Id, EndVertex = v2.Id, FrontSidedef = frontSideId }; if (backSectorId != -1) { line.BackSidedef = AddSidedef(backSectorId, texture); line.Flags.Add("twoSided"); } else { line.Flags.Add("impassable"); } _dhemap.Linedefs.Add(line); }
        private int AddSidedef(int sectorId, string texture) { var side = new Sidedef { Id = _nextId++, Sector = sectorId, TextureMiddle = texture, TextureTop = "-", TextureBottom = "-" }; _dhemap.Sidedefs.Add(side); return side.Id; }
        private Sector CreateSectorFromRoom(RoomProperties props) { return new Sector { Id = _nextId++, Tag = props.Tag ?? 0, FloorTexture = GetTexture(props.FloorFlat, "FLAT5_4"), CeilingTexture = (props.Ceiling == "sky" ? "F_SKY1" : GetTexture(props.CeilingFlat, "CEIL1_1")), LightLevel = GetLight(props.LightLevel), FloorHeight = GetHeight(props.Floor), CeilingHeight = GetHeight(props.Ceiling, 128) }; }
        private void PlaceThings(Room roomData, RectangleF bounds) { var items = roomData.Contents?.Items ?? new List<ContentItem>(); var monsters = roomData.Contents?.Monsters ?? new List<ContentItem>(); foreach (var item in items.Concat(monsters)) for (int i = 0; i < item.Count; i++) _dhemap.Things.Add(new Thing { Id = _nextId++, X = bounds.X + (float)(_random.NextDouble() * bounds.Width), Y = bounds.Y + (float)(_random.NextDouble() * bounds.Height), Angle = _random.Next(8) * 45, Type = item.TypeId, Flags = new List<string> { "skillEasy", "skillNormal", "skillHard" } }); }
        private string GetTexture(string concept, string defaultTexture = "STARG1") { if (string.IsNullOrEmpty(concept)) return defaultTexture; if (_dgraph.ThemePalette.TryGetValue(concept, out var textures) && textures.Any()) { int totalWeight = textures.Sum(t => t.Weight); int r = _random.Next(0, totalWeight); foreach (var texture in textures) { if (r < texture.Weight) return texture.Name; r -= texture.Weight; } } return defaultTexture; }
        private int GetHeight(string height, int defaultHeight = 0) => height switch { "very_low" => -32, "low" => 0, "normal" => defaultHeight, "high" => 128, "very_high" => 256, _ => defaultHeight };
        private int GetLight(string light) => light switch { "dark" => 80, "dim" => 120, "normal" => 160, "bright" => 220, "flickering" => 160, _ => 160 };
    }
}