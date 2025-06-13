using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Models.Dhemap;

namespace DGraphBuilder.Generation
{
    /// <summary>
    /// Construit la géométrie DHEMap à partir d'une disposition physique calculée.
    /// </summary>
    public class MapBuilder
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;
        private DhemapFile _dhemap;
        private Dictionary<int, Vertex> _vertexLookup;
        private int _nextId;

        // Classe interne pour stocker la géométrie construite d'une pièce.
        private class BuiltRoom { public PhysicalRoom PlacedRoom; public Sector DhemapSector; public Dictionary<string, Linedef> Walls = new Dictionary<string, Linedef>(); }

        public MapBuilder(DGraphFile dgraph, Random random)
        {
            _dgraph = dgraph;
            _random = random;
        }

        public DhemapFile Build(List<PhysicalRoom> layout)
        {
            InitializeDhemap();
            var roomToGeoMap = new Dictionary<string, BuiltRoom>();

            foreach (var pRoom in layout)
            {
                Console.WriteLine($"  Construction de la pièce : {pRoom.DGraphRoom.Id}");
                var builtRoom = BuildRoomGeometry(pRoom);
                roomToGeoMap[pRoom.DGraphRoom.Id] = builtRoom;
            }

            foreach (var connection in _dgraph.Connections)
            {
                if (roomToGeoMap.TryGetValue(connection.FromRoom, out var fromRoom) &&
                    roomToGeoMap.TryGetValue(connection.ToRoom, out var toRoom))
                {
                    CarveConnection(fromRoom, toRoom, connection);
                }
            }

            // La logique pour les pièces imbriquées doit être appelée ici, après la construction principale.
            // Pour l'instant, cette fonctionnalité reste simplifiée.

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
            _vertexLookup = new Dictionary<int, Vertex>();
            _nextId = 0;
        }

        private BuiltRoom BuildRoomGeometry(PhysicalRoom pRoom)
        {
            var rect = pRoom.GetBoundingBox();
            var sector = CreateSectorFromRoom(pRoom.DGraphRoom);
            _dhemap.Sectors.Add(sector);

            var v1 = AddVertex(rect.Left, rect.Top);
            var v2 = AddVertex(rect.Right, rect.Top);
            var v3 = AddVertex(rect.Right, rect.Bottom);
            var v4 = AddVertex(rect.Left, rect.Bottom);

            string wallTexture = GetTexture(pRoom.DGraphRoom.Properties.WallTexture);

            var builtRoom = new BuiltRoom { PlacedRoom = pRoom, DhemapSector = sector };
            builtRoom.Walls["North"] = AddLinedef(v4, v3, sector.Id, wallTexture);
            builtRoom.Walls["East"] = AddLinedef(v3, v2, sector.Id, wallTexture);
            builtRoom.Walls["South"] = AddLinedef(v2, v1, sector.Id, wallTexture);
            builtRoom.Walls["West"] = AddLinedef(v1, v4, sector.Id, wallTexture);

            PlaceThings(pRoom.DGraphRoom, rect);
            return builtRoom;
        }

        private void CarveConnection(BuiltRoom from, BuiltRoom to, Connection connection)
        {
            Console.WriteLine($"    -> Création de la connexion '{connection.Type}' entre '{from.PlacedRoom.DGraphRoom.Id}' et '{to.PlacedRoom.DGraphRoom.Id}'.");

            var (fromWall, toWall) = FindClosestWalls(from, to);
            if (fromWall == null || toWall == null) return;

            MakeLineTwoSided(fromWall, to.DhemapSector.Id, connection, from);
            _dhemap.Linedefs.Remove(toWall);
        }

        private void MakeLineTwoSided(Linedef line, int otherSectorId, Connection connection, BuiltRoom fromRoom)
        {
            line.Flags.Remove("impassable");
            if (!line.Flags.Contains("twoSided")) line.Flags.Add("twoSided");

            // CORRECTION : Le chemin d'accès correct est via PlacedRoom.
            string texture = GetTexture(connection.Type.Contains("door") ? "door_frame" : fromRoom.PlacedRoom.DGraphRoom.Properties.WallTexture);
            var backSidedef = new Sidedef { Id = _nextId++, Sector = otherSectorId, TextureMiddle = texture, TextureTop = "-", TextureBottom = "-" };
            _dhemap.Sidedefs.Add(backSidedef);
            line.BackSidedef = backSidedef.Id;

            if (connection.Type.Contains("door"))
            {
                line.Action = new ActionInfo { Special = 1, Tag = line.Id + 1000 }; // Tagging simple pour l'exemple
                // Une implémentation complète créerait un vrai secteur de porte et assignerait son tag.
            }
        }

