// MapGenerator.cs
using System;
using System.Linq;
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
            if (!_dgraph.Rooms.Any(r => r.ParentRoom == null))
                return new DhemapFile();

            Console.WriteLine("Étape 1: Calcul de la disposition des polygones de pièces...");
            var layoutEngine = new LayoutEngine(_dgraph, _random);
            bool layoutSuccess = layoutEngine.GenerateLayout();

            if (!layoutSuccess)
            {
                Console.WriteLine("ERREUR: La génération du layout a échoué. Impossible de placer toutes les pièces.");
                return new DhemapFile(); // Retourne une carte vide
            }

            Console.WriteLine("\nÉtape 2: Construction de la géométrie DHEMap à partir des polygones...");
            var mapBuilder = new MapBuilder(_dgraph, _random);
            var dhemap = mapBuilder.Build(layoutEngine.PlacedPolygons);

            return dhemap;
        }
    }
}