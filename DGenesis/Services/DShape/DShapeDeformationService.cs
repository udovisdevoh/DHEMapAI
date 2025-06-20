using DGenesis.Models;
using System;

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
                // Si l'angle est à 0, on utilise la méthode simple sans rotation.
                if (parameters.StretchAngle == 0)
                {
                    foreach (var vertex in shape.Vertices)
                    {
                        vertex.X *= parameters.StretchX;
                        vertex.Y *= parameters.StretchY;
                    }
                }
                else
                {
                    // Sinon, on applique la déformation sur un axe tourné.
                    // Conversion de l'angle en radians
                    double angleRad = parameters.StretchAngle * Math.PI / 180.0;
                    double cosAngle = Math.Cos(angleRad);
                    double sinAngle = Math.Sin(angleRad);

                    foreach (var vertex in shape.Vertices)
                    {
                        // Étape 1 : Rotation inverse pour aligner avec les axes globaux
                        double tempX = vertex.X * cosAngle + vertex.Y * sinAngle;
                        double tempY = -vertex.X * sinAngle + vertex.Y * cosAngle;

                        // Étape 2 : Étirement simple sur les axes alignés
                        tempX *= parameters.StretchX;
                        tempY *= parameters.StretchY;

                        // Étape 3 : Rotation pour revenir à l'orientation d'origine
                        vertex.X = tempX * cosAngle - tempY * sinAngle;
                        vertex.Y = tempX * sinAngle + tempY * cosAngle;
                    }
                }
            }
        }
    }
}