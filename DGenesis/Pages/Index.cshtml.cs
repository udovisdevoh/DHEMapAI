using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DGenesis.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<DGenesisCreationPromptGeneratorModel> _logger;

        public IndexModel(ILogger<DGenesisCreationPromptGeneratorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Aucune logique nécessaire pour une page de navigation statique
        }
    }
}