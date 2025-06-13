// Program.cs
using System;
using System.IO;
using System.Text.Json;
using DGraphBuilder.Models.DGraph;
using DGraphBuilder.Generation;

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

        Console.WriteLine($"Lecture du fichier D-Graph : {inputFile}");
        string dgraphJson = File.ReadAllText(inputFile);

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dgraph = JsonSerializer.Deserialize<DGraphFile>(dgraphJson, options);

            Console.WriteLine($"Génération de la carte '{dgraph.MapInfo.Name}' pour le jeu '{dgraph.MapInfo.Game}'...");
            if (seed.HasValue)
                Console.WriteLine($"Utilisation de la seed : {seed.Value}");

            var generator = new MapGenerator(dgraph, seed);
            var dhemap = generator.Generate();

            Console.WriteLine("Sérialisation vers le format DHEMap...");
            var dhemapOptions = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
            string dhemapJson = JsonSerializer.Serialize(dhemap, dhemapOptions);

            File.WriteAllText(outputFile, dhemapJson);
            Console.WriteLine($"Succès ! Fichier DHEMap généré : {outputFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur est survenue : {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}