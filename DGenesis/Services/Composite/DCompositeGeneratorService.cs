using DGenesis.Models;
using DGenesis.Services.Deformations;
using System;
using System.Collections.Generic;
using System.Linq;

// Ajout du using pour DGraph qui est utilisé comme squelette
using DGraph = DGenesis.Models.DGraph.DGraph;

namespace DGenesis.Services.Composite
{
    public class DCompositeGeneratorService
    {
        private readonly DShapeGeneratorService _shapeGenerator;
        private readonly DShapeDeformationService _deformationService;
        private readonly DShapeFusionService _fusionService;
        private readonly PolygonRepairService _repairService;

        public DCompositeGeneratorService(
            DShapeGeneratorService shapeGenerator,
            DShapeDeformationService deformationService,
            DShapeFusionService fusionService,
            PolygonRepairService repairService)
        {
            _shapeGenerator = shapeGenerator;
            _deformationService = deformationService;
            _fusionService = fusionService;
            _repairService = repairService;
        }

        public DShape Generate(DGraph skeleton, DShapeGenerationParameters roomParams, DShapeDeformationParameters deformParams)
        {
            if (skeleton == null || skeleton.Nodes.Count == 0)
                return new DShape();

            var rooms = new Dictionary<int, DShape>();

            // 1. Générer, déformer et placer une "pièce" (DShape) pour chaque noeud du squelette.
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

            // S'il n'y a qu'une seule pièce, pas besoin de fusionner ou réparer.
            if (skeleton.Nodes.Count == 1)
            {
                return rooms.Values.First();
            }

            // 2. Fusionner les pièces en suivant les arêtes du squelette.
            // On utilise un parcours de graphe pour s'assurer que tout est connecté logiquement.
            var nodesToProcess = new Queue<int>(new[] { skeleton.Nodes.First().Id });
            var processedNodes = new HashSet<int>(nodesToProcess);
            DShape compositeShape = rooms[nodesToProcess.Peek()];

            while (nodesToProcess.Any())
            {
                int currentNodeId = nodesToProcess.Dequeue();
                var neighbors = skeleton.Edges
                                      .Where(e => e.Source == currentNodeId || e.Target == currentNodeId)
                                      .Select(e => e.Source == currentNodeId ? e.Target : e.Source);

                foreach (var neighborId in neighbors)
                {
                    if (!processedNodes.Contains(neighborId))
                    {
                        compositeShape = _fusionService.Fuse(compositeShape, rooms[neighborId]);
                        processedNodes.Add(neighborId);
                        nodesToProcess.Enqueue(neighborId);
                    }
                }
            }

            // 3. ÉTAPE FINALE : Réparation du polygone fusionné pour enlever les auto-intersections.
            var finalShape = _repairService.Repair(compositeShape);

            // 4. Arrondi des coordonnées après toutes les transformations.
            finalShape.Vertices.ForEach(v => {
                v.X = Math.Round(v.X, 2);
                v.Y = Math.Round(v.Y, 2);
            });

            finalShape.Name = "composite_shape_repaired";
            finalShape.Description = $"Composite shape from a {skeleton.Nodes.Count}-node skeleton, repaired.";

            return finalShape;
        }
    }
}