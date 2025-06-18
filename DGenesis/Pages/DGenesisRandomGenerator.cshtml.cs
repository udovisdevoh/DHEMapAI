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

        // NOUVEAUTÉ 1 : Propriété pour recevoir la valeur du menu déroulant
        // "SelectedGame" correspond à l'attribut "name" de la balise <select>
        [BindProperty]
        public string SelectedGame { get; set; } = "doom2"; // "doom2" est la valeur par défaut

        public DGenesisRandomGeneratorModel(DGenesisRandomGeneratorService generatorService)
        {
            _generatorService = generatorService;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // NOUVEAUTÉ 2 : Utilisation de la propriété "SelectedGame" au lieu d'une valeur codée en dur
            var dgenesisFile = _generatorService.Generate(SelectedGame);

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