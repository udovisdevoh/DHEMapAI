using System;
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
            // Étape 1 : Calculer la disposition physique des pièces sans les chevaucher.
            Console.WriteLine("Étape 1: Calcul de la disposition des pièces...");
            var layoutEngine = new LayoutEngine(_dgraph, _random);
            var physicalLayout = layoutEngine.CalculateLayout();

            // Étape 2 : Construire la géométrie DHEMap concrète à partir de la disposition calculée.
            Console.WriteLine("\nÉtape 2: Construction de la géométrie DHEMap...");
            var mapBuilder = new MapBuilder(_dgraph, _random);
            var dhemap = mapBuilder.Build(physicalLayout);

            return dhemap;
        }
    }
}