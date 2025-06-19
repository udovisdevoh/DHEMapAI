using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGenesisRandomGeneratorService
    {
        private readonly AssetThemeService _themeService;
        private readonly GameAssetService _assetService;
        private readonly AssetFunctionService _functionService;
        private readonly Random _random = new Random();

        public DGenesisRandomGeneratorService(AssetThemeService themeService, GameAssetService assetService, AssetFunctionService functionService)
        {
            _themeService = themeService;
            _assetService = assetService;
            _functionService = functionService;
        }

        public DGenesisFile Generate(string game)
        {
            var file = new DGenesisFile
            {
                MapInfo = GenerateMapInfo(game),
                GenerationParams = GenerateGenerationParams(),
                SectorBehaviorPalette = GenerateSectorBehaviorPalette(game),
                FeaturePalette = GenerateFeaturePalette()
            };

            var (themePalette, primaryThemeKey) = GenerateThemePalette(game);
            file.ThemePalette = themePalette;

            file.ThematicTokens = GenerateThematicTokens(game, file.ThemePalette, primaryThemeKey);

            return file;
        }

        private MapInfo GenerateMapInfo(string game)
        {
            var musicList = _assetService.GetMusicForGame(game);
            return new MapInfo
            {
                Game = game,
                MapLumpName = GetDefaultMapDetailsForGame(game).MapLumpName,
                Name = "Generated Level " + _random.Next(100, 999),
                Music = musicList.Any() ? musicList[_random.Next(musicList.Count)] : ""
            };
        }

        private GenerationParams GenerateGenerationParams()
        {
            return new GenerationParams
            {
                RoomCount = _random.Next(8, 25),
                AvgConnectivity = Math.Round(_random.NextDouble() * 2.0 + 1.8, 1),
                AvgFloorHeightDelta = _random.Next(5) * 16,
                AvgHeadroom = _random.Next(96, 256),
                TotalVerticalSpan = _random.Next(384, 1280),
                VerticalTransitionProfile = new List<VerticalTransition>
                {
                    new VerticalTransition { Type = "level", Weight = _random.Next(20, 60) },
                    new VerticalTransition { Type = "step", Weight = _random.Next(20, 50) },
                    new VerticalTransition { Type = "overlook", Weight = _random.Next(5, 25) },
                    new VerticalTransition { Type = "lift", Weight = _random.Next(1, 15) }
                }
            };
        }

        private Dictionary<string, SectorBehavior> GenerateSectorBehaviorPalette(string game)
        {
            var palette = new Dictionary<string, SectorBehavior>();
            var allEffects = GameAssetService.GetAllSectorEffects();

            foreach (var effect in allEffects)
            {
                string key = effect.Value.ToLower().Replace(" ", "_").Replace("-", "").Replace(".", "");
                palette[key] = new SectorBehavior
                {
                    Description = effect.Value,
                    SectorSpecial = effect.Key,
                    Weight = effect.Key == 0 ? 100 : _random.Next(1, 20)
                };
            }
            return palette;
        }

        private Dictionary<string, Feature> GenerateFeaturePalette()
        {
            return new Dictionary<string, Feature>
            {
                { "door", new Feature { Description = "A standard door.", Weight = _random.Next(50, 100) }},
                { "key_door", new Feature { Description = "A locked door requiring a key.", Weight = _random.Next(20, 80) }},
                { "switch_door", new Feature { Description = "A door opened by a remote switch.", Weight = _random.Next(10, 60) }},
                { "secret_door", new Feature { Description = "A secret door.", Weight = _random.Next(5, 30) }},
                { "secret_switch", new Feature { Description = "A hidden switch.", Weight = _random.Next(5, 25) }},
                { "secret_exit", new Feature { Description = "A secret exit.", Weight = _random.Next(1, 10) }},
                { "crushing_ceiling", new Feature { Description = "A crushing ceiling trap.", Weight = _random.Next(0, 15) }},
                { "elevator", new Feature { Description = "An elevator.", Weight = _random.Next(10, 40) }},
                { "teleporter", new Feature { Description = "A teleporter.", Weight = _random.Next(5, 25) }}
            };
        }

        private (Dictionary<string, List<WeightedAsset>> palette, string themeKey) GenerateThemePalette(string game)
        {
            var palette = new Dictionary<string, List<WeightedAsset>>();

            var validTextures = new HashSet<string>(_assetService.GetTexturesForGame(game));
            var validFlats = new HashSet<string>(_assetService.GetFlatsForGame(game));

            var availableThemes = _themeService.GetAvailableThemeKeys(game);
            if (!availableThemes.Any()) return (palette, null);
            string primaryThemeKey = availableThemes[_random.Next(availableThemes.Count)];

            var concepts = new Dictionary<string, (string type, string function)>
            {
                { "wall_primary", ("texture", null) }, { "wall_accent", ("texture", "panel") },
                { "wall_support", ("texture", "support") }, { "wall_secret_indicator", ("texture", "secret_wall") },
                { "wall_panel", ("texture", "panel") }, { "door_frame", ("texture", "border") },
                { "floor_primary", ("flat", null) }, { "floor_accent", ("flat", null) },
                { "ceiling_primary", ("flat", null) }, { "ceiling_light_source", ("flat", "light_source_flat") },
                { "platform_surface", ("flat", null) }, { "door_regular", ("texture", "door") },
                { "door_locked", ("texture", "door") }, { "door_exit", ("texture", "door_exit") },
                { "switch_utility", ("texture", "switch") }, { "switch_exit", ("texture", "switch_exit") },
                { "door_indicator_blue", ("texture", "door_blue") }, { "door_indicator_red", ("texture", "door_red") },
                { "door_indicator_yellow", ("texture", "door_yellow") }
            };

            foreach (var concept in concepts)
            {
                string assetName = FindAssetForConcept(game, concept.Value.type, primaryThemeKey, concept.Value.function, validTextures, validFlats);
                if (!string.IsNullOrEmpty(assetName))
                {
                    palette[concept.Key] = new List<WeightedAsset> { new WeightedAsset { Name = assetName, Weight = 100 } };
                }
            }

            return (palette, primaryThemeKey);
        }

        private string FindAssetForConcept(string game, string assetType, string themeKey, string functionKey, HashSet<string> validTextures, HashSet<string> validFlats)
        {
            var assetPool = assetType == "texture" ? validTextures : validFlats;

            // Tentative 1: Intersection Thème + Fonction
            if (!string.IsNullOrEmpty(functionKey))
            {
                var thematicAssets = new HashSet<string>(assetType == "texture"
                    ? _themeService.GetAssetsForTheme(game, themeKey).Textures
                    : _themeService.GetAssetsForTheme(game, themeKey).Flats);

                var functionalAssets = new HashSet<string>(_functionService.GetAssetsForFunction(game, functionKey));

                var candidates = thematicAssets.Intersect(functionalAssets).ToList();
                if (candidates.Any()) return candidates[_random.Next(candidates.Count)];

                // Tentative 2: Repli sur la Fonction seule
                var functionalCandidates = functionalAssets.Intersect(assetPool).ToList();
                if (functionalCandidates.Any()) return functionalCandidates[_random.Next(functionalCandidates.Count)];
            }

            // Tentative 3: Repli sur le Thème seul (pour les concepts sans fonction comme wall_primary)
            var thematicOnlyCandidates = (assetType == "texture" ? _themeService.GetAssetsForTheme(game, themeKey).Textures : _themeService.GetAssetsForTheme(game, themeKey).Flats)
                .Intersect(assetPool).ToList();
            if (thematicOnlyCandidates.Any()) return thematicOnlyCandidates[_random.Next(thematicOnlyCandidates.Count)];

            // Tentative 4: Dernier recours, n'importe quel asset valide du bon type
            if (assetPool.Any()) return assetPool.ElementAt(_random.Next(assetPool.Count));

            return null;
        }

        private List<ThematicToken> GenerateThematicTokens(string game, Dictionary<string, List<WeightedAsset>> themePalette, string primaryThemeKey)
        {
            var tokens = new List<ThematicToken>();
            var existingAssets = new HashSet<string>();

            foreach (var concept in themePalette)
            {
                foreach (var asset in concept.Value)
                {
                    if (existingAssets.Add(asset.Name))
                    {
                        tokens.Add(new ThematicToken
                        {
                            Name = asset.Name,
                            Type = GetTokenType(game, asset.Name),
                            AdjacencyRules = new List<AdjacencyRule>()
                        });
                    }
                }
            }

            int desiredMonsterCount = _random.Next(4, 9);
            var finalMonsterSelection = new List<GameAssetThing>();

            if (!string.IsNullOrEmpty(primaryThemeKey))
            {
                var allThings = _assetService.GetThingsForGame(game);
                var thematicThingIds = new HashSet<int>(_themeService.GetAssetsForTheme(game, primaryThemeKey).Things);
                var allMonsterIds = new HashSet<int>(_functionService.GetAssetsForFunction(game, "monster").Select(name => allThings.FirstOrDefault(t => t.Name == name)?.TypeId ?? -1));

                var candidateIds = thematicThingIds.Intersect(allMonsterIds).ToList();
                var thematicMonsterCandidates = allThings.Where(t => candidateIds.Contains(t.TypeId)).ToList();

                var shuffledThematicMonsters = thematicMonsterCandidates.OrderBy(m => _random.Next()).ToList();
                finalMonsterSelection.AddRange(shuffledThematicMonsters.Take(desiredMonsterCount));
            }

            if (finalMonsterSelection.Count < desiredMonsterCount)
            {
                int needed = desiredMonsterCount - finalMonsterSelection.Count;
                var allMonsters = _assetService.GetThingsForGame(game)
                    .Where(t => _functionService.GetAssetsForFunction(game, "monster").Contains(t.Name)).ToList();

                var alreadySelectedIds = new HashSet<int>(finalMonsterSelection.Select(m => m.TypeId));
                var fallbackCandidates = allMonsters.Where(m => !alreadySelectedIds.Contains(m.TypeId)).ToList();
                var shuffledFallbackMonsters = fallbackCandidates.OrderBy(m => _random.Next()).ToList();
                finalMonsterSelection.AddRange(shuffledFallbackMonsters.Take(needed));
            }

            foreach (var thing in finalMonsterSelection)
            {
                if (existingAssets.Add(thing.Name))
                {
                    tokens.Add(new ThematicToken
                    {
                        Name = thing.Name,
                        Type = "object",
                        TypeId = thing.TypeId,
                        AdjacencyRules = new List<AdjacencyRule>()
                    });
                }
            }
            return tokens;
        }

        private void FilterAssetCollection(ThemedAssetCollection collection, HashSet<string> validTextures, HashSet<string> validFlats, HashSet<int> validThings, HashSet<string> validMusic)
        {
            if (collection == null) return;
            collection.Textures = collection.Textures?.Where(t => validTextures.Contains(t)).ToList() ?? new List<string>();
            collection.Flats = collection.Flats?.Where(f => validFlats.Contains(f)).ToList() ?? new List<string>();
            collection.Things = collection.Things?.Where(t => validThings.Contains(t)).ToList() ?? new List<int>();
            collection.Music = collection.Music?.Where(m => validMusic.Contains(m)).ToList() ?? new List<string>();
        }

        private string GetTokenType(string game, string assetName)
        {
            string function = _functionService.GetAssetFunction(game, assetName);

            if (function != null && function.StartsWith("switch"))
            {
                return "connection_action";
            }

            if (_assetService.GetTexturesForGame(game).Contains(assetName))
            {
                return "wall";
            }

            return "flat";
        }

        private (string MapLumpName, string Music) GetDefaultMapDetailsForGame(string game)
        {
            return game.ToLower() switch
            {
                "doom" => ("E1M1", "D_E1M1"),
                "doom2" => ("MAP01", "D_RUNNIN"),
                "heretic" => ("E1M1", "MUS_E1M1"),
                "hexen" => ("MAP01", "MUS_WINTRO"),
                _ => ("MAP01", "D_RUNNIN"),
            };
        }
    }
}