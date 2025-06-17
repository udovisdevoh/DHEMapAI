using DGenesis.Models;
using DGenesis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DGenesis.Pages
{
    public class DGenesisRandomGeneratorModel : PageModel
    {
        private readonly DGenesisRandomGeneratorService _generatorService;
        
        public string GeneratedJson { get; private set; }

        public DGenesisRandomGeneratorModel(DGenesisRandomGeneratorService generatorService)
        {
            _generatorService = generatorService;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // Pour l'instant, on génère seulement pour doom2.
            // On pourra ajouter un sélecteur de jeu plus tard.
            var dgenesisFile = _generatorService.Generate("doom2");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            GeneratedJson = JsonSerializer.Serialize(dgenesisFile, options);

            return Page();
        }
    }
}