using DGenesis.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DGenesis.Services.Composite
{
    public class PolygonRepairService
    {
        public DShape Repair(DShape inputShape)
        {
            var bestVertices = new List<DShapeVertex>(inputShape.Vertices);
            var currentVertices = new List<DShapeVertex>(inputShape.Vertices);
            int bestCost = CountIntersections(bestVertices);

            if (bestCost == 0) return inputShape;

            var tabuList = new List<int>();
            int tabuTenure = 5; // Durée pendant laquelle un mouvement est "tabou"
            int maxIterations = 500; // Sécurité pour ne pas boucler à l'infini

            for (int i = 0; i < maxIterations; i++)
            {
                if (bestCost == 0) break;

                var bestNeighbor = new List<DShapeVertex>();
                int bestNeighborCost = int.MaxValue;
                int deletedIndex = -1;

                // Générer et évaluer le voisinage (tous les polygones avec un sommet en moins)
                for (int j = 0; j < currentVertices.Count; j++)
                {
                    if (tabuList.Contains(j)) continue;

                    var neighbor = new List<DShapeVertex>(currentVertices);
                    neighbor.RemoveAt(j);

                    int neighborCost = CountIntersections(neighbor);

                    if (neighborCost < bestNeighborCost)
                    {
                        bestNeighborCost = neighborCost;
                        bestNeighbor = neighbor;
                        deletedIndex = j;
                    }
                }

                // Mettre à jour la solution courante avec le meilleur voisin trouvé
                if (bestNeighbor.Any())
                {
                    currentVertices = bestNeighbor;
                    int currentCost = bestNeighborCost;

                    // Mettre à jour la meilleure solution jamais trouvée
                    if (currentCost < bestCost)
                    {
                        bestCost = currentCost;
                        bestVertices = bestNeighbor;
                    }

                    // Mettre à jour la liste tabou
                    tabuList.Add(deletedIndex);
                    if (tabuList.Count > tabuTenure)
                    {
                        tabuList.RemoveAt(0);
                    }
                }
                else
                {
                    // Aucun voisin non-tabou trouvé, on arrête
                    break;
                }
            }

            // On retourne la meilleure solution trouvée, même si elle n'est pas parfaite
            var repairedShape = new DShape { Vertices = bestVertices };
            if (bestCost > 0)
            {
                repairedShape.Description += $" [Repair incomplete, {bestCost} intersections remain]";
            }
            return repairedShape;
        }

        private int CountIntersections(List<DShapeVertex> vertices)
        {
            int count = 0;
            if (vertices.Count < 4) return 0;

            for (int i = 0; i < vertices.Count; i++)
            {
                var p1 = vertices[i];
                var p2 = vertices[(i + 1) % vertices.Count];

                // On compare avec les arêtes non-adjacentes
                for (int j = i + 2; j < vertices.Count; j++)
                {
                    // Le dernier segment ne doit pas être comparé avec le premier (car ils sont adjacents)
                    if (i == 0 && j == vertices.Count - 1) continue;

                    var p3 = vertices[j];
                    var p4 = vertices[(j + 1) % vertices.Count];

                    if (GetLineIntersection(p1, p2, p3, p4) != null)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private DShapeVertex GetLineIntersection(DShapeVertex p1, DShapeVertex p2, DShapeVertex p3, DShapeVertex p4)
        {
            double det = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);
            if (Math.Abs(det) < 1e-9) return null;

            double t = ((p1.X - p3.X) * (p3.Y - p4.Y) - (p1.Y - p3.Y) * (p3.X - p4.X)) / det;
            double u = -((p1.X - p2.X) * (p1.Y - p3.Y) - (p1.Y - p2.Y) * (p1.X - p3.X)) / det;

            if (t > 1e-9 && t < 1 - 1e-9 && u > 1e-9 && u < 1 - 1e-9)
            {
                return new DShapeVertex { X = p1.X + t * (p2.X - p1.X), Y = p1.Y + t * (p2.Y - p1.Y) };
            }
            return null;
        }
    }
}