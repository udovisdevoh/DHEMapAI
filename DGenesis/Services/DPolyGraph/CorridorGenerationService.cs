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
            var polyMap = polyGraph.Sectors.ToDictionary(s => s.Id);

            var existingAdjacencies = FindGeometricAdjacencies(polyMap);

            foreach (var edge in graph.Edges)
            {
                bool isAdjacent = existingAdjacencies.ContainsKey(edge.Source) && existingAdjacencies[edge.Source].Contains(edge.Target);

                if (!isAdjacent)
                {
                    var polyA = polyMap[edge.Source].Polygon;
                    var polyB = polyMap[edge.Target].Polygon;

                    FindClosestPoints(polyA, polyB, out var closestPointA, out var closestPointB);

                    if (closestPointA != null && closestPointB != null)
                    {
                        var corridorPolygon = CreateCorridorPolygon(closestPointA, closestPointB, 32.0); // Largeur du corridor : 32 unités
                        corridors.Add(new DPolySector
                        {
                            Id = corridorIdCounter--,
                            Type = "corridor",
                            Polygon = corridorPolygon
                        });
                    }
                }
            }
            return corridors;
        }

        private Dictionary<int, HashSet<int>> FindGeometricAdjacencies(Dictionary<int, DPolySector> polyMap)
        {
            var adjacencies = new Dictionary<int, HashSet<int>>();
            var sectors = polyMap.Values.ToList();

            for (int i = 0; i < sectors.Count; i++)
            {
                for (int j = i + 1; j < sectors.Count; j++)
                {
                    var sectorA = sectors[i];
                    var sectorB = sectors[j];

                    if (!adjacencies.ContainsKey(sectorA.Id)) adjacencies[sectorA.Id] = new HashSet<int>();
                    if (!adjacencies.ContainsKey(sectorB.Id)) adjacencies[sectorB.Id] = new HashSet<int>();

                    // Deux polygones sont adjacents s'ils partagent au moins 2 sommets (une arête)
                    int sharedVertices = sectorA.Polygon.Count(vA => sectorB.Polygon.Any(vB => Math.Abs(vA.X - vB.X) < 0.1 && Math.Abs(vA.Y - vB.Y) < 0.1));

                    if (sharedVertices >= 2)
                    {
                        adjacencies[sectorA.Id].Add(sectorB.Id);
                        adjacencies[sectorB.Id].Add(sectorA.Id);
                    }
                }
            }
            return adjacencies;
        }

        private void FindClosestPoints(List<DPolyVertex> polyA, List<DPolyVertex> polyB, out DPolyVertex pointA, out DPolyVertex pointB)
        {
            double minDistanceSq = double.MaxValue;
            pointA = null;
            pointB = null;

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