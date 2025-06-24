using DGenesis.Models.DGraph;
using DGenesis.Models.DPolyGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DPolyGraphGeneratorService
    {
        private readonly SectorLayoutService _layoutService; // NOUVEAU

        // Le service de layout est maintenant injecté
        public DPolyGraphGeneratorService(SectorLayoutService layoutService)
        {
            _layoutService = layoutService;
        }

        public DPolyGraph Generate(DGraph dGraph)
        {
            var polyGraph = new DPolyGraph();
            var lockToKeyMap = new Dictionary<int, int>();

            // ÉTAPE 1: Générer la géométrie non-superposée
            var polygonLayout = _layoutService.GenerateLayout(dGraph.Nodes);

            // ÉTAPE 2: Créer les secteurs et mapper les relations
            foreach (var node in dGraph.Nodes)
            {
                var sector = new DPolySector
                {
                    Id = node.Id,
                    Type = node.Type,
                    // Utilise le polygone généré par le service de layout
                    Polygon = polygonLayout.ContainsKey(node.Id) ? polygonLayout[node.Id] : new List<DPolyVertex>()
                };

                if (node.Unlocks != null && node.Unlocks.Any())
                {
                    int lockedId = node.Unlocks.First();
                    sector.UnlocksSector = lockedId;
                    if (!lockToKeyMap.ContainsKey(lockedId))
                    {
                        lockToKeyMap.Add(lockedId, node.Id);
                    }
                }

                polyGraph.Sectors.Add(sector);
            }

            // ÉTAPE 3: Assigner la propriété "unlockedBySector"
            foreach (var sector in polyGraph.Sectors)
            {
                if (lockToKeyMap.TryGetValue(sector.Id, out int keyId))
                {
                    sector.UnlockedBySector = keyId;
                }
            }

            return polyGraph;
        }
    }
}