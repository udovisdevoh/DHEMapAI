using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Generation;

namespace DGraphBuilder
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: dotnet run <inputFile.dgraph> <outputFile.dhemap> [seed]");
                return;
            }

            string inputFile = args[0];
            string outputFile = args[1];
            int? seed = args.Length > 2 && int.TryParse(args[2], out int parsedSeed) ? parsedSeed : null;

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Erreur : Fichier d'entrée introuvable : {inputFile}");
                return;
            }

            try
            {
                Console.WriteLine($"Lecture du fichier D-Graph : {inputFile}");
                var dgraphOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
                var dgraph = JsonSerializer.Deserialize<DGraphFile>(File.ReadAllText(inputFile), dgraphOptions);

                Console.WriteLine($"Génération de la carte '{dgraph.MapInfo.Name}'...");
                if (seed.HasValue) Console.WriteLine($"Utilisation de la seed : {seed.Value}");

                var generator = new MapGenerator(dgraph, seed);
                var dhemap = generator.Generate();

                Console.WriteLine("Sérialisation vers le format DHEMap...");
                var dhemapOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                string dhemapJson = JsonSerializer.Serialize(dhemap, dhemapOptions);

                File.WriteAllText(outputFile, dhemapJson);
                Console.WriteLine($"Succès ! Fichier DHEMap généré : {outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur est survenue : {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}