        private (Linedef, Linedef) FindClosestWalls(BuiltRoom r1, BuiltRoom r2)
        {
            Linedef bestL1 = null, bestL2 = null;
            float minDist = float.MaxValue;

            foreach (var l1 in r1.Walls.Values.Where(l => l.BackSidedef == null))
            {
                foreach (var l2 in r2.Walls.Values.Where(l => l.BackSidedef == null))
                {
                    var mid1 = GetLinedefMidpoint(l1);
                    var mid2 = GetLinedefMidpoint(l2);

                    float dist = Vector2.DistanceSquared(mid1, mid2);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestL1 = l1;
                        bestL2 = l2;
                    }
                }
            }
            return (bestL1, bestL2);
        }

        private Vector2 GetLinedefMidpoint(Linedef line) { var v1 = _vertexLookup[line.StartVertex]; var v2 = _vertexLookup[line.EndVertex]; return new Vector2((v1.X + v2.X) / 2, (v1.Y + v2.Y) / 2); }
        private Vertex AddVertex(float x, float y) { var v = new Vertex { Id = _nextId++, X = x, Y = y }; _dhemap.Vertices.Add(v); _vertexLookup[v.Id] = v; return v; }
        private Linedef AddLinedef(Vertex v1, Vertex v2, int sectorId, string texture, bool isTwoSided = false) { var sideId = AddSidedef(sectorId, texture); var line = new Linedef { Id = _nextId++, StartVertex = v1.Id, EndVertex = v2.Id, FrontSidedef = sideId }; if (isTwoSided) { line.BackSidedef = AddSidedef(sectorId, texture); line.Flags.Add("twoSided"); } else { line.Flags.Add("impassable"); } _dhemap.Linedefs.Add(line); return line; }
        private int AddSidedef(int sectorId, string texture) { var side = new Sidedef { Id = _nextId++, Sector = sectorId, TextureMiddle = texture, TextureTop = "-", TextureBottom = "-" }; _dhemap.Sidedefs.Add(side); return side.Id; }
        private Sector CreateSectorFromRoom(Room roomData) { return new Sector { Id = _nextId++, Tag = roomData.Properties.Tag ?? 0, FloorTexture = GetTexture(roomData.Properties.FloorFlat), CeilingTexture = (roomData.Properties.Ceiling == "sky" ? "F_SKY1" : GetTexture(roomData.Properties.CeilingFlat)), LightLevel = GetLight(roomData.Properties.LightLevel), FloorHeight = GetHeight(roomData.Properties.Floor), CeilingHeight = GetHeight(roomData.Properties.Ceiling, 128) }; }
        private void PlaceThings(Room roomData, RectangleF bounds) { var items = roomData.Contents?.Items ?? new List<ContentItem>(); var monsters = roomData.Contents?.Monsters ?? new List<ContentItem>(); foreach (var item in items.Concat(monsters)) for (int i = 0; i < item.Count; i++) _dhemap.Things.Add(new Thing { Id = _nextId++, X = bounds.X + (float)(_random.NextDouble() * bounds.Width), Y = bounds.Y + (float)(_random.NextDouble() * bounds.Height), Angle = _random.Next(8) * 45, Type = item.TypeId, Flags = new List<string> { "skillEasy", "skillNormal", "skillHard" } }); }
        private string GetTexture(string concept) { if (string.IsNullOrEmpty(concept)) return "STARG1"; if (_dgraph.ThemePalette.TryGetValue(concept, out var textures) && textures.Any()) { int totalWeight = textures.Sum(t => t.Weight); int r = _random.Next(0, totalWeight); foreach (var texture in textures) { if (r < texture.Weight) return texture.Name; r -= texture.Weight; } } return "STARG1"; }
        private int GetHeight(string height, int defaultHeight = 0) => height switch { "very_low" => -32, "low" => 0, "normal" => defaultHeight, "high" => 128, "very_high" => 256, _ => defaultHeight };
        private int GetLight(string light) => light switch { "dark" => 80, "dim" => 120, "normal" => 160, "bright" => 220, "flickering" => 160, _ => 160 };
    }
}