using DGenesis.Models.DGraph;
using DGenesis.Models.DPolyGraph;
using DGenesis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DGenesis.Pages
{
    public class DPolyGraphGeneratorModel : PageModel
    {
        private readonly DPolyGraphGeneratorService _generatorService;

        [BindProperty]
        public string InputDGraphJson { get; set; }

        public string OutputDPolyGraphJson { get; private set; }

        public string ErrorMessage { get; private set; }

        public DPolyGraphGeneratorModel(DPolyGraphGeneratorService generatorService)
        {
            _generatorService = generatorService;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(InputDGraphJson))
            {
                ErrorMessage = "Le JSON d'input DGraph ne peut pas être vide.";
                return Page();
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dGraph = JsonSerializer.Deserialize<DGraph>(InputDGraphJson, options);

                if (dGraph == null || dGraph.Nodes == null || dGraph.Nodes.Count == 0)
                {
                    ErrorMessage = "Le JSON DGraph est invalide ou ne contient aucun nœud.";
                    return Page();
                }

                var polyGraph = _generatorService.Generate(dGraph);

                var serializeOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                OutputDPolyGraphJson = JsonSerializer.Serialize(polyGraph, serializeOptions);
            }
            catch (JsonException ex)
            {
                ErrorMessage = $"Erreur de parsing JSON : {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Une erreur inattendue est survenue : {ex.Message}";
            }

            return Page();
        }
    }
}