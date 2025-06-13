using System;
using System.Collections.Generic;
using System.Linq;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Models.Dhemap;

namespace DGraphBuilder.Generation
{
    // NOTE : Ceci est une implémentation simplifiée. Un vrai générateur
    // aurait des algorithmes de placement et de géométrie beaucoup plus complexes.
    public class MapGenerator
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;
        private readonly Dictionary<string, string> _paletteCache = new Dictionary<string, string>();

        private int _nextVertexId = 0;
        private int _nextLinedefId = 0;
        private int _nextSidedefId = 0;
        private int _nextSectorId = 0;
        private int _nextThingId = 0;

        public MapGenerator(DGraphFile dgraph, int? seed)
        {
            _dgraph = dgraph;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public DhemapFile Generate()
        {
            var dhemap = new DhemapFile();

            // Traduire les informations de base
            dhemap.MapInfo = new Models.Dhemap.MapInfo
            {
                Game = _dgraph.MapInfo.Game,
                Map = _dgraph.MapInfo.Map,
                Name = _dgraph.MapInfo.Name,
                Music = _dgraph.MapInfo.Music,
                SkyTexture = "SKY1" // Placeholder
            };

            dhemap.Vertices = new List<Vertex>();
            dhemap.Linedefs = new List<Linedef>();
            dhemap.Sidedefs = new List<Sidedef>();
            dhemap.Sectors = new List<Sector>();
            dhemap.Things = new List<Thing>();

            // Logique de placement simplifiée
            float currentX = 0;
            float currentY = 0;

            foreach (var room in _dgraph.Rooms.Where(r => r.ParentRoom == null))
            {
                if (room.ParentRoom != null) continue; // Gérer les pièces imbriquées plus tard

                // Générer une pièce rectangulaire simple
                float roomWidth = (float)(_random.Next(256, 512));
                float roomHeight = (float)(_random.Next(256, 512));

                int v1 = AddVertex(currentX, currentY);
                int v2 = AddVertex(currentX + roomWidth, currentY);
                int v3 = AddVertex(currentX + roomWidth, currentY + roomHeight);
                int v4 = AddVertex(currentX, currentY + roomHeight);

                var sector = CreateSectorFromRoom(room);
                dhemap.Sectors.Add(sector);

                // Créer les murs
                AddLinedef(dhemap, v1, v2, sector.Id, GetTexture("wall_primary"));
                AddLinedef(dhemap, v2, v3, sector.Id, GetTexture("wall_primary"));
                AddLinedef(dhemap, v3, v4, sector.Id, GetTexture("wall_primary"));
                AddLinedef(dhemap, v4, v1, sector.Id, GetTexture("wall_primary"));

                // Placer les objets
                if (room.Contents?.Items != null)
                    PlaceThings(dhemap, room.Contents.Items, currentX, currentY, roomWidth, roomHeight);
                if (room.Contents?.Monsters != null)
                    PlaceThings(dhemap, room.Contents.Monsters, currentX, currentY, roomWidth, roomHeight);

                // Déplacer la position pour la prochaine pièce
                currentX += roomWidth + 128; // Ajouter un espace pour la connexion
            }

            return dhemap;
        }

        private Sector CreateSectorFromRoom(Room room)
        {
            return new Sector
            {
                Id = _nextSectorId++,
                FloorHeight = GetHeight(room.Properties.Floor),
                CeilingHeight = GetHeight(room.Properties.Ceiling, 128),
                LightLevel = GetLight(room.Properties.LightLevel),
                FloorTexture = GetTexture(room.Properties.FloorFlat),
                CeilingTexture = room.Properties.Ceiling == "sky" ? "F_SKY1" : GetTexture(room.Properties.CeilingFlat),
                Tag = room.Properties.Tag ?? 0
            };
        }

        private int AddVertex(float x, float y)
        {
            var v = new Vertex { Id = _nextVertexId++, X = x, Y = y };
            // Note: le DHEMap final n'a pas besoin de la liste de vertex,
            // mais nous en aurons besoin pour la génération.
            // On la retourne dans le DHEMap final pour la complétude.
            return v.Id;
        }

        private void AddLinedef(DhemapFile dhemap, int v1, int v2, int sectorId, string texture)
        {
            var sidedef = new Sidedef { Id = _nextSidedefId++, Sector = sectorId, TextureMiddle = texture };
            dhemap.Sidedefs.Add(sidedef);

            var linedef = new Linedef
            {
                Id = _nextLinedefId++,
                StartVertex = v1,
                EndVertex = v2,
                FrontSidedef = sidedef.Id,
                Flags = new List<string> { "impassable" }
            };
            dhemap.Linedefs.Add(linedef);
        }

        private void PlaceThings(DhemapFile dhemap, List<ContentItem> items, float x, float y, float w, float h)
        {
            foreach (var item in items)
            {
                for (int i = 0; i < item.Count; i++)
                {
                    dhemap.Things.Add(new Thing
                    {
                        Id = _nextThingId++,
                        X = x + (float)(_random.NextDouble() * w),
                        Y = y + (float)(_random.NextDouble() * h),
                        Angle = _random.Next(8) * 45,
                        Type = item.TypeId,
                        Flags = new List<string> { "skillEasy", "skillNormal", "skillHard" }
                    });
                }
            }
        }

        private string GetTexture(string concept)
        {
            if (string.IsNullOrEmpty(concept)) return "NUKAGE1"; // Fallback
            if (_paletteCache.ContainsKey(concept)) return _paletteCache[concept];

            if (_dgraph.ThemePalette.TryGetValue(concept, out var textures) && textures.Any())
            {
                int totalWeight = textures.Sum(t => t.Weight);
                int randomPick = _random.Next(0, totalWeight);
                foreach (var texture in textures)
                {
                    if (randomPick < texture.Weight)
                    {
                        _paletteCache[concept] = texture.Name;
                        return texture.Name;
                    }
                    randomPick -= texture.Weight;
                }
            }
            return "NUKAGE1"; // Fallback
        }

        private int GetHeight(string height, int defaultHeight = 0) => height switch
        {
            "very_low" => -32,
            "low" => 0,
            "normal" => defaultHeight,
            "high" => 128,
            "very_high" => 256,
            _ => defaultHeight
        };
        private int GetLight(string light) => light switch
        {
            "dark" => 80,
            "dim" => 120,
            "normal" => 160,
            "bright" => 220,
            _ => 160
        };
    }
}