namespace DGenesis.Models.Geometry
{
    // Utilisons le DPolyVertex existant pour représenter un point.
    using DGenesis.Models.DPolyGraph;

    public class Line
    {
        public DPolyVertex Point1 { get; set; }
        public DPolyVertex Point2 { get; set; }
    }

    public class BoundingBox
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
    }
}