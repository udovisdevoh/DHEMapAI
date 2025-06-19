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
                Music = musicList.Any() ? musicList.ElementAt(_random.Next(musicList.Count)) : ""
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
            var masterAssetPool = assetType == "texture" ? validTextures : validFlats;
            var thematicAssetSource = _themeService.GetAssetsForTheme(game, themeKey);
            var thematicCandidates = new HashSet<string>(assetType == "texture" ? thematicAssetSource.Textures : thematicAssetSource.Flats);

            // S'assurer que les assets thématiques sont valides pour ce jeu
            thematicCandidates.IntersectWith(masterAssetPool);

            // Si le concept a une fonction (ex: "door", "switch_exit")
            if (!string.IsNullOrEmpty(functionKey))
            {
                // On cherche d'abord la fonction spécifique (ex: "door_exit"), puis on se rabat sur la générique (ex: "door") si elle n'existe pas.
                var specificFunctionData = _functionService.GetFunctionData(game, functionKey);
                var specificFunctionalAssets = assetType == "texture" ? specificFunctionData.Textures : specificFunctionData.Flats;

                if (specificFunctionalAssets.Any())
                {
                    // Tentative 1: Thème + Fonction spécifique
                    var idealCandidates = thematicCandidates.Intersect(specificFunctionalAssets).ToList();
                    if (idealCandidates.Any()) return idealCandidates[_random.Next(idealCandidates.Count)];

                    // Tentative 2: Fonction spécifique seule
                    return specificFunctionalAssets[_random.Next(specificFunctionalAssets.Count)];
                }

                // Tentative 3 (Fallback) : Thème + Fonction générique (ex: chercher un "switch" si "switch_exit" échoue)
                string genericFunctionKey = functionKey.Split('_').First();
                if (genericFunctionKey != functionKey)
                {
                    var genericFunctionData = _functionService.GetFunctionData(game, genericFunctionKey);
                    var genericFunctionalAssets = assetType == "texture" ? genericFunctionData.Textures : genericFunctionData.Flats;
                    if (genericFunctionalAssets.Any())
                    {
                        var idealCandidates = thematicCandidates.Intersect(genericFunctionalAssets).ToList();
                        if (idealCandidates.Any()) return idealCandidates[_random.Next(idealCandidates.Count)];

                        return genericFunctionalAssets[_random.Next(genericFunctionalAssets.Count)];
                    }
                }
            }

            // Pour les concepts sans fonction (ex: "wall_primary") ou si tout le reste a échoué
            if (thematicCandidates.Any())
            {
                return thematicCandidates.ElementAt(_random.Next(thematicCandidates.Count));
            }

            // Dernier recours absolu
            if (masterAssetPool.Any())
            {
                return masterAssetPool.ElementAt(_random.Next(masterAssetPool.Count));
            }

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
                var allMonsterIds = new HashSet<int>(_functionService.GetFunctionData(game, "monster").Things);
                var candidateIds = thematicThingIds.Intersect(allMonsterIds).ToList();

                if (!candidateIds.Any()) { candidateIds = allMonsterIds.ToList(); }

                var thematicMonsterCandidates = allThings.Where(t => candidateIds.Contains(t.TypeId)).ToList();
                var shuffledThematicMonsters = thematicMonsterCandidates.OrderBy(m => _random.Next()).ToList();
                finalMonsterSelection.AddRange(shuffledThematicMonsters.Take(desiredMonsterCount));
            }

            if (finalMonsterSelection.Count < desiredMonsterCount)
            {
                int needed = desiredMonsterCount - finalMonsterSelection.Count;
                var allMonsters = _assetService.GetThingsForGame(game)
                    .Where(t => _functionService.GetFunctionData(game, "monster").Things.Contains(t.TypeId)).ToList();
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

        private string GetTokenType(string game, string assetName)
        {
            string function = _functionService.GetAssetFunction(game, assetName);

            if (function != null)
            {
                if (function.StartsWith("switch")) return "connection_action";
                if (function.StartsWith("damaging_floor") || function == "light_source_flat" || function == "animated") return "flat";
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