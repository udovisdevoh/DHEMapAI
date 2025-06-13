// Generation/MapGenerator.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        private int _nextVertexId = 0;
        private int _nextLinedefId = 0;
        private int _nextSidedefId = 0;
        private int _nextSectorId = 0;
        private int _nextThingId = 0;

        // Classe interne pour suivre l'état d'une pièce générée
        private class PlacedRoom
        {
            public Room DGraphRoom { get; set; }
            public Sector DhemapSector { get; set; }
            public List<Vertex> Vertices { get; set; }
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

            // Trouver la pièce de départ (celle avec le Player 1 Start)
            var startRoomData = _dgraph.Rooms.FirstOrDefault(r => r.Contents?.Items?.Any(i => i.Name == "Player1Start") ?? false)
                              ?? _dgraph.Rooms.First();

            // Placer la première pièce à l'origine
            var initialPosition = new PointF(0, 0);
            PlaceRoom(startRoomData, initialPosition);

            // Utiliser une file pour placer les pièces connectées
            var queue = new Queue<Room>();
            queue.Enqueue(startRoomData);

            while (queue.Count > 0)
            {
                var currentRoomData = queue.Dequeue();
                var connections = _dgraph.Connections.Where(c => c.FromRoom == currentRoomData.Id || c.ToRoom == currentRoomData.Id);

                foreach (var connection in connections)
                {
                    string neighborId = connection.FromRoom == currentRoomData.Id ? connection.ToRoom : connection.FromRoom;
                    if (_placedRooms.ContainsKey(neighborId)) continue; // Déjà placé

                    var neighborRoomData = _dgraph.Rooms.First(r => r.Id == neighborId);

                    // Tenter de placer la nouvelle pièce à côté de la pièce actuelle
                    if (TryPlaceNeighbor(currentRoomData, neighborRoomData, out PointF newPosition))
                    {
                        PlaceRoom(neighborRoomData, newPosition);
                        queue.Enqueue(neighborRoomData);
                        // Après avoir placé les deux pièces, on crée la connexion
                        CreateConnection(currentRoomData.Id, neighborRoomData.Id, connection);
                    }
                    else
                    {
                        Console.WriteLine($"Avertissement : Impossible de placer la pièce '{neighborId}' connectée à '{currentRoomData.Id}' sans collision.");
                    }
                }
            }

            return _dhemap;
        }

        private void InitializeDhemap()
        {
            _dhemap.MapInfo = new Models.Dhemap.MapInfo
            {
                Game = _dgraph.MapInfo.Game,
                Map = _dgraph.MapInfo.Map,
                Name = _dgraph.MapInfo.Name,
                Music = _dgraph.MapInfo.Music,
                SkyTexture = "SKY1" // Placeholder, pourrait venir du DGraph
            };
            _dhemap.Vertices = new List<Vertex>();
            _dhemap.Linedefs = new List<Linedef>();
            _dhemap.Sidedefs = new List<Sidedef>();
            _dhemap.Sectors = new List<Sector>();
            _dhemap.Things = new List<Thing>();
        }

        // Place une pièce et crée sa géométrie de base
        private void PlaceRoom(Room roomData, PointF position)
        {
            Console.WriteLine($"  Placement de la pièce: {roomData.Id}");
            float width = (float)(_random.Next(256, 513));
            float height = (float)(_random.Next(256, 513));

            var placedRoom = new PlacedRoom
            {
                DGraphRoom = roomData,
                Vertices = new List<Vertex>(),
                BoundingBox = new RectangleF(position.X, position.Y, width, height)
            };

            var v1 = AddVertex(position.X, position.Y);
            var v2 = AddVertex(position.X + width, position.Y);
            var v3 = AddVertex(position.X + width, position.Y + height);
            var v4 = AddVertex(position.X, position.Y + height);
            placedRoom.Vertices.AddRange(new[] { v1, v2, v3, v4 });

            var sector = CreateSectorFromRoom(roomData);
            _dhemap.Sectors.Add(sector);
            placedRoom.DhemapSector = sector;

            // Création des murs
            string wallTexture = GetTexture(roomData.Properties.WallTexture);
            AddLinedef(v1, v2, sector.Id, wallTexture);
            AddLinedef(v2, v3, sector.Id, wallTexture);
            AddLinedef(v3, v4, sector.Id, wallTexture);
            AddLinedef(v4, v1, sector.Id, wallTexture);

            // Placer les objets
            var items = roomData.Contents?.Items ?? new List<ContentItem>();
            var monsters = roomData.Contents?.Monsters ?? new List<ContentItem>();
            PlaceThings(items.Concat(monsters).ToList(), placedRoom.BoundingBox);

            _placedRooms.Add(roomData.Id, placedRoom);
        }

        // Tente de trouver une position pour une nouvelle pièce sans collision
        private bool TryPlaceNeighbor(Room fromRoom, Room toRoom, out PointF position)
        {
            var fromPlaced = _placedRooms[fromRoom.Id];
            float toWidth = (float)(_random.Next(256, 513));
            float toHeight = (float)(_random.Next(256, 513));
            int spacing = 128; // Espace pour la porte/connexion

            var directions = new List<int> { 0, 1, 2, 3 }.OrderBy(x => _random.Next()).ToList(); // Ordre aléatoire
            foreach (var dir in directions)
            {
                RectangleF candidateBox = new RectangleF();
                if (dir == 0) // Est
                    candidateBox = new RectangleF(fromPlaced.BoundingBox.Right + spacing, fromPlaced.BoundingBox.Y, toWidth, toHeight);
                else if (dir == 1) // Ouest
                    candidateBox = new RectangleF(fromPlaced.BoundingBox.Left - toWidth - spacing, fromPlaced.BoundingBox.Y, toWidth, toHeight);
                else if (dir == 2) // Nord
                    candidateBox = new RectangleF(fromPlaced.BoundingBox.X, fromPlaced.BoundingBox.Top + spacing, toWidth, toHeight);
                else // Sud
                    candidateBox = new RectangleF(fromPlaced.BoundingBox.X, fromPlaced.BoundingBox.Bottom - toHeight - spacing, toWidth, toHeight);

                // Vérification de collision
                if (!_placedRooms.Values.Any(p => p.BoundingBox.IntersectsWith(candidateBox)))
                {
                    position = new PointF(candidateBox.X, candidateBox.Y);
                    return true;
                }
            }
            position = PointF.Empty;
            return false;
        }

        // Logique simplifiée pour créer une ouverture.
        private void CreateConnection(string fromRoomId, string toRoomId, Connection connection)
        {
            // Note: Une implémentation complète serait beaucoup plus complexe,
            // elle identifierait les murs adjacents, les supprimerait,
            // et créerait de nouveaux sommets et murs pour le passage.
            // Pour cet exemple, nous signalons simplement l'intention.
            Console.WriteLine($"    -> Création de la connexion '{connection.Type}' entre '{fromRoomId}' et '{toRoomId}' (logique à implémenter).");
        }


        // --- Fonctions utilitaires ---
        private Sector CreateSectorFromRoom(Room room) { /* ... */ return new Sector { Id = _nextSectorId++, Tag = room.Properties.Tag ?? 0, FloorTexture = GetTexture(room.Properties.FloorFlat), CeilingTexture = GetTexture(room.Properties.CeilingFlat), LightLevel = GetLight(room.Properties.LightLevel), FloorHeight = GetHeight(room.Properties.Floor), CeilingHeight = GetHeight(room.Properties.Ceiling, 128) }; }
        private Vertex AddVertex(float x, float y) { var v = new Vertex { Id = _nextVertexId++, X = x, Y = y }; _dhemap.Vertices.Add(v); return v; }
        private void AddLinedef(Vertex v1, Vertex v2, int sectorId, string texture) { var side = new Sidedef { Id = _nextSidedefId++, Sector = sectorId, TextureMiddle = texture }; _dhemap.Sidedefs.Add(side); _dhemap.Linedefs.Add(new Linedef { Id = _nextLinedefId++, StartVertex = v1.Id, EndVertex = v2.Id, FrontSidedef = side.Id, Flags = new List<string> { "impassable" } }); }
        private void PlaceThings(List<ContentItem> items, RectangleF bounds) { foreach (var item in items) for (int i = 0; i < item.Count; i++) _dhemap.Things.Add(new Thing { Id = _nextThingId++, X = bounds.X + (float)(_random.NextDouble() * bounds.Width), Y = bounds.Y + (float)(_random.NextDouble() * bounds.Height), Angle = _random.Next(8) * 45, Type = item.TypeId, Flags = new List<string> { "skillEasy", "skillNormal", "skillHard" } }); }
        private string GetTexture(string concept) { if (string.IsNullOrEmpty(concept)) return "STARTAN2"; if (_dgraph.ThemePalette.TryGetValue(concept, out var textures) && textures.Any()) { int totalWeight = textures.Sum(t => t.Weight); int r = _random.Next(0, totalWeight); foreach (var texture in textures) { if (r < texture.Weight) return texture.Name; r -= texture.Weight; } } return "STARTAN2"; }
        private int GetHeight(string height, int defaultHeight = 0) => height switch { "very_low" => -32, "low" => 0, "normal" => defaultHeight, "high" => 128, "very_high" => 256, _ => defaultHeight };
        private int GetLight(string light) => light switch { "dark" => 80, "dim" => 120, "normal" => 160, "bright" => 220, "flickering" => 160, _ => 160 };
    }
}