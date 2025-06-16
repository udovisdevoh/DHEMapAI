using DGenesis.Models;
using DGenesis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace DGenesis.Pages
{
    public class IndexModel : PageModel
    {
        private readonly GameAssetService _assetService;

        public IndexModel(GameAssetService assetService)
        {
            _assetService = assetService;
        }

        [BindProperty]
        public new GenesisPromptRequest Request { get; set; } = new GenesisPromptRequest();

        public string GeneratedPrompt { get; private set; }

        private record DefaultMapDetails(string MapLumpName, string Music);

        public void OnGet()
        {
        }

        public void OnPost()
        {
            if (!ModelState.IsValid)
            {
                GeneratedPrompt = "Veuillez corriger les erreurs dans le formulaire.";
                return;
            }
            GeneratedPrompt = BuildPrompt();
        }

        private string BuildPrompt()
        {
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine("Votre mission est de générer un fichier JSON complet et valide qui respecte scrupuleusement le format 'D-Genesis'.");
            promptBuilder.AppendLine("Vous devez utiliser la spécification, les listes d'assets et les directives de conception ci-dessous pour accomplir cette tâche.");
            promptBuilder.AppendLine("Ne produisez QUE le bloc de code JSON final, sans aucun texte ou commentaire additionnel.");

            promptBuilder.AppendLine("\n--- PARTIE 1 : SPÉCIFICATION DU FORMAT D-GENESIS ---\n");
            promptBuilder.AppendLine(GetDGenesisSpecification());

            promptBuilder.AppendLine("\n--- PARTIE 2 : ASSETS DE RÉFÉRENCE POUR LE JEU '" + Request.Game.ToUpper() + "' ---\n");
            promptBuilder.AppendLine("## TEXTURES (WALLS)");
            promptBuilder.AppendLine(string.Join(", ", _assetService.GetTexturesForGame(Request.Game)));
            promptBuilder.AppendLine("\n## FLATS (FLOORS/CEILINGS)");
            promptBuilder.AppendLine(string.Join(", ", _assetService.GetFlatsForGame(Request.Game)));
            promptBuilder.AppendLine("\n## THINGS (OBJECTS)");
            var thingsList = _assetService.GetThingsForGame(Request.Game)
                .Select(t => $"- {t.Name} (typeId: {t.TypeId})");
            promptBuilder.AppendLine(string.Join("\n", thingsList));
            promptBuilder.AppendLine("\n## MUSIC");
            promptBuilder.AppendLine(string.Join(", ", _assetService.GetMusicForGame(Request.Game)));

            var defaultDetails = GetDefaultMapDetailsForGame(Request.Game);
            promptBuilder.AppendLine("\n--- PARTIE 3 : DIRECTIVES DE CONCEPTION ---\n");
            promptBuilder.AppendLine("## mapInfo");
            promptBuilder.AppendLine($"- game: \"{Request.Game}\"");
            promptBuilder.AppendLine($"- mapLumpName: \"{defaultDetails.MapLumpName}\"");
            promptBuilder.AppendLine($"- music: \"{defaultDetails.Music}\"");
            promptBuilder.AppendLine("- name: (Vous devez inventer un nom de carte cohérent avec le thème)");

            promptBuilder.AppendLine("\n## generationParams");
            promptBuilder.AppendLine($"- roomCount: {Request.RoomCount}");
            promptBuilder.AppendLine($"- secretRoomPercentage: {Request.SecretRoomPercentage.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
            promptBuilder.AppendLine($"- avgConnectivity: {Request.AvgConnectivity.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}");
            promptBuilder.AppendLine($"- avgFloorHeightDelta: {Request.AvgFloorHeightDelta}");
            promptBuilder.AppendLine($"- avgHeadroom: {Request.AvgHeadroom}");
            promptBuilder.AppendLine($"- totalVerticalSpan: {Request.TotalVerticalSpan}");

            promptBuilder.AppendLine("\n## Thème et Comportement (Instructions pour créer les thematicTokens)");
            promptBuilder.AppendLine("À partir des descriptions ci-dessous, vous devez créer la liste `thematicTokens` en choisissant des assets pertinents dans les listes de la PARTIE 2. Définissez des `baseWeight` et des `adjacencyRules` logiques pour créer un thème cohérent.");
            promptBuilder.AppendLine("\n### Thème Architectural (murs, sols, plafonds, style des connexions):");
            promptBuilder.AppendLine(Request.ArchitecturalTheme);
            promptBuilder.AppendLine("\n### Thème de Gameplay (monstres, objets, ambiance, rythme):");
            promptBuilder.AppendLine(Request.GameplayTheme);

            return promptBuilder.ToString();
        }

        private string GetDGenesisSpecification()
        {
            try
            {
                string basePath = AppContext.BaseDirectory;
                string filePath = Path.Combine(basePath, "readme.md");

                if (System.IO.File.Exists(filePath))
                {
                    return System.IO.File.ReadAllText(filePath);
                }
                else
                {
                    return "ERREUR: Le fichier readme.md est introuvable. Assurez-vous que le fichier est à la racine du projet et que sa propriété 'Copier dans le répertoire de sortie' est définie sur 'Copier si plus récent' ou 'Toujours copier'.";
                }
            }
            catch (Exception ex)
            {
                return $"ERREUR lors de la lecture du fichier de spécification: {ex.Message}";
            }
        }

        private DefaultMapDetails GetDefaultMapDetailsForGame(string game)
        {
            return game.ToLower() switch
            {
                "doom" => new DefaultMapDetails(MapLumpName: "E1M1", Music: "D_E1M1"),
                "doom2" => new DefaultMapDetails(MapLumpName: "MAP01", Music: "D_RUNNIN"),
                "heretic" => new DefaultMapDetails(MapLumpName: "E1M1", Music: "MUS_E1M1"),
                "hexen" => new DefaultMapDetails(MapLumpName: "MAP01", Music: "MUS_WINTRO"),
                _ => new DefaultMapDetails(MapLumpName: "MAP01", Music: "D_RUNNIN"), // Fallback
            };
        }
    }
}