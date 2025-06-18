using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DGenesisRandomGeneratorService
    {
        private readonly AssetTagService _tagService;
        private readonly GameAssetService _assetService;
        private readonly Random _random = new Random();

        public DGenesisRandomGeneratorService(AssetTagService tagService, GameAssetService assetService)
        {
            _tagService = tagService;
            _assetService = assetService;
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

            var (themePalette, primaryThemeTag) = GenerateThemePalette(game);
            file.ThemePalette = themePalette;

            file.ThematicTokens = GenerateThematicTokens(game, file.ThemePalette, primaryThemeTag);

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

        private (Dictionary<string, List<WeightedAsset>> palette, string themeTag) GenerateThemePalette(string game)
        {
            var palette = new Dictionary<string, List<WeightedAsset>>();

            var validTextureSet = new HashSet<string>(_assetService.GetTexturesForGame(game));
            var validFlatSet = new HashSet<string>(_assetService.GetFlatsForGame(game));
            var validThingIdSet = new HashSet<int>(_assetService.GetThingsForGame(game).Select(t => t.TypeId));
            var validMusicSet = new HashSet<string>(_assetService.GetMusicForGame(game));

            var availableThemes = _tagService.GetAvailableThemeTags(game);
            if (!availableThemes.Any()) return (palette, null);
            string primaryThemeTag = availableThemes[_random.Next(availableThemes.Count)];

            var themeAssets = _tagService.GetAssetsForTag(game, primaryThemeTag);
            FilterAssetCollection(themeAssets, validTextureSet, validFlatSet, validThingIdSet, validMusicSet);

            var functionalTags = new Dictionary<string, TaggedAssetCollection>();
            var functionalTagNames = new[] {
                "door", "switch", "light_source", "border", "support", "panel", "secret", "exit",
                "key_indicator_blue", "key_indicator_red", "key_indicator_yellow", "key_indicator_green"
            };

            foreach (var tagName in functionalTagNames)
            {
                var collection = _tagService.GetAssetsForTag(game, tagName);
                FilterAssetCollection(collection, validTextureSet, validFlatSet, validThingIdSet, validMusicSet);
                functionalTags[tagName] = collection;
            }

            var concepts = new Dictionary<string, (string type, string[] tags)>
            {
                { "wall_primary", ("texture", new []{ primaryThemeTag }) }, { "wall_accent", ("texture", new []{ primaryThemeTag, "panel" }) },
                { "wall_support", ("texture", new []{ primaryThemeTag, "support" }) }, { "wall_secret_indicator", ("texture", new []{ primaryThemeTag, "secret" }) },
                { "wall_panel", ("texture", new []{ primaryThemeTag, "panel" }) }, { "door_frame", ("texture", new []{ primaryThemeTag, "border" }) },
                { "floor_primary", ("flat", new []{ primaryThemeTag }) }, { "floor_accent", ("flat", new []{ primaryThemeTag }) },
                { "ceiling_primary", ("flat", new []{ primaryThemeTag }) }, { "ceiling_light_source", ("flat", new []{ "light_source" }) },
                { "platform_surface", ("flat", new []{ primaryThemeTag }) }, { "door_regular", ("texture", new []{ "door" }) },
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
                foreach (var tag in conceptTags.Where(t => t != primaryThemeTag && functionalTags.ContainsKey(t)))
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

                if (!candidates.Any() && conceptTags.Contains(primaryThemeTag) && conceptTags.Length > 1)
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
            return (palette, primaryThemeTag);
        }

        private List<ThematicToken> GenerateThematicTokens(string game, Dictionary<string, List<WeightedAsset>> themePalette, string primaryThemeTag)
        {
            var tokens = new List<ThematicToken>();
            var existingAssets = new HashSet<string>();

            foreach (var list in themePalette.Values)
            {
                foreach (var asset in list)
                {
                    if (existingAssets.Add(asset.Name))
                    {
                        tokens.Add(new ThematicToken
                        {
                            Name = asset.Name,
                            Type = GetTokenTypeForAsset(asset.Name, themePalette),
                            AdjacencyRules = new List<AdjacencyRule>()
                        });
                    }
                }
            }

            int desiredMonsterCount = _random.Next(4, 9);
            var finalMonsterSelection = new List<GameAssetThing>();

            if (!string.IsNullOrEmpty(primaryThemeTag))
            {
                var allThings = _assetService.GetThingsForGame(game);

                // 1. Obtenir les monstres thématiques
                var thematicThingIds = new HashSet<int>(_tagService.GetAssetsForTag(game, primaryThemeTag).Things);
                var allMonsterIds = new HashSet<int>(_tagService.GetAssetsForTag(game, "monster").Things);
                var candidateIds = thematicThingIds.Intersect(allMonsterIds).ToList();
                var thematicMonsterCandidates = allThings.Where(t => candidateIds.Contains(t.TypeId)).ToList();

                // 2. Ajouter les monstres thématiques uniques à la sélection finale
                var shuffledThematicMonsters = thematicMonsterCandidates.OrderBy(m => _random.Next()).ToList();
                finalMonsterSelection.AddRange(shuffledThematicMonsters.Take(desiredMonsterCount));
            }

            // 3. Compléter la sélection si nécessaire avec des monstres non-thématiques
            if (finalMonsterSelection.Count < desiredMonsterCount)
            {
                int needed = desiredMonsterCount - finalMonsterSelection.Count;
                var allMonsters = _assetService.GetThingsForGame(game)
                    .Where(t => _tagService.GetAssetsForTag(game, "monster").Things.Contains(t.TypeId)).ToList();

                var alreadySelectedIds = new HashSet<int>(finalMonsterSelection.Select(m => m.TypeId));

                var fallbackCandidates = allMonsters.Where(m => !alreadySelectedIds.Contains(m.TypeId)).ToList();
                var shuffledFallbackMonsters = fallbackCandidates.OrderBy(m => _random.Next()).ToList();

                finalMonsterSelection.AddRange(shuffledFallbackMonsters.Take(needed));
            }

            // 4. Ajouter les monstres sélectionnés aux tokens
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
            // FIN de la nouvelle logique

            return tokens;
        }

        private void FilterAssetCollection(TaggedAssetCollection collection, HashSet<string> validTextures, HashSet<string> validFlats, HashSet<int> validThings, HashSet<string> validMusic)
        {
            if (collection == null) return;
            collection.Textures = collection.Textures?.Where(t => validTextures.Contains(t)).ToList() ?? new List<string>();
            collection.Flats = collection.Flats?.Where(f => validFlats.Contains(f)).ToList() ?? new List<string>();
            collection.Things = collection.Things?.Where(t => validThings.Contains(t)).ToList() ?? new List<int>();
            collection.Music = collection.Music?.Where(m => validMusic.Contains(m)).ToList() ?? new List<string>();
        }

        private string GetTokenTypeForAsset(string assetName, Dictionary<string, List<WeightedAsset>> themePalette)
        {
            foreach (var kvp in themePalette)
            {
                if (kvp.Value.Any(a => a.Name == assetName))
                {
                    if (kvp.Key.StartsWith("switch_")) { return "connection_action"; }
                    if (kvp.Key.StartsWith("wall_") || kvp.Key.StartsWith("door_")) { return "wall"; }
                    return "flat";
                }
            }
            return "wall";
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