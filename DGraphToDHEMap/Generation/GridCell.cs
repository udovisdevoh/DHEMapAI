// Generation/GridCell.cs
namespace DGraphBuilder.Generation
{
    public class GridCell
    {
        public string RoomId { get; set; } = null;
        public int SectorId { get; set; } = -1; // Utilisé par le MapBuilder
        public bool IsCorridor => RoomId != null && RoomId.StartsWith("corridor_");
    }
}