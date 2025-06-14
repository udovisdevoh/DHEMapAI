using System;
using System.Linq;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Models.Dhemap;

namespace DGraphBuilder.Generation
{
    // Classe de configuration statique pour les constantes partagées
    public static class GenerationConfig
    {
        public const int GridSize = 64;
        public const int CellSize = 512; // Augmenté pour des pièces plus grandes
    }

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

            Console.WriteLine("Étape 1: Calcul de la disposition des pièces sur une grille...");
            var layoutEngine = new LayoutEngine(_dgraph, _random);
            var gridLayout = layoutEngine.CalculateGridLayout();

            Console.WriteLine("\nÉtape 2: Construction de la géométrie DHEMap à partir de la grille...");
            var mapBuilder = new MapBuilder(_dgraph, _random);
            var dhemap = mapBuilder.Build(gridLayout);

            return dhemap;
        }
    }
}