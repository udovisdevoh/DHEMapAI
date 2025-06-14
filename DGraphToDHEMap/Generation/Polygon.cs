using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DGraphBuilder.Generation
{
    public class Polygon
    {
        public List<PointF> Vertices { get; private set; }

        public Polygon(List<PointF> vertices)
        {
            Vertices = vertices;
        }

        public PointF GetCenter()
        {
            if (Vertices == null || Vertices.Count == 0)
                return PointF.Empty;

            float avgX = 0;
            float avgY = 0;
            foreach (var v in Vertices)
            {
                avgX += v.X;
                avgY += v.Y;
            }
            return new PointF(avgX / Vertices.Count, avgY / Vertices.Count);
        }

        public void Translate(float dx, float dy)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i] = new PointF(Vertices[i].X + dx, Vertices[i].Y + dy);
            }
        }
    }
}