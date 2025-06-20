using System.ComponentModel.DataAnnotations;

namespace DGenesis.Models
{
    public class GenesisPromptRequest
    {
        [Required]
        [Display(Name = "Jeu Cible")]
        public string Game { get; set; } = "doom2";

        [Display(Name = "Nombre de Pièces")]
        [Range(3, 100)]
        public int RoomCount { get; set; } = 12;

        [Display(Name = "Probabilité de Secret (0.0 - 1.0)")]
        [Range(0.0, 1.0)]
        public double SecretRoomPercentage { get; set; } = 0.15;

        [Display(Name = "Connectivité Moyenne")]
        [Range(1.0, 5.0)]
        public double AvgConnectivity { get; set; } = 2.5;

        [Display(Name = "Delta de Hauteur Moyen")]
        public int AvgFloorHeightDelta { get; set; } = 48;

        [Display(Name = "Hauteur Moyenne des Pièces")]
        public int AvgHeadroom { get; set; } = 128;

        [Display(Name = "Envergure Verticale Totale")]
        public int TotalVerticalSpan { get; set; } = 1024;

        [Display(Name = "Description du Thème Architectural")]
        [DataType(DataType.MultilineText)]
        public string ArchitecturalTheme { get; set; } = "(Soyez créatif)";

        [Display(Name = "Description du Gameplay (Monstres, Objets, Ambiance)")]
        [DataType(DataType.MultilineText)]
        public string GameplayTheme { get; set; } = "(Soyez créatif)";
    }
}