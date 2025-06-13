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

        private class BuiltRoom { public PhysicalRoom PlacedRoom; public Sector DhemapSector; public List<Linedef> Linedefs = new List<Linedef>(); }

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

            builtRoom.Linedefs.Add(AddLinedef(v1, v2, sector.Id, wallTexture));
            builtRoom.Linedefs.Add(AddLinedef(v2, v3, sector.Id, wallTexture));
            builtRoom.Linedefs.Add(AddLinedef(v3, v4, sector.Id, wallTexture));
            builtRoom.Linedefs.Add(AddLinedef(v4, v1, sector.Id, wallTexture));

            PlaceThings(pRoom.DGraphRoom, rect);
            return builtRoom;
        }

        private void CarveConnection(BuiltRoom from, BuiltRoom to, Connection connection)
        {
            Console.WriteLine($"    -> Création de la connexion '{connection.Type}' entre '{from.PlacedRoom.DGraphRoom.Id}' et '{to.PlacedRoom.DGraphRoom.Id}'.");

            var (fromWall, toWall) = FindClosestWalls(from, to);
            if (fromWall == null || toWall == null) return;

            float openingSize = 64;
            var fromOpening = SplitLinedef(from, fromWall, openingSize);
            var toOpening = SplitLinedef(to, toWall, openingSize);

            var doorSector = new Sector
            {
                Id = _nextId++,
                Tag = _nextId,
                FloorHeight = Math.Min(from.DhemapSector.FloorHeight, to.DhemapSector.FloorHeight),
                CeilingHeight = Math.Min(from.DhemapSector.FloorHeight, to.DhemapSector.FloorHeight),
                FloorTexture = from.DhemapSector.FloorTexture,
                CeilingTexture = from.DhemapSector.CeilingTexture,
                LightLevel = 160
            };
            _dhemap.Sectors.Add(doorSector);

            // Relier les ouvertures pour fermer le secteur porte
            var jamb1 = AddLinedef(_vertexLookup[fromOpening.opening.EndVertex], _vertexLookup[toOpening.opening.StartVertex], doorSector.Id, GetTexture("door_frame"));
            var jamb2 = AddLinedef(_vertexLookup[toOpening.opening.EndVertex], _vertexLookup[fromOpening.opening.StartVertex], doorSector.Id, GetTexture("door_frame"));

            // Mettre à jour les back-sidedefs des ouvertures pour pointer vers le nouveau secteur porte
            fromOpening.opening.BackSidedef = AddSidedef(doorSector.Id, "-");
            fromOpening.opening.Flags.Add("twoSided");
            fromOpening.opening.Flags.Remove("impassable");

            toOpening.opening.BackSidedef = AddSidedef(doorSector.Id, "-");
            toOpening.opening.Flags.Add("twoSided");
            toOpening.opening.Flags.Remove("impassable");

            // Appliquer l'action de porte
            if (connection.Type.Contains("door"))
            {
                // La porte est activée depuis l'un des deux côtés
                fromOpening.opening.Action = new ActionInfo { Special = 1, Tag = doorSector.Tag };
                toOpening.opening.Action = new ActionInfo { Special = 1, Tag = doorSector.Tag };
                // La porte elle-même se lève
                doorSector.CeilingHeight = Math.Min(from.DhemapSector.CeilingHeight, to.DhemapSector.CeilingHeight);
            }
        }

        private (Linedef opening, Linedef[] newWalls) SplitLinedef(BuiltRoom room, Linedef line, float openingSize)
        {
            _dhemap.Linedefs.Remove(line);
            room.Linedefs.Remove(line);

            var v_start = _vertexLookup[line.StartVertex];
            var v_end = _vertexLookup[line.EndVertex];

            var lineVec = new Vector2(v_end.X - v_start.X, v_end.Y - v_start.Y);
            var lineLength = lineVec.Length();
            lineVec = Vector2.Normalize(lineVec);

            float midPointDist = lineLength / 2f;
            if (openingSize > lineLength - 16) openingSize = lineLength - 16; // Assurer qu'il reste du mur

            var v_open1 = AddVertex(v_start.X + lineVec.X * (midPointDist - openingSize / 2), v_start.Y + lineVec.Y * (midPointDist - openingSize / 2));
            var v_open2 = AddVertex(v_start.X + lineVec.X * (midPointDist + openingSize / 2), v_start.Y + lineVec.Y * (midPointDist + openingSize / 2));

            var sideTexture = _dhemap.Sidedefs.First(s => s.Id == line.FrontSidedef).TextureMiddle;

            var wall1 = AddLinedef(v_start, v_open1, room.DhemapSector.Id, sideTexture);
            var opening = AddLinedef(v_open1, v_open2, room.DhemapSector.Id, "-"); // L'ouverture n'a pas de texture
            var wall2 = AddLinedef(v_open2, v_end, room.DhemapSector.Id, sideTexture);

            room.Linedefs.AddRange(new[] { wall1, opening, wall2 });
            return (opening, new[] { wall1, wall2 });
        }

        private (Linedef, Linedef) FindClosestWalls(BuiltRoom r1, BuiltRoom r2)
        {
            Linedef bestL1 = null, bestL2 = null;
            float minDist = float.MaxValue;

            foreach (var l1 in r1.Linedefs.Where(l => l.BackSidedef == null))
            {
                foreach (var l2 in r2.Linedefs.Where(l => l.BackSidedef == null))
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
        private Linedef AddLinedef(Vertex v1, Vertex v2, int sectorId, string texture) { var sideId = AddSidedef(sectorId, texture); var line = new Linedef { Id = _nextId++, StartVertex = v1.Id, EndVertex = v2.Id, FrontSidedef = sideId }; line.Flags.Add("impassable"); _dhemap.Linedefs.Add(line); return line; }
        private int AddSidedef(int sectorId, string texture) { var side = new Sidedef { Id = _nextId++, Sector = sectorId, TextureMiddle = texture, TextureTop = "-", TextureBottom = "-" }; _dhemap.Sidedefs.Add(side); return side.Id; }
        private Sector CreateSectorFromRoom(Room roomData) { return new Sector { Id = _nextId++, Tag = roomData.Properties.Tag ?? 0, FloorTexture = GetTexture(roomData.Properties.FloorFlat), CeilingTexture = (roomData.Properties.Ceiling == "sky" ? "F_SKY1" : GetTexture(roomData.Properties.CeilingFlat)), LightLevel = GetLight(roomData.Properties.LightLevel), FloorHeight = GetHeight(roomData.Properties.Floor), CeilingHeight = GetHeight(roomData.Properties.Ceiling, 128) }; }
        private void PlaceThings(Room roomData, RectangleF bounds) { var items = roomData.Contents?.Items ?? new List<ContentItem>(); var monsters = roomData.Contents?.Monsters ?? new List<ContentItem>(); foreach (var item in items.Concat(monsters)) for (int i = 0; i < item.Count; i++) _dhemap.Things.Add(new Thing { Id = _nextId++, X = bounds.X + (float)(_random.NextDouble() * bounds.Width), Y = bounds.Y + (float)(_random.NextDouble() * bounds.Height), Angle = _random.Next(8) * 45, Type = item.TypeId, Flags = new List<string> { "skillEasy", "skillNormal", "skillHard" } }); }
        private string GetTexture(string concept) { if (string.IsNullOrEmpty(concept)) return "STARG1"; if (_dgraph.ThemePalette.TryGetValue(concept, out var textures) && textures.Any()) { int totalWeight = textures.Sum(t => t.Weight); int r = _random.Next(0, totalWeight); foreach (var texture in textures) { if (r < texture.Weight) return texture.Name; r -= texture.Weight; } } return "STARG1"; }
        private int GetHeight(string height, int defaultHeight = 0) => height switch { "very_low" => -32, "low" => 0, "normal" => defaultHeight, "high" => 128, "very_high" => 256, _ => defaultHeight };
        private int GetLight(string light) => light switch { "dark" => 80, "dim" => 120, "normal" => 160, "bright" => 220, "flickering" => 160, _ => 160 };
    }
}