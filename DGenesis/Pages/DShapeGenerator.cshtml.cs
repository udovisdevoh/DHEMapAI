using DGenesis.Models;
using DGenesis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DGenesis.Pages
{
    public class DShapeGeneratorModel : PageModel
    {
        private readonly DShapeGeneratorService _generatorService;

        [BindProperty]
        [Display(Name = "Nombre de sommets")]
        public int VertexCount { get; set; } = 8;

        [BindProperty]
        [Display(Name = "Axes de symétrie")]
        public int SymmetryAxes { get; set; } = 0;

        [BindProperty]
        [Display(Name = "Taille")]
        public double Size { get; set; } = 256;

        [BindProperty]
        [Display(Name = "Irrégularité")]
        public double Irregularity { get; set; } = 0.25;

        public string GeneratedDShapeJson { get; private set; }

        public DShapeGeneratorModel(DShapeGeneratorService generatorService)
        {
            _generatorService = generatorService;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var dshape = _generatorService.Generate(VertexCount, SymmetryAxes, Size, Irregularity);

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