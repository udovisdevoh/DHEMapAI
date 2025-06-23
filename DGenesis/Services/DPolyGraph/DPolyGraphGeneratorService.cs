using DGenesis.Models.DGraph;
using DGenesis.Models.DPolyGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class DPolyGraphGeneratorService
    {
        private readonly Random _random = new Random();

        public DPolyGraph Generate(DGraph dGraph)
        {
            var polyGraph = new DPolyGraph();
            var lockToKeyMap = new Dictionary<int, int>();

            // Première passe pour créer les secteurs et mapper les relations de déverrouillage
            foreach (var node in dGraph.Nodes)
            {
                var sector = new DPolySector
                {
                    Id = node.Id,
                    Type = node.Type,
                    Polygon = GenerateRandomPolygon(node.Position)
                };

                // Si le noeud DGraph a une liste "Unlocks", on prend le premier pour la relation 1-1
                if (node.Unlocks != null && node.Unlocks.Any())
                {
                    int lockedId = node.Unlocks.First();
                    sector.UnlocksSector = lockedId;
                    // On mappe la relation pour la passe suivante
                    if (!lockToKeyMap.ContainsKey(lockedId))
                    {
                        lockToKeyMap.Add(lockedId, node.Id);
                    }
                }

                polyGraph.Sectors.Add(sector);
            }

            // Deuxième passe pour assigner la propriété "unlockedBySector"
            foreach (var sector in polyGraph.Sectors)
            {
                if (lockToKeyMap.TryGetValue(sector.Id, out int keyId))
                {
                    sector.UnlockedBySector = keyId;
                }
            }

            return polyGraph;
        }

        private List<DPolyVertex> GenerateRandomPolygon(Position center, int minVertices = 4, int maxVertices = 7, double minRadius = 80.0, double maxRadius = 160.0)
        {
            int vertexCount = _random.Next(minVertices, maxVertices + 1);
            var angles = new List<double>();

            for (int i = 0; i < vertexCount; i++)
            {
                angles.Add(_random.NextDouble() * 2 * Math.PI);
            }
            // Trier les angles assure que le polygone ne se croise pas lui-même (il sera "étoilé")
            angles.Sort();

            var vertices = new List<DPolyVertex>();
            foreach (var angle in angles)
            {
                double radius = minRadius + _random.NextDouble() * (maxRadius - minRadius);
                vertices.Add(new DPolyVertex
                {
                    X = Math.Round(center.X + radius * Math.Cos(angle), 2),
                    Y = Math.Round(center.Y + radius * Math.Sin(angle), 2)
                });
            }

            return vertices;
        }
    }
}