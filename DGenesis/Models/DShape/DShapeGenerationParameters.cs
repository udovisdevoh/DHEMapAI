using System.ComponentModel.DataAnnotations;

namespace DGenesis.Models
{
    public class DShapeGenerationParameters
    {
        [Display(Name = "Nombre de sommets")]
        public int VertexCount { get; set; } = 8;

        [Display(Name = "Axes de symétrie")]
        public int SymmetryAxes { get; set; } = 0;

        [Display(Name = "Type de Symétrie")]
        public string SymmetryType { get; set; } = "Axial";

        [Display(Name = "Taille (Rayon moyen)")]
        public double Size { get; set; } = 256;

        [Display(Name = "Irrégularité (0.0 à 1.0)")]
        public double Irregularity { get; set; } = 0.25;

        [Display(Name = "Angle de la symétrie (°)")]
        public double SymmetryAngle { get; set; } = 0;
    }
}