using System.ComponentModel.DataAnnotations;

namespace DGenesis.Models
{
    public class DShapeDeformationParameters
    {
        [Display(Name = "Étirement X")]
        public double StretchX { get; set; } = 1.0;

        [Display(Name = "Étirement Y")]
        public double StretchY { get; set; } = 1.0;

        [Display(Name = "Angle de l'étirement (°)")]
        public double StretchAngle { get; set; } = 0;
    }
}