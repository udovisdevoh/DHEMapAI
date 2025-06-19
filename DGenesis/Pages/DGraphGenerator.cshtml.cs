using DGenesis.Models.DGraph;
using DGenesis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DGenesis.Pages
{
    public class DGraphGeneratorModel : PageModel
    {
        private readonly DGraphGeneratorService _generatorService;

        // Propriétés liées au formulaire
        [BindProperty]
        public int TotalNodes { get; set; } = 10;

        [BindProperty]
        public int LockedPairs { get; set; } = 1;

        [BindProperty]
        public int ExitNodes { get; set; } = 1;

        // Propriété pour stocker le JSON généré
        public string? GeneratedDGraphJson { get; private set; }

        public DGraphGeneratorModel(DGraphGeneratorService generatorService)
        {
            _generatorService = generatorService;
        }

        public void OnGet()
        {
            // La page s'affiche avec les valeurs par défaut du formulaire
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Appeler le service pour générer le graphe
            var dgraph = _generatorService.Generate(TotalNodes, LockedPairs, ExitNodes);

            // Sérialiser le graphe en JSON pour l'envoyer à la vue
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            GeneratedDGraphJson = JsonSerializer.Serialize(dgraph, options);

            return Page();
        }
    }
}