// Models/DhemapModels.cs
using System.Collections.Generic;

namespace DGraphBuilder.Models.Dhemap
{
    public class DhemapFile { public string Format { get; set; } = "DHEMap"; public string Version { get; set; } = "1.0"; public MapInfo MapInfo { get; set; } public List<Vertex> Vertices { get; set; } = new List<Vertex>(); public List<Linedef> Linedefs { get; set; } = new List<Linedef>(); public List<Sidedef> Sidedefs { get; set; } = new List<Sidedef>(); public List<Sector> Sectors { get; set; } = new List<Sector>(); public List<Thing> Things { get; set; } = new List<Thing>(); }
    public class MapInfo { public string Game { get; set; } public int Episode { get; set; } public int Map { get; set; } public string Name { get; set; } public string SkyTexture { get; set; } public string Music { get; set; } }
    public class Vertex { public int Id { get; set; } public float X { get; set; } public float Y { get; set; } }
    public class Linedef { public int Id { get; set; } public int StartVertex { get; set; } public int EndVertex { get; set; } public List<string> Flags { get; set; } = new List<string>(); public ActionInfo Action { get; set; } public int FrontSidedef { get; set; } public int? BackSidedef { get; set; } }
    public class ActionInfo { public int Special { get; set; } public int Tag { get; set; } }
    public class Sidedef { public int Id { get; set; } public int OffsetX { get; set; } = 0; public int OffsetY { get; set; } = 0; public string TextureTop { get; set; } = "-"; public string TextureMiddle { get; set; } = "-"; public string TextureBottom { get; set; } = "-"; public int Sector { get; set; } }
    public class Sector { public int Id { get; set; } public int FloorHeight { get; set; } public int CeilingHeight { get; set; } public string FloorTexture { get; set; } public string CeilingTexture { get; set; } public int LightLevel { get; set; } public int Special { get; set; } public int Tag { get; set; } }
    public class Thing { public int Id { get; set; } public float X { get; set; } public float Y { get; set; } public int Angle { get; set; } public int Type { get; set; } public List<string> Flags { get; set; } = new List<string>(); }
}