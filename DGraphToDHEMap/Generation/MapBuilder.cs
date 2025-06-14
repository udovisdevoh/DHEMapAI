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
        private Dictionary<PointF, int> _vertexMap;
        private int _nextId;

        public MapBuilder(DGraphFile dgraph, Random random)
        {
            _dgraph = dgraph;
            _random = random;
        }

        public DhemapFile Build(Dictionary<string, Polygon> layout)
        {
            InitializeDhemap();
            var sectorIdMap = CreateSectorsFromPolygons(layout);
            CreateOpenings(layout, sectorIdMap);
            PlaceAllThings(layout);
            PlaceAllFeatures(layout, sectorIdMap);
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
            _vertexMap = new Dictionary<PointF, int>();
            _nextId = 0;
        }

        private Dictionary<string, int> CreateSectorsFromPolygons(Dictionary<string, Polygon> layout)
        {
            var sectorIdMap = new Dictionary<string, int>();
            foreach (var kvp in layout)
            {
                string id = kvp.Key;
                Polygon polygon = kvp.Value;
                var roomData = _dgraph.Rooms.FirstOrDefault(r => r.Id == id);
                Sector sector = (roomData != null) ? CreateSectorFromRoom(roomData.Properties) : CreateSectorFromRoom(new RoomProperties { Floor = "normal", Ceiling = "normal", LightLevel = "normal", WallTexture = "wall_accent", FloorFlat = "floor_primary", CeilingFlat = "ceiling_primary" });
                _dhemap.Sectors.Add(sector);
                sectorIdMap[id] = sector.Id;

                var vertexIds = polygon.Vertices.Select(GetOrCreateVertex).ToList();
                for (int i = 0; i < vertexIds.Count; i++)
                {
                    int startVertexId = vertexIds[i];
                    int endVertexId = vertexIds[(i + 1) % vertexIds.Count];
                    string wallTexture = GetTexture(roomData?.Properties.WallTexture ?? "wall_primary");
                    AddLinedef(startVertexId, endVertexId, sector.Id, -1, wallTexture);
                }
            }
            return sectorIdMap;
        }

        private void CreateOpenings(Dictionary<string, Polygon> layout, Dictionary<string, int> sectorIdMap)
        {
            foreach (var connection in _dgraph.Connections)
            {
                string corridorId = $"corridor_{connection.FromRoom}_{connection.ToRoom}";
                if (!layout.ContainsKey(connection.FromRoom) || !layout.ContainsKey(connection.ToRoom) || !layout.ContainsKey(corridorId)) continue;

                MakeConnection(layout[connection.FromRoom], layout[corridorId], sectorIdMap[connection.FromRoom], sectorIdMap[corridorId], connection);
                MakeConnection(layout[connection.ToRoom], layout[corridorId], sectorIdMap[connection.ToRoom], sectorIdMap[corridorId], connection);
            }
        }

        private void MakeConnection(Polygon poly1, Polygon poly2, int sector1Id, int sector2Id, Connection connection)
        {
            var wallsOfPoly1 = _dhemap.Linedefs.Where(l => l.FrontSidedef != -1 && _dhemap.Sidedefs.First(s => s.Id == l.FrontSidedef).Sector == sector1Id);
            Linedef wall1 = FindClosestLinedefToPolygonCenter(wallsOfPoly1, poly2);

            var wallsOfPoly2 = _dhemap.Linedefs.Where(l => l.FrontSidedef != -1 && _dhemap.Sidedefs.First(s => s.Id == l.FrontSidedef).Sector == sector2Id);
            Linedef wall2 = FindClosestLinedefToPolygonCenter(wallsOfPoly2, poly1);

            if (wall1 == null || wall2 == null) return;

            string texture1 = _dhemap.Sidedefs.First(s => s.Id == wall1.FrontSidedef).TextureMiddle;
            string texture2 = _dhemap.Sidedefs.First(s => s.Id == wall2.FrontSidedef).TextureMiddle;

            _dhemap.Linedefs.Remove(wall1);
            _dhemap.Linedefs.Remove(wall2);

            Vertex v1s = _dhemap.Vertices.First(v => v.Id == wall1.StartVertex);
            Vertex v1e = _dhemap.Vertices.First(v => v.Id == wall1.EndVertex);
            Vertex v2s = _dhemap.Vertices.First(v => v.Id == wall2.StartVertex);
            Vertex v2e = _dhemap.Vertices.First(v => v.Id == wall2.EndVertex);

            float openingWidth = 128f;
            var (p1, p2) = GetSplitPointsOnLine(v1s, v1e, openingWidth);
            int newV1Id = GetOrCreateVertex(p1);
            int newV2Id = GetOrCreateVertex(p2);

            AddLinedef(v1s.Id, newV1Id, sector1Id, -1, texture1);
            AddLinedef(newV2Id, v1e.Id, sector1Id, -1, texture1);

            var (p3, p4) = GetSplitPointsOnLine(v2s, v2e, openingWidth);
            int newV3Id = GetOrCreateVertex(p3);
            int newV4Id = GetOrCreateVertex(p4);

            AddLinedef(v2s.Id, newV3Id, sector2Id, -1, texture2);
            AddLinedef(newV4Id, v2e.Id, sector2Id, -1, texture2);

            string doorTexture = GetTexture(connection.Properties?.Texture, "DOORTRAK");
            var openingLinedef = AddLinedef(newV1Id, newV2Id, sector1Id, sector2Id, doorTexture);
            AddLinedef(newV4Id, newV3Id, sector2Id, sector1Id, doorTexture);

            AddLinedef(newV1Id, newV3Id, sector2Id, -1, "DOORJAMB");
            AddLinedef(newV2Id, newV4Id, sector2Id, -1, "DOORJAMB");

            if (connection.Type == "door" || connection.Type == "locked_door")
            {
                openingLinedef.Flags.Add("playerUse");
                int tag = _nextId++;
                openingLinedef.Action = new ActionInfo { Special = 1, Tag = tag };
                _dhemap.Sectors.First(s => s.Id == sector1Id).Tag = tag;
            }
        }

        private (PointF, PointF) GetSplitPointsOnLine(Vertex start, Vertex end, float width)
        {
            PointF mid = new PointF((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            PointF vec = new PointF(end.X - start.X, end.Y - start.Y);
            float len = (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
            PointF unitVec = (len > 0) ? new PointF(vec.X / len, vec.Y / len) : new PointF(0, 0);

            float halfWidth = Math.Min(width / 2, len / 2 - 1);

            PointF p1 = new PointF(mid.X - unitVec.X * halfWidth, mid.Y - unitVec.Y * halfWidth);
            PointF p2 = new PointF(mid.X + unitVec.X * halfWidth, mid.Y + unitVec.Y * halfWidth);
            return (p1, p2);
        }

        private Linedef FindClosestLinedefToPolygonCenter(IEnumerable<Linedef> linedefs, Polygon targetPoly)
        {
            if (!linedefs.Any()) return null;
            PointF center = targetPoly.GetCenter();
            return linedefs.OrderBy(line => {
                var v1 = _dhemap.Vertices.First(v => v.Id == line.StartVertex);
                var v2 = _dhemap.Vertices.First(v => v.Id == line.EndVertex);
                PointF midPoint = new PointF((v1.X + v2.X) / 2, (v1.Y + v2.Y) / 2);
                return (center.X - midPoint.X) * (center.X - midPoint.X) + (center.Y - midPoint.Y) * (center.Y - midPoint.Y);
            }).FirstOrDefault();
        }

        private int GetOrCreateVertex(PointF p)
        {
            if (_vertexMap.TryGetValue(p, out int vertexId)) return vertexId;
            var newVertex = new Vertex { Id = _nextId++, X = p.X, Y = p.Y };
            _dhemap.Vertices.Add(newVertex);
            _vertexMap[p] = newVertex.Id;
            return newVertex.Id;
        }

        private void PlaceAllThings(Dictionary<string, Polygon> layout) { foreach (var roomData in _dgraph.Rooms) { if (layout.TryGetValue(roomData.Id, out var polygon)) { PlaceThingsInPolygon(roomData.Contents, polygon); } } }
        private void PlaceAllFeatures(Dictionary<string, Polygon> layout, Dictionary<string, int> sectorIdMap) { foreach (var roomData in _dgraph.Rooms) { if (roomData.Features == null || !roomData.Features.Any() || !layout.TryGetValue(roomData.Id, out var polygon)) continue; int roomSectorId = sectorIdMap[roomData.Id]; var walls = _dhemap.Linedefs.Where(l => l.BackSidedef == null && _dhemap.Sidedefs.First(s => s.Id == l.FrontSidedef).Sector == roomSectorId).ToList(); if (!walls.Any()) continue; foreach (var feature in roomData.Features) { for (int i = 0; i < feature.Count; i++) { if (!walls.Any()) break; var wallToPlaceOn = walls[_random.Next(walls.Count)]; wallToPlaceOn.Flags.Add("playerUse"); wallToPlaceOn.Action = new ActionInfo { Special = feature.ActionId, Tag = feature.Properties?.TargetTag ?? 0 }; var sidedef = _dhemap.Sidedefs.First(s => s.Id == wallToPlaceOn.FrontSidedef); sidedef.TextureMiddle = GetTexture(feature.Properties.Texture, "SW1ON"); walls.Remove(wallToPlaceOn); } } } }
        private Linedef AddLinedef(int v1Id, int v2Id, int frontSectorId, int backSectorId, string texture) { var frontSideId = AddSidedef(frontSectorId, texture); var line = new Linedef { Id = _nextId++, StartVertex = v1Id, EndVertex = v2Id, FrontSidedef = frontSideId }; if (backSectorId != -1) { line.BackSidedef = AddSidedef(backSectorId, texture); line.Flags.Add("twoSided"); } else { line.Flags.Add("impassable"); } _dhemap.Linedefs.Add(line); return line; }
        private int AddSidedef(int sectorId, string texture) { var side = new Sidedef { Id = _nextId++, Sector = sectorId, TextureMiddle = texture, }; _dhemap.Sidedefs.Add(side); return side.Id; }
        private Sector CreateSectorFromRoom(RoomProperties props) { var roomData = _dgraph.Rooms.FirstOrDefault(r => r.Properties.Tag == props.Tag && props.Tag.HasValue); int tag = props.Tag ?? (roomData != null ? _dhemap.Sectors.Count + 1 : 0); return new Sector { Id = _nextId++, Tag = tag, FloorTexture = GetTexture(props.FloorFlat, "FLAT5_4"), CeilingTexture = (props.Ceiling == "sky" ? "F_SKY1" : GetTexture(props.CeilingFlat, "CEIL1_1")), LightLevel = GetLight(props.LightLevel), FloorHeight = GetHeight(props.Floor), CeilingHeight = GetHeight(props.Ceiling, 128) }; }
        private void PlaceThingsInPolygon(Contents contents, Polygon polygon) { if (contents == null) return; var allItems = (contents.Items ?? new List<ContentItem>()).Concat(contents.Monsters ?? new List<ContentItem>()).Concat(contents.Decorations ?? new List<ContentItem>()); var bounds = GetPolygonBounds(polygon); foreach (var item in allItems) for (int i = 0; i < item.Count; i++) { PointF pos = GetRandomPointInPolygon(polygon, bounds); _dhemap.Things.Add(new Thing { Id = _nextId++, X = pos.X, Y = pos.Y, Angle = _random.Next(8) * 45, Type = item.TypeId, Flags = new List<string> { "skillEasy", "skillNormal", "skillHard" } }); } }
        private RectangleF GetPolygonBounds(Polygon polygon) { if (!polygon.Vertices.Any()) return RectangleF.Empty; float minX = polygon.Vertices.Min(v => v.X); float maxX = polygon.Vertices.Max(v => v.X); float minY = polygon.Vertices.Min(v => v.Y); float maxY = polygon.Vertices.Max(v => v.Y); return new RectangleF(minX, minY, maxX - minX, maxY - minY); }
        private PointF GetRandomPointInPolygon(Polygon polygon, RectangleF bounds) { if (!polygon.Vertices.Any()) return PointF.Empty; for (int k = 0; k < 50; k++) { float x = bounds.X + (float)(_random.NextDouble() * bounds.Width); float y = bounds.Y + (float)(_random.NextDouble() * bounds.Height); var testPoint = new PointF(x, y); if (IsPointInPolygon(polygon, testPoint)) return testPoint; } return polygon.GetCenter(); }
        private bool IsPointInPolygon(Polygon polygon, PointF testPoint) { bool result = false; int j = polygon.Vertices.Count - 1; for (int i = 0; i < polygon.Vertices.Count; i++) { if (polygon.Vertices[i].Y < testPoint.Y && polygon.Vertices[j].Y >= testPoint.Y || polygon.Vertices[j].Y < testPoint.Y && polygon.Vertices[i].Y >= testPoint.Y) { if (polygon.Vertices[i].X + (testPoint.Y - polygon.Vertices[i].Y) / (polygon.Vertices[j].Y - polygon.Vertices[i].Y) * (polygon.Vertices[j].X - polygon.Vertices[i].X) < testPoint.X) { result = !result; } } j = i; } return result; }
        private string GetTexture(string concept, string defaultTexture = "STARG1") { if (string.IsNullOrEmpty(concept)) return defaultTexture; if (_dgraph.ThemePalette.TryGetValue(concept, out var textures) && textures.Any()) { int totalWeight = textures.Sum(t => t.Weight); int r = _random.Next(0, totalWeight); foreach (var texture in textures) { if (r < texture.Weight) return texture.Name; r -= texture.Weight; } } return defaultTexture; }
        private int GetHeight(string height, int defaultHeight = 0) => height switch { "very_low" => -32, "low" => 0, "normal" => defaultHeight, "high" => 128, "very_high" => 256, _ => defaultHeight };
        private int GetLight(string light) => light switch { "very_dark" => 60, "dark" => 80, "normal" => 160, "bright" => 220, "flickering" => 160, "strobe" => 160, _ => 160 };
    }
}