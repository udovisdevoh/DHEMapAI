using DGenesis.Models.DGraph;
using DGenesis.Models.DPolyGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class CorridorGenerationService
    {
        public List<DPolySector> GenerateCorridors(DGraph graph, DPolyGraph polyGraph, ref int corridorIdCounter)
        {
            var corridors = new List<DPolySector>();
            var polyMap = polyGraph.Sectors.Where(s => s.Id >= 0).ToDictionary(s => s.Id);

            if (polyMap.Count == 0) return corridors;

            var existingAdjacencies = FindGeometricAdjacencies(polyMap);

            foreach (var edge in graph.Edges)
            {
                // Vérifie si les polygones sont déjà adjacents géométriquement
                bool isAdjacent = existingAdjacencies.ContainsKey(edge.Source) && existingAdjacencies[edge.Source].Contains(edge.Target);

                if (!isAdjacent)
                {
                    if (!polyMap.ContainsKey(edge.Source) || !polyMap.ContainsKey(edge.Target)) continue;

                    var polyA = polyMap[edge.Source].Polygon;
                    var polyB = polyMap[edge.Target].Polygon;

                    FindClosestPoints(polyA, polyB, out var closestPointA, out var closestPointB);

                    if (closestPointA != null && closestPointB != null)
                    {
                        var corridorPolygon = CreateCorridorPolygon(closestPointA, closestPointB, 32.0); // Largeur du corridor : 32 unités
                        if (corridorPolygon.Any())
                        {
                            corridors.Add(new DPolySector
                            {
                                Id = corridorIdCounter--,
                                Type = "corridor",
                                Polygon = corridorPolygon
                            });
                        }
                    }
                }
            }
            return corridors;
        }

        private Dictionary<int, HashSet<int>> FindGeometricAdjacencies(Dictionary<int, DPolySector> polyMap)
        {
            var adjacencies = new Dictionary<int, HashSet<int>>();
            var sectors = polyMap.Values.ToList();

            foreach (var s in sectors) { adjacencies[s.Id] = new HashSet<int>(); }

            for (int i = 0; i < sectors.Count; i++)
            {
                for (int j = i + 1; j < sectors.Count; j++)
                {
                    var sectorA = sectors[i];
                    var sectorB = sectors[j];

                    // Itération sur chaque arête du polygone A
                    for (int k = 0; k < sectorA.Polygon.Count; k++)
                    {
                        var pA1 = sectorA.Polygon[k];
                        var pA2 = sectorA.Polygon[(k + 1) % sectorA.Polygon.Count];

                        // Itération sur chaque arête du polygone B
                        for (int l = 0; l < sectorB.Polygon.Count; l++)
                        {
                            var pB1 = sectorB.Polygon[l];
                            var pB2 = sectorB.Polygon[(l + 1) % sectorB.Polygon.Count];

                            // Une arête est partagée si ses points de départ/fin correspondent dans l'ordre inverse
                            if (ArePointsClose(pA1, pB2) && ArePointsClose(pA2, pB1))
                            {
                                adjacencies[sectorA.Id].Add(sectorB.Id);
                                adjacencies[sectorB.Id].Add(sectorA.Id);
                                goto next_sector_pair; // Optimisation: passer à la prochaine paire de secteurs
                            }
                        }
                    }
                next_sector_pair:;
                }
            }
            return adjacencies;
        }

        private bool ArePointsClose(DPolyVertex p1, DPolyVertex p2, double tolerance = 0.1)
        {
            return Math.Abs(p1.X - p2.X) < tolerance && Math.Abs(p1.Y - p2.Y) < tolerance;
        }

        private void FindClosestPoints(List<DPolyVertex> polyA, List<DPolyVertex> polyB, out DPolyVertex pointA, out DPolyVertex pointB)
        {
            double minDistanceSq = double.MaxValue;
            pointA = null;
            pointB = null;

            if (polyA == null || polyB == null || !polyA.Any() || !polyB.Any()) return;

            foreach (var vA in polyA)
            {
                foreach (var vB in polyB)
                {
                    double distSq = Math.Pow(vA.X - vB.X, 2) + Math.Pow(vA.Y - vB.Y, 2);
                    if (distSq < minDistanceSq)
                    {
                        minDistanceSq = distSq;
                        pointA = vA;
                        pointB = vB;
                    }
                }
            }
        }

        private List<DPolyVertex> CreateCorridorPolygon(DPolyVertex pA, DPolyVertex pB, double width)
        {
            var corridorVector = new DPolyVertex { X = pB.X - pA.X, Y = pB.Y - pA.Y };
            var length = Math.Sqrt(corridorVector.X * corridorVector.X + corridorVector.Y * corridorVector.Y);
            if (length < 1e-9) return new List<DPolyVertex>();

            var normalized = new DPolyVertex { X = corridorVector.X / length, Y = corridorVector.Y / length };
            var perpendicular = new DPolyVertex { X = -normalized.Y, Y = normalized.X };

            double halfWidth = width / 2.0;

            var v1 = new DPolyVertex { X = pA.X + perpendicular.X * halfWidth, Y = pA.Y + perpendicular.Y * halfWidth };
            var v2 = new DPolyVertex { X = pB.X + perpendicular.X * halfWidth, Y = pB.Y + perpendicular.Y * halfWidth };
            var v3 = new DPolyVertex { X = pB.X - perpendicular.X * halfWidth, Y = pB.Y - perpendicular.Y * halfWidth };
            var v4 = new DPolyVertex { X = pA.X - perpendicular.X * halfWidth, Y = pA.Y - perpendicular.Y * halfWidth };

            return new List<DPolyVertex> { v1, v2, v3, v4 }.Select(v => new DPolyVertex { X = Math.Round(v.X, 2), Y = Math.Round(v.Y, 2) }).ToList();
        }
    }
}