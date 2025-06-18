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
        private readonly AssetFunctionService _functionService; // AJOUT : Injection du nouveau service
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

            var (themePalette, primaryTheme) = GenerateThemePalette(game);
            file.ThemePalette = themePalette;

            file.ThematicTokens = GenerateThematicTokens(game, file.ThemePalette, primaryTheme);

            return file;
        }

        private MapInfo GenerateMapInfo(string game)
        {
            // La liste des musiques est maintenant filtrée pour s'assurer de sa validité
            var validMusic = new HashSet<string>(_assetService.GetMusicForGame(game));
            var thematicMusic = _themeService.GetAssetsForTheme(game, "music")?.Music?
                .Where(m => validMusic.Contains(m)).ToList() ?? new List<string>();

            string chosenMusic = "";
            if (thematicMusic.Any())
            {
                chosenMusic = thematicMusic[_random.Next(thematicMusic.Count)];
            }
            else if (validMusic.Any())
            {
                chosenMusic = validMusic.ElementAt(_random.Next(validMusic.Count));
            }

            return new MapInfo
            {
                Game = game,
                MapLumpName = GetDefaultMapDetailsForGame(game).MapLumpName,
                Name = "Generated Level " + _random.Next(100, 999),
                Music = chosenMusic
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

        private (Dictionary<string, List<WeightedAsset>> palette, string themeTag) GenerateThemePalette(string game)
        {
            var palette = new Dictionary<string, List<WeightedAsset>>();

            var validTextureSet = new HashSet<string>(_assetService.GetTexturesForGame(game));
            var validFlatSet = new HashSet<string>(_assetService.GetFlatsForGame(game));
            var validThingIdSet = new HashSet<int>(_assetService.GetThingsForGame(game).Select(t => t.TypeId));
            var validMusicSet = new HashSet<string>(_assetService.GetMusicForGame(game));

            var availableThemes = _themeService.GetAvailableThemes(game);
            if (!availableThemes.Any()) return (palette, null);
            string primaryTheme = availableThemes[_random.Next(availableThemes.Count)];

            var themeAssets = _themeService.GetAssetsForTheme(game, primaryTheme);
            FilterAssetCollection(themeAssets, validTextureSet, validFlatSet, validThingIdSet, validMusicSet);

            var functionalTags = new Dictionary<string, ThemedAssetCollection>();
            var functionalTagNames = new[] {
                "door", "switch", "light_source", "border", "support", "panel", "secret", "exit",
                "key_indicator_blue", "key_indicator_red", "key_indicator_yellow", "key_indicator_green"
            };

            foreach (var tagName in functionalTagNames)
            {
                var collection = _themeService.GetAssetsForTheme(game, tagName);
                FilterAssetCollection(collection, validTextureSet, validFlatSet, validThingIdSet, validMusicSet);
                functionalTags[tagName] = collection;
            }

            var concepts = new Dictionary<string, (string type, string[] tags)>
            {
                { "wall_primary", ("texture", new []{ primaryTheme }) }, { "wall_accent", ("texture", new []{ primaryTheme, "panel" }) },
                { "wall_support", ("texture", new []{ primaryTheme, "support" }) }, { "wall_secret_indicator", ("texture", new []{ primaryTheme, "secret" }) },
                { "wall_panel", ("texture", new []{ primaryTheme, "panel" }) }, { "door_frame", ("texture", new []{ primaryTheme, "border" }) },
                { "floor_primary", ("flat", new []{ primaryTheme }) }, { "floor_accent", ("flat", new []{ primaryTheme }) },
                { "ceiling_primary", ("flat", new []{ primaryTheme }) }, { "ceiling_light_source", ("flat", new []{ "light_source" }) },
                { "platform_surface", ("flat", new []{ primaryTheme }) }, { "door_regular", ("texture", new []{ "door" }) },
                { "door_locked", ("texture", new []{ "door" }) }, { "door_exit", ("texture", new []{ "exit", "door" }) },
                { "switch_utility", ("texture", new []{ "switch" }) }, { "switch_exit", ("texture", new []{ "exit", "switch" }) },
                { "door_indicator_blue", ("texture", new []{ "key_indicator_blue" }) }, { "door_indicator_red", ("texture", new []{ "key_indicator_red" }) },
                { "door_indicator_yellow", ("texture", new []{ "key_indicator_yellow" }) }
            };

            foreach (var concept in concepts)
            {
                var conceptTags = concept.Value.tags;
                var assetType = concept.Value.type;
                List<string> candidates = new List<string>();

                var baseList = assetType == "texture" ? themeAssets.Textures : themeAssets.Flats;
                candidates = new List<string>(baseList);
                foreach (var tag in conceptTags.Where(t => t != primaryTheme && functionalTags.ContainsKey(t)))
                {
                    var functionalAssets = assetType == "texture" ? functionalTags[tag].Textures : functionalTags[tag].Flats;
                    candidates = candidates.Intersect(functionalAssets).ToList();
                }

                if (!candidates.Any())
                {
                    var functionalConceptTags = conceptTags.Where(t => functionalTags.ContainsKey(t)).ToList();
                    if (functionalConceptTags.Any())
                    {
                        var firstFunctionalTag = functionalTags[functionalConceptTags.First()];
                        candidates = assetType == "texture" ? new List<string>(firstFunctionalTag.Textures) : new List<string>(firstFunctionalTag.Flats);

                        foreach (var tag in functionalConceptTags.Skip(1))
                        {
                            var functionalAssets = assetType == "texture" ? functionalTags[tag].Textures : functionalTags[tag].Flats;
                            candidates = candidates.Intersect(functionalAssets).ToList();
                        }
                    }
                }

                if (!candidates.Any() && conceptTags.Contains(primaryTheme) && conceptTags.Length > 1)
                {
                    candidates = assetType == "texture" ? new List<string>(themeAssets.Textures) : new List<string>(themeAssets.Flats);
                }

                if (!candidates.Any())
                {
                    candidates = assetType == "texture" ? new List<string>(validTextureSet) : new List<string>(validFlatSet);
                }

                if (candidates.Any())
                {
                    palette[concept.Key] = new List<WeightedAsset>
                    {
                        new WeightedAsset { Name = candidates[_random.Next(candidates.Count)], Weight = 100 }
                    };
                }
            }
            return (palette, primaryTheme);
        }

        private List<ThematicToken> GenerateThematicTokens(string game, Dictionary<string, List<WeightedAsset>> themePalette, string primaryTheme)
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

            if (!string.IsNullOrEmpty(primaryTheme))
            {
                var allThings = _assetService.GetThingsForGame(game);
                var thematicThingIds = new HashSet<int>(_themeService.GetAssetsForTheme(game, primaryTheme).Things);
                var allMonsterIds = new HashSet<int>(_themeService.GetAssetsForTheme(game, "monster").Things);
                var candidateIds = thematicThingIds.Intersect(allMonsterIds).ToList();
                var thematicMonsterCandidates = allThings.Where(t => candidateIds.Contains(t.TypeId)).ToList();

                var shuffledThematicMonsters = thematicMonsterCandidates.OrderBy(m => _random.Next()).ToList();
                finalMonsterSelection.AddRange(shuffledThematicMonsters.Take(desiredMonsterCount));
            }

            if (finalMonsterSelection.Count < desiredMonsterCount)
            {
                int needed = desiredMonsterCount - finalMonsterSelection.Count;
                var allMonsters = _assetService.GetThingsForGame(game)
                    .Where(t => _themeService.GetAssetsForTheme(game, "monster").Things.Contains(t.TypeId)).ToList();
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

            if (function != null)
            {
                // Un asset défini comme un interrupteur est toujours une "connection_action"
                if (function.StartsWith("switch"))
                {
                    return "connection_action";
                }
                // Un asset défini comme une porte ou une lumière murale est de type "wall"
                if (function.StartsWith("door") || function.StartsWith("light_source_wall") || function.StartsWith("secret_wall"))
                {
                    return "wall";
                }
            }

            // Fallback : si aucune fonction spécifique n'est trouvée, on se base sur la liste principale de l'asset.
            // C'est utile pour les textures génériques qui n'ont pas de fonction listée.
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