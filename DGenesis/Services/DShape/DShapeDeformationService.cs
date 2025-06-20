using DGenesis.Models;

namespace DGenesis.Services.Deformations
{
    public class DShapeDeformationService
    {
        public void Apply(DShape shape, DShapeDeformationParameters parameters)
        {
            if (shape == null || shape.Vertices == null || parameters == null)
                return;

            // Appliquer l'étirement
            if (parameters.StretchX != 1.0 || parameters.StretchY != 1.0)
            {
                foreach (var vertex in shape.Vertices)
                {
                    vertex.X *= parameters.StretchX;
                    vertex.Y *= parameters.StretchY;
                }
            }

            // D'autres déformations (Shear, Twist, etc.) pourront être ajoutées ici.
        }
    }
}