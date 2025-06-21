using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services.Composite
{
    public class DShapeFusionService
    {
        public DShape Fuse(DShape shapeA, DShape shapeB)
        {
            if (shapeA == null || shapeA.Vertices.Count == 0) return shapeB;
            if (shapeB == null || shapeB.Vertices.Count == 0) return shapeA;

            // 1. Trouver la paire de sommets la plus proche entre les deux formes
            int indexA = -1, indexB = -1;
            double minDistanceSq = double.MaxValue;

            for (int i = 0; i < shapeA.Vertices.Count; i++)
            {
                for (int j = 0; j < shapeB.Vertices.Count; j++)
                {
                    double distSq = Math.Pow(shapeA.Vertices[i].X - shapeB.Vertices[j].X, 2) +
                                  Math.Pow(shapeA.Vertices[i].Y - shapeB.Vertices[j].Y, 2);
                    if (distSq < minDistanceSq)
                    {
                        minDistanceSq = distSq;
                        indexA = i;
                        indexB = j;
                    }
                }
            }

            // 2. "Recoudre" les listes de sommets pour créer un nouveau polygone unique
            // C'est la logique simple qui crée une forme en "8", produisant des intersections
            // qui seront ensuite traitées par le PolygonRepairService.
            var newVertices = new List<DShapeVertex>();

            // Ajouter la première partie de la forme A
            for (int i = 0; i <= indexA; i++)
            {
                newVertices.Add(shapeA.Vertices[i]);
            }

            // Ajouter toute la forme B, en commençant par le point de connexion
            for (int i = 0; i < shapeB.Vertices.Count; i++)
            {
                int currentIndex = (indexB + i) % shapeB.Vertices.Count;
                newVertices.Add(shapeB.Vertices[currentIndex]);
            }

            // Ajouter le reste de la forme A
            for (int i = indexA + 1; i < shapeA.Vertices.Count; i++)
            {
                newVertices.Add(shapeA.Vertices[i]);
            }

            return new DShape { Vertices = newVertices };
        }
    }
}