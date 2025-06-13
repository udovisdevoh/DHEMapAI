using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Models.Dhemap;

namespace DGraphBuilder.Generation
{
    /// <summary>
    /// Orchestre la conversion d'un D-Graph en DHEMap.
    /// </summary>
    public class MapGenerator
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;

        public MapGenerator(DGraphFile dgraph, int? seed)
        {
            _dgraph = dgraph;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public DhemapFile Generate()
        {
            // Étape 1 : Calculer la disposition physique des pièces.
            Console.WriteLine("Étape 1: Calcul de la disposition des pièces...");
            var layoutEngine = new LayoutEngine(_dgraph, _random);
            var physicalLayout = layoutEngine.CalculateLayout();

            // ### CORRECTION : Étape 1.5 - Normaliser les coordonnées ###
            Console.WriteLine("Étape 1.5: Normalisation des coordonnées de la carte...");
            NormalizeLayoutPositions(physicalLayout);

            // Étape 2 : Construire la géométrie DHEMap concrète à partir de la disposition normalisée.
            Console.WriteLine("\nÉtape 2: Construction de la géométrie DHEMap...");
            var mapBuilder = new MapBuilder(_dgraph, _random);
            var dhemap = mapBuilder.Build(physicalLayout);

            return dhemap;
        }

        private void NormalizeLayoutPositions(List<PhysicalRoom> layout)
        {
            if (!layout.Any()) return;

            float minX = float.MaxValue;
            float minY = float.MaxValue;

            foreach (var room in layout)
            {
                var box = room.GetBoundingBox();
                if (box.Left < minX) minX = box.Left;
                if (box.Top < minY) minY = box.Top;
            }

            // Décalage pour ramener le coin supérieur gauche de la carte près de l'origine (avec une marge).
            var offsetX = -minX + 128;
            var offsetY = -minY + 128;

            foreach (var room in layout)
            {
                room.Position = new Vector2(room.Position.X + offsetX, room.Position.Y + offsetY);
            }
        }
    }
}