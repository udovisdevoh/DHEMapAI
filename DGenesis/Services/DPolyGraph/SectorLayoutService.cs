using DGenesis.Models.DGraph;
using DGenesis.Models.DPolyGraph;
using DGenesis.Models.Geometry;
using DGenesis.Services.Geometric;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services
{
    public class SectorLayoutService
    {
        private readonly PolygonClippingService _clippingService;

        public SectorLayoutService(PolygonClippingService clippingService)
        {
            _clippingService = clippingService;
        }

        // La méthode ne prend plus que la liste de nœuds
        public Dictionary<int, List<DPolyVertex>> GenerateLayout(IReadOnlyList<DGraphNode> allNodes)
        {
            if (allNodes.Count < 2)
            {
                if (allNodes.Count == 1)
                {
                    var singleNodeResults = new Dictionary<int, List<DPolyVertex>>();
                    singleNodeResults[allNodes[0].Id] = BoundingBoxToPolygon(CalculateBoundingBox(allNodes));
                    return singleNodeResults;
                }
                return new Dictionary<int, List<DPolyVertex>>();
            }

            var boundingBox = CalculateBoundingBox(allNodes);
            var results = new Dictionary<int, List<DPolyVertex>>();

            foreach (var node in allNodes)
            {
                List<DPolyVertex> nodePolygon = BoundingBoxToPolygon(boundingBox);

                // CORRECTION : On revient à la logique de Voronoï pur en clippant contre TOUS les autres nœuds.
                // Cela garantit l'absence de chevauchement.
                foreach (var otherNode in allNodes)
                {
                    if (node.Id == otherNode.Id) continue;

                    var p1 = new DPolyVertex { X = node.Position.X, Y = node.Position.Y };
                    var p2 = new DPolyVertex { X = otherNode.Position.X, Y = otherNode.Position.Y };

                    var midPoint = new DPolyVertex { X = (p1.X + p2.X) / 2, Y = (p1.Y + p2.Y) / 2 };
                    var perpendicularVector = new DPolyVertex { X = p1.Y - p2.Y, Y = p2.X - p1.X };

                    double largeScalar = 10000.0;
                    var pA = new DPolyVertex { X = midPoint.X - perpendicularVector.X * largeScalar, Y = midPoint.Y - perpendicularVector.Y * largeScalar };
                    var pB = new DPolyVertex { X = midPoint.X + perpendicularVector.X * largeScalar, Y = midPoint.Y + perpendicularVector.Y * largeScalar };

                    var clipLine = new Line { Point1 = pA, Point2 = pB };

                    nodePolygon = _clippingService.Clip(nodePolygon, clipLine);
                }

                var sortedPolygon = SortVerticesOfConvexPolygon(nodePolygon);

                // Le rétrécissement n'est plus nécessaire ici.
                results[node.Id] = sortedPolygon.Select(v => new DPolyVertex { X = Math.Round(v.X, 2), Y = Math.Round(v.Y, 2) }).ToList();
            }

            return results;
        }

        private List<DPolyVertex> SortVerticesOfConvexPolygon(List<DPolyVertex> polygon)
        {
            if (polygon == null || polygon.Count < 3) return polygon;

            double centroidX = polygon.Average(v => v.X);
            double centroidY = polygon.Average(v => v.Y);

            return polygon.OrderBy(v => Math.Atan2(v.Y - centroidY, v.X - centroidX)).ToList();
        }

        private BoundingBox CalculateBoundingBox(IReadOnlyList<DGraphNode> nodes)
        {
            var allX = nodes.Select(n => n.Position.X).ToList();
            var allY = nodes.Select(n => n.Position.Y).ToList();
            double padding = 1000;

            return new BoundingBox
            {
                MinX = (allX.Any() ? allX.Min() : 0) - padding,
                MaxX = (allX.Any() ? allX.Max() : 0) + padding,
                MinY = (allY.Any() ? allY.Min() : 0) - padding,
                MaxY = (allY.Any() ? allY.Max() : 0) + padding
            };
        }

        private List<DPolyVertex> BoundingBoxToPolygon(BoundingBox box)
        {
            return new List<DPolyVertex>
            {
                new DPolyVertex { X = box.MinX, Y = box.MinY },
                new DPolyVertex { X = box.MaxX, Y = box.MinY },
                new DPolyVertex { X = box.MaxX, Y = box.MaxY },
                new DPolyVertex { X = box.MinX, Y = box.MaxY }
            };
        }
    }
}