using DGenesis.Models;
using DGenesis.Models.DGraph;
using DGenesis.Services.Deformations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services.Composite
{
    public class DCompositeGeneratorService
    {
        private readonly DShapeGeneratorService _shapeGenerator;
        private readonly DShapeDeformationService _deformationService;
        private readonly DShapeFusionService _fusionService;

        public DCompositeGeneratorService(
            DShapeGeneratorService shapeGenerator,
            DShapeDeformationService deformationService,
            DShapeFusionService fusionService)
        {
            _shapeGenerator = shapeGenerator;
            _deformationService = deformationService;
            _fusionService = fusionService;
        }

        public DShape Generate(DGraph skeleton, DShapeGenerationParameters roomParams, DShapeDeformationParameters deformParams)
        {
            if (skeleton == null || skeleton.Nodes.Count == 0)
                return new DShape();

            var rooms = new Dictionary<int, DShape>();

            // 1. Générer une "pièce" (DShape) pour chaque noeud du squelette
            foreach (var node in skeleton.Nodes)
            {
                var baseShape = _shapeGenerator.Generate(roomParams);
                _deformationService.Apply(baseShape, deformParams);

                // Translater la pièce à sa position dans le squelette
                foreach (var vertex in baseShape.Vertices)
                {
                    vertex.X += node.Position.X;
                    vertex.Y += node.Position.Y;
                }
                rooms[node.Id] = baseShape;
            }

            if (skeleton.Nodes.Count == 1)
            {
                return rooms[skeleton.Nodes.First().Id];
            }

            // 2. Fusionner les pièces en suivant les arêtes du squelette
            DShape compositeShape = null;
            var processedEdges = new HashSet<Tuple<int, int>>();

            // On utilise un parcours de graphe pour s'assurer que tout est connecté logiquement
            var queue = new Queue<int>();
            queue.Enqueue(skeleton.Nodes.First().Id);
            var visitedNodes = new HashSet<int> { skeleton.Nodes.First().Id };
            compositeShape = rooms[skeleton.Nodes.First().Id];

            while (queue.Any())
            {
                var currentNodeId = queue.Dequeue();
                var neighbors = skeleton.Edges
                    .Where(e => e.Source == currentNodeId || e.Target == currentNodeId)
                    .Select(e => e.Source == currentNodeId ? e.Target : e.Source);

                foreach (var neighborId in neighbors)
                {
                    if (!visitedNodes.Contains(neighborId))
                    {
                        compositeShape = _fusionService.Fuse(compositeShape, rooms[neighborId]);
                        visitedNodes.Add(neighborId);
                        queue.Enqueue(neighborId);
                    }
                }
            }

            // Arrondi final après toutes les transformations et fusions
            compositeShape.Vertices.ForEach(v => {
                v.X = Math.Round(v.X, 2);
                v.Y = Math.Round(v.Y, 2);
            });

            compositeShape.Name = "composite_shape";
            compositeShape.Description = $"Composite shape from a {skeleton.Nodes.Count}-node skeleton.";

            return compositeShape;
        }
    }
}