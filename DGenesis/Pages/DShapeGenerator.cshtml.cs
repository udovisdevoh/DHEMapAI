using DGenesis.Models;
using DGenesis.Services;
using DGenesis.Services.Deformations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DGenesis.Pages
{
    public class DShapeGeneratorModel : PageModel
    {
        private readonly DShapeGeneratorService _generatorService;
        private readonly DShapeDeformationService _deformationService;

        [BindProperty]
        public DShapeGenerationParameters GenParams { get; set; } = new DShapeGenerationParameters();

        [BindProperty]
        public DShapeDeformationParameters DefParams { get; set; } = new DShapeDeformationParameters();


        public string GeneratedDShapeJson { get; private set; }

        public DShapeGeneratorModel(DShapeGeneratorService generatorService, DShapeDeformationService deformationService)
        {
            _generatorService = generatorService;
            _deformationService = deformationService;
        }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Étape 1: Générer la forme de base
            var dshape = _generatorService.Generate(GenParams);

            // Étape 2: Appliquer les déformations
            _deformationService.Apply(dshape, DefParams);

            // Arrondir les valeurs finales après toutes les transformations
            dshape.Vertices.ForEach(v => {
                v.X = Math.Round(v.X, 2);
                v.Y = Math.Round(v.Y, 2);
            });

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            GeneratedDShapeJson = JsonSerializer.Serialize(dshape, options);

            return Page();
        }
    }
}