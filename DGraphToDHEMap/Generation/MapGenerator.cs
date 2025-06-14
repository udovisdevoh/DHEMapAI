using System;
using System.Linq;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Models.Dhemap;

namespace DGraphBuilder.Generation
{
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

            Console.WriteLine("Étape 1: Calcul de la disposition des pièces...");
            var layoutEngine = new LayoutEngine(_dgraph, _random);
            var physicalLayout = layoutEngine.CalculateLayout();

            Console.WriteLine("\nÉtape 2: Construction de la géométrie DHEMap...");
            var mapBuilder = new MapBuilder(_dgraph, _random);
            var dhemap = mapBuilder.Build(physicalLayout);

            return dhemap;
        }
    }
}