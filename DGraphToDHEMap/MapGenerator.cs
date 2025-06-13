using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Models.Dhemap;

namespace DGraphBuilder.Generation
{
    public class MapGenerator
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;
        private readonly DhemapFile _dhemap = new DhemapFile();
        private readonly Dictionary<string, PlacedRoom> _placedRooms = new Dictionary<string, PlacedRoom>();

        // CORRECTION : Dictionnaire pour une recherche rapide et sûre des sommets par leur ID.
        private readonly Dictionary<int, Vertex> _vertexLookup = new Dictionary<int, Vertex>();

        private int _nextId = 0;

        // Classe interne pour suivre l'état d'une pièce générée
        private class PlacedRoom
        {
            public Room DGraphRoom { get; set; }
            public Sector DhemapSector { get; set; }
            public List<Linedef> Linedefs { get; set; } = new List<Linedef>();
            public RectangleF BoundingBox { get; set; }
        }

        public MapGenerator(DGraphFile dgraph, int? seed)
        {
            _dgraph = dgraph;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public DhemapFile Generate()
        {
            InitializeDhemap();

            var startRoomData = _dgraph.Rooms.FirstOrDefault(r => r.Contents?.Items?.Any(i => i.Name == "Player1Start") ?? false)
                              ?? _dgraph.Rooms.First(r => r.ParentRoom == null);

            var queue = new Queue<Room>();
            queue.Enqueue(startRoomData);

            PlaceFirstRoom(startRoomData);

            while (queue.Count > 0)
            {
                var currentRoomData = queue.Dequeue();
                var connections = _dgraph.Connections.Where(c => (c.FromRoom == currentRoomData.Id && !_placedRooms.ContainsKey(c.ToRoom)) ||
                                                                (c.ToRoom == currentRoomData.Id && !_placedRooms.ContainsKey(c.FromRoom)));

                foreach (var connection in connections)
                {
                    string neighborId = connection.FromRoom == currentRoomData.Id ? connection.ToRoom : connection.FromRoom;
                    if (string.IsNullOrEmpty(neighborId) || _placedRooms.ContainsKey(neighborId)) continue;

                    var neighborRoomData = _dgraph.Rooms.First(r => r.Id == neighborId);

                    if (TryPlaceNeighbor(currentRoomData, neighborRoomData, connection))
                    {
                        queue.Enqueue(neighborRoomData);
                    }
                    else
                    {
                        Console.WriteLine($"Avertissement : Impossible de trouver une position sans collision pour la pièce '{neighborId}'.");
                    }
                }
            }

            PlaceNestedRooms();
            return _dhemap;
        }

        private void InitializeDhemap()
        {
            _dhemap.MapInfo = new Models.Dhemap.MapInfo { Game = _dgraph.MapInfo.Game, Map = _dgraph.MapInfo.Map, Name = _dgraph.MapInfo.Name, Music = _dgraph.MapInfo.Music, SkyTexture = "SKY1" };
            _dhemap.Vertices = new List<Vertex>();
            _dhemap.Linedefs = new List<Linedef>();
            _dhemap.Sidedefs = new List<Sidedef>();
            _dhemap.Sectors = new List<Sector>();
            _dhemap.Things = new List<Thing>();
        }

        private void PlaceFirstRoom(Room roomData)
        {
            Console.WriteLine($"Placement de la pièce de départ : {roomData.Id}");
            var rect = new RectangleF(0, 0, _random.Next(384, 513), _random.Next(384, 513));
            CreateRoomGeometry(roomData, rect);
        }

        private bool TryPlaceNeighbor(Room from, Room to, Connection connection)
        {
            var parentPlacedRoom = _placedRooms[from.Id];
            float neighborWidth = _random.Next(256, 513);
            float neighborHeight = _random.Next(256, 513);
            int spacing = connection.Type == "opening" ? 0 : 64; // Espace pour les portes

            var bestPlacement = new { Box = RectangleF.Empty, Score = float.MaxValue, WallToConnect = (Linedef)null };

            // Essayer de se connecter à chaque mur de la pièce parente
            foreach (var wall in parentPlacedRoom.Linedefs.ToList()) // ToList pour éviter les modifs pendant l'itération
            {
                if (wall.BackSidedef != null) continue; // Mur déjà connecté

                var startV = _vertexLookup[wall.StartVertex];
                var endV = _vertexLookup[wall.EndVertex];

                var midPoint = new PointF((startV.X + endV.X) / 2, (startV.Y + endV.Y) / 2);
                var normal = new Vector2(endV.Y - startV.Y, startV.X - endV.X);
                normal = Vector2.Normalize(normal);

                var position = new PointF(midPoint.X + normal.X * (spacing) - neighborWidth / 2, midPoint.Y + normal.Y * (spacing) - neighborHeight / 2);
                var candidateBox = new RectangleF(position.X, position.Y, neighborWidth, neighborHeight);

                float score = 0;
                foreach (var placed in _placedRooms.Values)
                {
                    var checkRect = placed.BoundingBox;
                    checkRect.Inflate(16, 16); // Marge de sécurité
                    if (checkRect.IntersectsWith(candidateBox))
                    {
                        var intersection = RectangleF.Intersect(checkRect, candidateBox);
                        score += intersection.Width * intersection.Height;
                    }
                }

                if (score < bestPlacement.Score)
                {
                    bestPlacement = new { Box = candidateBox, Score = score, WallToConnect = wall };
                }
            }

            if (bestPlacement.WallToConnect != null)
            {
                Console.WriteLine($"  Placement de la pièce: {to.Id} (Collision score: {bestPlacement.Score})");
                var newPlacedRoom = CreateRoomGeometry(to, bestPlacement.Box);
                CarveConnection(parentPlacedRoom, newPlacedRoom, connection);
                return true;
            }
            return false;
        }

        private void PlaceNestedRooms()
        {
            foreach (var roomData in _dgraph.Rooms.Where(r => r.ParentRoom != null))
            {
                if (_placedRooms.TryGetValue(roomData.ParentRoom, out var parent))
                {
                    Console.WriteLine($"  Placement de la pièce imbriquée: {roomData.Id} dans {roomData.ParentRoom}");
                    float width = parent.BoundingBox.Width * (_random.Next(15, 30) / 100f);
                    float height = parent.BoundingBox.Height * (_random.Next(15, 30) / 100f);
                    float x = parent.BoundingBox.X + (parent.BoundingBox.Width - width) / 2;
                    float y = parent.BoundingBox.Y + (parent.BoundingBox.Height - height) / 2;
                    CreateRoomGeometry(roomData, new RectangleF(x, y, width, height), parent.DhemapSector.Id);
                }
            }
        }

        private PlacedRoom CreateRoomGeometry(Room roomData, RectangleF rect, int? parentSectorId = null)
        {
            var sector = CreateSectorFromRoom(roomData);
            _dhemap.Sectors.Add(sector);

            var v1 = AddVertex(rect.X, rect.Y);
            var v2 = AddVertex(rect.Right, rect.Y);
            var v3 = AddVertex(rect.Right, rect.Bottom);
            var v4 = AddVertex(rect.Left, rect.Bottom);

            string wallTexture = GetTexture(roomData.Properties.WallTexture);
            var linedefs = new List<Linedef>
            {
                AddLinedef(v1, v2, sector.Id, wallTexture, parentSectorId),
                AddLinedef(v2, v3, sector.Id, wallTexture, parentSectorId),
                AddLinedef(v3, v4, sector.Id, wallTexture, parentSectorId),
                AddLinedef(v4, v1, sector.Id, wallTexture, parentSectorId)
            };

            var placedRoom = new PlacedRoom { DGraphRoom = roomData, DhemapSector = sector, Linedefs = linedefs, BoundingBox = rect };

            var items = roomData.Contents?.Items ?? new List<ContentItem>();
            var monsters = roomData.Contents?.Monsters ?? new List<ContentItem>();
            PlaceThings(items.Concat(monsters).ToList(), rect);

            if (parentSectorId == null) _placedRooms.Add(roomData.Id, placedRoom);
            return placedRoom;
        }

        private void CarveConnection(PlacedRoom from, PlacedRoom to, Connection connection)
        {
            Console.WriteLine($"    -> Création de la connexion '{connection.Type}' entre '{from.DGraphRoom.Id}' et '{to.DGraphRoom.Id}'.");

            // Trouver les murs les plus proches entre les deux pièces
            var (fromWall, toWall) = FindClosestParallelWalls(from, to);
            if (fromWall == null || toWall == null) return;

            // Créer le secteur de la porte
            var doorSector = new Sector { Id = _nextId++, Tag = _nextId, FloorHeight = from.DhemapSector.FloorHeight, CeilingHeight = from.DhemapSector.FloorHeight + 4, FloorTexture = from.DhemapSector.FloorTexture, CeilingTexture = from.DhemapSector.CeilingTexture, LightLevel = 160 };
            _dhemap.Sectors.Add(doorSector);

            // "Casser" les deux murs pour faire une ouverture
            var fromOpening = SplitLinedef(fromWall, 64);
            var toOpening = SplitLinedef(toWall, 64);

            // Connecter les pièces à la porte
            fromOpening.opening.BackSidedef = AddSidedef(doorSector.Id, GetTexture("door_frame"));
            toOpening.opening.BackSidedef = AddSidedef(doorSector.Id, GetTexture("door_frame"));
            fromOpening.opening.Flags.Add("twoSided");
            toOpening.opening.Flags.Add("twoSided");

            // Connecter les murs de la porte
            AddLinedef(_vertexLookup[fromOpening.opening.EndVertex], _vertexLookup[toOpening.opening.StartVertex], doorSector.Id, GetTexture("door_frame"));
            AddLinedef(_vertexLookup[toOpening.opening.EndVertex], _vertexLookup[fromOpening.opening.StartVertex], doorSector.Id, GetTexture("door_frame"));

            // Appliquer l'action de porte
            var doorActionLine = connection.Type == "locked_door" ? toOpening.opening : fromOpening.opening;
            doorActionLine.Action = new ActionInfo { Special = 26, Tag = doorSector.Id }; // Blue Key Door
        }

        private (Linedef opening, Linedef[] newWalls) SplitLinedef(Linedef line, float openingSize)
        {
            // Supprimer l'ancien linedef
            _dhemap.Linedefs.Remove(line);

            var v_start = _vertexLookup[line.StartVertex];
            var v_end = _vertexLookup[line.EndVertex];

            var lineVec = new Vector2(v_end.X - v_start.X, v_end.Y - v_start.Y);
            var lineLength = lineVec.Length();
            lineVec = Vector2.Normalize(lineVec);

            float midPoint = lineLength / 2f;
            var v_open1 = AddVertex(v_start.X + lineVec.X * (midPoint - openingSize / 2), v_start.Y + lineVec.Y * (midPoint - openingSize / 2));
            var v_open2 = AddVertex(v_start.X + lineVec.X * (midPoint + openingSize / 2), v_start.Y + lineVec.Y * (midPoint + openingSize / 2));

            var side = _dhemap.Sidedefs.First(s => s.Id == line.FrontSidedef);

            var wall1 = AddLinedef(v_start, v_open1, side.Sector, side.TextureMiddle);
            var opening = AddLinedef(v_open1, v_open2, side.Sector, GetTexture("door_regular"));
            var wall2 = AddLinedef(v_open2, v_end, side.Sector, side.TextureMiddle);

            return (opening, new[] { wall1, wall2 });
        }

        private (Linedef, Linedef) FindClosestParallelWalls(PlacedRoom r1, PlacedRoom r2)
        {
            // Logique simplifiée pour trouver des murs face à face
            return (r1.Linedefs.FirstOrDefault(l => l.BackSidedef == null), r2.Linedefs.FirstOrDefault(l => l.BackSidedef == null));
        }

        // --- Fonctions Utilitaires ---
        private Vertex AddVertex(float x, float y) { var v = new Vertex { Id = _nextId++, X = x, Y = y }; _dhemap.Vertices.Add(v); _vertexLookup[v.Id] = v; return v; }
        private Linedef AddLinedef(Vertex v1, Vertex v2, int sectorId, string texture, int? backSectorId = null) { var side = AddSidedef(sectorId, texture); var line = new Linedef { Id = _nextId++, StartVertex = v1.Id, EndVertex = v2.Id, FrontSidedef = side }; if (backSectorId.HasValue) { line.BackSidedef = AddSidedef(backSectorId.Value, texture); line.Flags.Add("twoSided"); } else { line.Flags.Add("impassable"); } _dhemap.Linedefs.Add(line); return line; }
        private int AddSidedef(int sectorId, string texture) { var side = new Sidedef { Id = _nextId++, Sector = sectorId, TextureMiddle = texture }; _dhemap.Sidedefs.Add(side); return side.Id; }
        private Sector CreateSectorFromRoom(Room roomData) { return new Sector { Id = _nextId++, Tag = roomData.Properties.Tag ?? 0, FloorTexture = GetTexture(roomData.Properties.FloorFlat), CeilingTexture = (roomData.Properties.Ceiling == "sky" || GetTexture(roomData.Properties.CeilingFlat) == null) ? "F_SKY1" : GetTexture(roomData.Properties.CeilingFlat), LightLevel = GetLight(roomData.Properties.LightLevel), FloorHeight = GetHeight(roomData.Properties.Floor), CeilingHeight = GetHeight(roomData.Properties.Ceiling, 128) }; }
        private void PlaceThings(List<ContentItem> items, RectangleF bounds) { foreach (var item in items) for (int i = 0; i < item.Count; i++) _dhemap.Things.Add(new Thing { Id = _nextId++, X = bounds.X + (float)(_random.NextDouble() * bounds.Width), Y = bounds.Y + (float)(_random.NextDouble() * bounds.Height), Angle = _random.Next(8) * 45, Type = item.TypeId, Flags = new List<string> { "skillEasy", "skillNormal", "skillHard" } }); }
        private string GetTexture(string concept) { if (string.IsNullOrEmpty(concept)) return "STARG1"; if (_dgraph.ThemePalette.TryGetValue(concept, out var textures) && textures.Any()) { int totalWeight = textures.Sum(t => t.Weight); int r = _random.Next(0, totalWeight); foreach (var texture in textures) { if (r < texture.Weight) return texture.Name; r -= texture.Weight; } } return "STARG1"; }
        private int GetHeight(string height, int defaultHeight = 0) => height switch { "very_low" => -32, "low" => 0, "normal" => defaultHeight, "high" => 128, "very_high" => 256, _ => defaultHeight };
        private int GetLight(string light) => light switch { "dark" => 80, "dim" => 120, "normal" => 160, "bright" => 220, "flickering" => 160, _ => 160 };
    }
}