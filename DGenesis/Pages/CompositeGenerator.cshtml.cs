using DGenesis.Models;
using DGenesis.Models.DGraph;
using DGenesis.Services.Composite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DGenesis.Pages
{
    public class CompositeGeneratorModel : PageModel
    {
        private readonly DCompositeGeneratorService _compositeGenerator;

        [BindProperty, Display(Name = "Squelette de la Forme (D-Graph JSON)")]
        public string InputDGraphJson { get; set; }

        [BindProperty]
        public DShapeGenerationParameters GenParams { get; set; } = new DShapeGenerationParameters();

        [BindProperty]
        public DShapeDeformationParameters DefParams { get; set; } = new DShapeDeformationParameters();

        // NOUVELLES PROPRIÉTÉS POUR CONSERVER L'ÉTAT
        [BindProperty]
        public double PresetScale { get; set; } = 400;

        [BindProperty]
        public string ActivePresetKey { get; set; } = "L";


        public string OutputDShapeJson { get; private set; }

        public CompositeGeneratorModel(DCompositeGeneratorService compositeGenerator)
        {
            _compositeGenerator = compositeGenerator;
        }

        public void OnGet()
        {
            // L'initialisation se fait maintenant entièrement en JS pour la cohérence
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var skeleton = JsonSerializer.Deserialize<DGraph>(InputDGraphJson, options);

                var finalShape = _compositeGenerator.Generate(skeleton, GenParams, DefParams);

                var outputOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                OutputDShapeJson = JsonSerializer.Serialize(finalShape, outputOptions);
            }
            catch (JsonException ex)
            {
                ModelState.AddModelError("InputDGraphJson", "Le JSON du D-Graph est invalide : " + ex.Message);
            }

            return Page();
        }
    }
}