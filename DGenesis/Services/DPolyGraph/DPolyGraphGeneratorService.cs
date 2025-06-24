using DGenesis.Models.DGraph;
using DGenesis.Models.DPolyGraph;
using DGenesis.Models.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DPolyGraphGeneratorService
    {
        private readonly SectorLayoutService _layoutService;
        private readonly GraphAnalysisService _graphAnalysisService; // NOUVEAU
        private readonly Random _random = new Random();

        public DPolyGraphGeneratorService(SectorLayoutService layoutService, GraphAnalysisService graphAnalysisService)
        {
            _layoutService = layoutService;
            _graphAnalysisService = graphAnalysisService; // NOUVEAU
        }

        public DPolyGraph Generate(DGraph dGraph)
        {
            var allNodes = new List<DGraphNode>(dGraph.Nodes);
            var nodeMap = dGraph.Nodes.ToDictionary(n => n.Id);
            int voidNodeId = -1;

            // --- ÉTAPE 1: Ajout des vides intérieurs (cours) ---
            var cycles = _graphAnalysisService.FindSimpleCycles(dGraph, 3, 5);
            foreach (var cycle in cycles)
            {
                double centroidX = 0, centroidY = 0;
                foreach (var nodeId in cycle)
                {
                    centroidX += nodeMap[nodeId].Position.X;
                    centroidY += nodeMap[nodeId].Position.Y;
                }

                allNodes.Add(new DGraphNode
                {
                    Id = voidNodeId--,
                    Type = "void",
                    Position = new Position
                    {
                        X = centroidX / cycle.Count,
                        Y = centroidY / cycle.Count
                    }
                });
            }

            // --- ÉTAPE 2: Ajout des vides extérieurs ---
            var bbox = CalculateBoundingBox(dGraph.Nodes);
            int externalVoids = _random.Next(2, 5);
            for (int i = 0; i < externalVoids; i++)
            {
                double x = 0, y = 0;
                double side = _random.Next(4);
                if (side == 0) { x = bbox.MinX - 200; y = _random.NextDouble() * (bbox.MaxY - bbox.MinY) + bbox.MinY; }
                else if (side == 1) { x = bbox.MaxX + 200; y = _random.NextDouble() * (bbox.MaxY - bbox.MinY) + bbox.MinY; }
                else if (side == 2) { y = bbox.MinY - 200; x = _random.NextDouble() * (bbox.MaxX - bbox.MinX) + bbox.MinX; }
                else { y = bbox.MaxY + 200; x = _random.NextDouble() * (bbox.MaxX - bbox.MinX) + bbox.MinX; }

                allNodes.Add(new DGraphNode
                {
                    Id = voidNodeId--,
                    Type = "void",
                    Position = new Position { X = x, Y = y }
                });
            }

            // --- ÉTAPE 3: Génération de la géométrie avec TOUS les nœuds ---
            var polygonLayout = _layoutService.GenerateLayout(allNodes);
            var polyGraph = new DPolyGraph();
            var lockToKeyMap = new Dictionary<int, int>();

            foreach (var node in allNodes)
            {
                var sector = new DPolySector
                {
                    Id = node.Id,
                    Type = node.Type,
                    Polygon = polygonLayout.ContainsKey(node.Id) ? polygonLayout[node.Id] : new List<DPolyVertex>()
                };

                if (node.Id >= 0 && node.Unlocks != null && node.Unlocks.Any())
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

            // --- ÉTAPE 4: Finalisation ---
            foreach (var sector in polyGraph.Sectors)
            {
                if (lockToKeyMap.TryGetValue(sector.Id, out int keyId))
                {
                    sector.UnlockedBySector = keyId;
                }
            }

            return polyGraph;
        }

        private BoundingBox CalculateBoundingBox(IReadOnlyList<DGraphNode> nodes)
        {
            var allX = nodes.Select(n => n.Position.X).ToList();
            var allY = nodes.Select(n => n.Position.Y).ToList();
            return new BoundingBox
            {
                MinX = allX.Any() ? allX.Min() : 0,
                MaxX = allX.Any() ? allX.Max() : 0,
                MinY = allY.Any() ? allY.Min() : 0,
                MaxY = allY.Any() ? allY.Max() : 0
            };
        }
    }
}