using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Models.Dhemap;

namespace DGraphBuilder.Generation
{
    public class MapBuilder
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;
        private DhemapFile _dhemap;
        private Dictionary<int, Vertex> _vertexLookup;
        private int _nextId;
        private const int CellSize = 1024;

        private class BuiltSectorInfo
        {
            public Sector DhemapSector { get; set; }
            public Dictionary<string, Linedef> Walls { get; set; } = new Dictionary<string, Linedef>();
            public RectangleF BoundingBox { get; set; }
        }

        public MapBuilder(DGraphFile dgraph, Random random)
        {
            _dgraph = dgraph;
            _random = random;
        }

        public DhemapFile Build(GridCell[,] grid)
        {
            InitializeDhemap();
            var builtSectors = new Dictionary<Point, BuiltSectorInfo>();

            // Étape 1 : Construire toutes les pièces et tous les couloirs comme des boîtes fermées
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    var cell = grid[x, y];
                    if (cell.RoomId != null || cell.IsCorridor)
                    {
                        var rect = new RectangleF(x * CellSize, y * CellSize, CellSize, CellSize);
                        RoomProperties props;
                        if (cell.RoomId != null)
                        {
                            var roomData = _dgraph.Rooms.First(r => r.Id == cell.RoomId);
                            props = roomData.Properties;
                            var builtRoom = BuildSectorGeometry(rect, props);
                            PlaceThings(roomData, rect);
                            builtSectors[new Point(x, y)] = builtRoom;
                        }
                        else // C'est un couloir
                        {
                            props = new RoomProperties { Floor = "normal", Ceiling = "normal", LightLevel = "normal", FloorFlat = "floor_primary", CeilingFlat = "ceiling_primary", WallTexture = "wall_primary" };
                            var builtCorridor = BuildSectorGeometry(rect, props);
                            builtSectors[new Point(x, y)] = builtCorridor;
                        }
                    }
                }
            }

            // Étape 2 : Connecter les secteurs adjacents
            Console.WriteLine("\nÉtape 3: Création des connexions entre les secteurs...");
            ConnectAdjacentSectors(grid, builtSectors);

            return _dhemap;
        }

        private void ConnectAdjacentSectors(GridCell[,] grid, Dictionary<Point, BuiltSectorInfo> builtSectors)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (!builtSectors.ContainsKey(new Point(x, y))) continue;

                    var current = builtSectors[new Point(x, y)];

                    // Vérifier le voisin à l'Est
                    var eastNeighborPos = new Point(x + 1, y);
                    if (builtSectors.ContainsKey(eastNeighborPos))
                    {
                        var neighbor = builtSectors[eastNeighborPos];
                        CarveConnection(current, "East", neighbor, "West");
                    }

                    // Vérifier le voisin au Sud (l'axe Y est inversé dans beaucoup de systèmes graphiques)
                    var southNeighborPos = new Point(x, y + 1);
                    if (builtSectors.ContainsKey(southNeighborPos))
                    {
                        var neighbor = builtSectors[southNeighborPos];
                        CarveConnection(current, "South", neighbor, "North");
                    }
                }
            }
        }

        private void CarveConnection(BuiltSectorInfo from, string fromSide, BuiltSectorInfo to, string toSide)
        {
            Console.WriteLine($"    -> Connexion de {fromSide} de {from.DhemapSector.Id} à {toSide} de {to.DhemapSector.Id}.");

            var fromWall = from.Walls[fromSide];
            var toWall = to.Walls[toSide];
            if (fromWall.BackSidedef.HasValue || toWall.BackSidedef.HasValue) return; // Déjà connecté

            float openingSize = 128;
            var fromOpening = SplitLinedef(from, fromWall, openingSize);
            var toOpening = SplitLinedef(to, toWall, openingSize);

            var doorSector = CreateSectorFromRoom(new RoomProperties { Floor = "low", Ceiling = "low", FloorFlat = "floor_primary", CeilingFlat = "ceiling_primary", LightLevel = "normal", WallTexture = "door_frame" });
            _dhemap.Sectors.Add(doorSector);

            AddLinedef(_vertexLookup[fromOpening.opening.EndVertex], _vertexLookup[toOpening.opening.StartVertex], doorSector.Id, GetTexture("door_frame"));
            AddLinedef(_vertexLookup[toOpening.opening.EndVertex], _vertexLookup[fromOpening.opening.StartVertex], doorSector.Id, GetTexture("door_frame"));

            MakeLineTwoSided(fromOpening.opening, doorSector.Id);
            MakeLineTwoSided(toOpening.opening, doorSector.Id);
        }

        private void MakeLineTwoSided(Linedef line, int otherSectorId)
        {
            line.Flags.Remove("impassable");
            if (!line.Flags.Contains("twoSided")) line.Flags.Add("twoSided");
            line.BackSidedef = AddSidedef(otherSectorId, "-");
        }

        private (Linedef opening, Linedef[] newWalls) SplitLinedef(BuiltSectorInfo room, Linedef line, float openingSize)
        {
            _dhemap.Linedefs.Remove(line);
            var wallKey = room.Walls.First(kvp => kvp.Value == line).Key;
            room.Walls.Remove(wallKey);

            var v_start = _vertexLookup[line.StartVertex];
            var v_end = _vertexLookup[line.EndVertex];

            var lineVec = new Vector2(v_end.X - v_start.X, v_end.Y - v_start.Y);
            var lineLength = lineVec.Length();
            lineVec = Vector2.Normalize(lineVec);

            float midPointDist = lineLength / 2f;
            if (openingSize >= lineLength - 16) openingSize = lineLength - 16;

            var v_open1 = AddVertex(v_start.X + lineVec.X * (midPointDist - openingSize / 2), v_start.Y + lineVec.Y * (midPointDist - openingSize / 2));
            var v_open2 = AddVertex(v_start.X + lineVec.X * (midPointDist + openingSize / 2), v_start.Y + lineVec.Y * (midPointDist + openingSize / 2));

            var sideTexture = _dhemap.Sidedefs.First(s => s.Id == line.FrontSidedef).TextureMiddle;

            var wall1 = AddLinedef(v_start, v_open1, room.DhemapSector.Id, sideTexture);
            var opening = AddLinedef(v_open1, v_open2, room.DhemapSector.Id, "-");
            var wall2 = AddLinedef(v_open2, v_end, room.DhemapSector.Id, sideTexture);

            // On ne peut pas simplement ajouter, il faut reconstruire la liste de murs pour garder l'ordre
            return (opening, new[] { wall1, wall2 });
        }

        // --- Le reste des méthodes utilitaires ---
        private void InitializeDhemap() { _dhemap = new DhemapFile { MapInfo = new Models.Dhemap.MapInfo { Game = _dgraph.MapInfo.Game, Map = _dgraph.MapInfo.Map, Name = _dgraph.MapInfo.Name, Music = _dgraph.MapInfo.Music, SkyTexture = "SKY1" }, Vertices = new List<Vertex>(), Linedefs = new List<Linedef>(), Sidedefs = new List<Sidedef>(), Sectors = new List<Sector>(), Things = new List<Thing>() }; _vertexLookup = new Dictionary<int, Vertex>(); _nextId = 0; }
        private BuiltSectorInfo BuildSectorGeometry(RectangleF rect, RoomProperties props) { var sector = CreateSectorFromRoom(props); _dhemap.Sectors.Add(sector); var v1 = AddVertex(rect.Left, rect.Top); var v2 = AddVertex(rect.Right, rect.Top); var v3 = AddVertex(rect.Right, rect.Bottom); var v4 = AddVertex(rect.Left, rect.Bottom); string wallTexture = GetTexture(props.WallTexture); var builtRoom = new BuiltSectorInfo { DhemapSector = sector, BoundingBox = rect }; builtRoom.Walls["North"] = AddLinedef(v1, v2, sector.Id, wallTexture); builtRoom.Walls["East"] = AddLinedef(v2, v3, sector.Id, wallTexture); builtRoom.Walls["South"] = AddLinedef(v3, v4, sector.Id, wallTexture); builtRoom.Walls["West"] = AddLinedef(v4, v1, sector.Id, wallTexture); return builtRoom; }
        private Vertex AddVertex(float x, float y) { var v = new Vertex { Id = _nextId++, X = x, Y = y }; _dhemap.Vertices.Add(v); _vertexLookup[v.Id] = v; return v; }
        private Linedef AddLinedef(Vertex v1, Vertex v2, int sectorId, string texture) { var sideId = AddSidedef(sectorId, texture); var line = new Linedef { Id = _nextId++, StartVertex = v1.Id, EndVertex = v2.Id, FrontSidedef = sideId }; line.Flags.Add("impassable"); _dhemap.Linedefs.Add(line); return line; }
        private int AddSidedef(int sectorId, string texture) { var side = new Sidedef { Id = _nextId++, Sector = sectorId, TextureMiddle = texture, TextureTop = "-", TextureBottom = "-" }; _dhemap.Sidedefs.Add(side); return side.Id; }
        private Sector CreateSectorFromRoom(RoomProperties props) { return new Sector { Id = _nextId++, Tag = props.Tag ?? 0, FloorTexture = GetTexture(props.FloorFlat), CeilingTexture = (props.Ceiling == "sky" ? "F_SKY1" : GetTexture(props.CeilingFlat)), LightLevel = GetLight(props.LightLevel), FloorHeight = GetHeight(props.Floor), CeilingHeight = GetHeight(props.Ceiling, 128) }; }
        private void PlaceThings(Room roomData, RectangleF bounds) { var items = roomData.Contents?.Items ?? new List<ContentItem>(); var monsters = roomData.Contents?.Monsters ?? new List<ContentItem>(); foreach (var item in items.Concat(monsters)) for (int i = 0; i < item.Count; i++) _dhemap.Things.Add(new Thing { Id = _nextId++, X = bounds.X + (float)(_random.NextDouble() * bounds.Width), Y = bounds.Y + (float)(_random.NextDouble() * bounds.Height), Angle = _random.Next(8) * 45, Type = item.TypeId, Flags = new List<string> { "skillEasy", "skillNormal", "skillHard" } }); }
        private string GetTexture(string concept) { if (string.IsNullOrEmpty(concept)) return "STARG1"; if (_dgraph.ThemePalette.TryGetValue(concept, out var textures) && textures.Any()) { int totalWeight = textures.Sum(t => t.Weight); int r = _random.Next(0, totalWeight); foreach (var texture in textures) { if (r < texture.Weight) return texture.Name; r -= texture.Weight; } } return "STARG1"; }
        private int GetHeight(string height, int defaultHeight = 0) => height switch { "very_low" => -32, "low" => 0, "normal" => defaultHeight, "high" => 128, "very_high" => 256, _ => defaultHeight };
        private int GetLight(string light) => light switch { "dark" => 80, "dim" => 120, "normal" => 160, "bright" => 220, "flickering" => 160, _ => 160 };
    }
}