using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Numerics;

// =============================================================================
// PROGRAM ENTRY POINT
// =============================================================================
public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: dotnet run <input.dhemap> <output.wad>");
            return;
        }

        string inputFile = args[0];
        string outputFile = args[1];

        Console.WriteLine($"Reading DHEMap from: {inputFile}");
        if (!File.Exists(inputFile))
        {
            Console.WriteLine("Error: Input file not found.");
            return;
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dhemap = JsonSerializer.Deserialize<Dhemap>(File.ReadAllText(inputFile), options);

            Console.WriteLine($"Map: '{dhemap.MapInfo.Name}' for game '{dhemap.MapInfo.Game}'");
            Console.WriteLine("Starting map compilation...");

            var compiler = new MapCompiler(dhemap);
            var compiledLumps = compiler.Compile();

            Console.WriteLine("Compilation successful. Writing to WAD file...");

            WadWriter.WriteWad(outputFile, compiledLumps);

            Console.WriteLine($"Successfully created playable WAD: {outputFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}

// =============================================================================
// DHEMAP JSON DATA MODELS
// =============================================================================
#region DHEMap Models
public class Dhemap
{
    public MapInfo MapInfo { get; set; }
    public List<Vertex> Vertices { get; set; }
    public List<Linedef> Linedefs { get; set; }
    public List<Sidedef> Sidedefs { get; set; }
    public List<Sector> Sectors { get; set; }
    public List<Thing> Things { get; set; }
}
public class MapInfo
{
    public string Game { get; set; }
    public int Episode { get; set; }
    public int Map { get; set; }
    public string Name { get; set; }
}
public class Vertex
{
    public int Id { get; set; }
    public short X { get; set; }
    public short Y { get; set; }
}
public class Linedef
{
    public int Id { get; set; }
    public int StartVertex { get; set; }
    public int EndVertex { get; set; }
    public List<string> Flags { get; set; }
    public ActionInfo Action { get; set; }
    public int FrontSidedef { get; set; }
    public int? BackSidedef { get; set; }
    public List<int> HexenArgs { get; set; }
}
public class ActionInfo
{
    public int Special { get; set; }
    public int Tag { get; set; }
}
public class Sidedef
{
    public int Id { get; set; }
    public short OffsetX { get; set; }
    public short OffsetY { get; set; }
    public string TextureTop { get; set; }
    public string TextureMiddle { get; set; }
    public string TextureBottom { get; set; }
    public int Sector { get; set; }
}
public class Sector
{
    public int Id { get; set; }
    public short FloorHeight { get; set; }
    public short CeilingHeight { get; set; }
    public string FloorTexture { get; set; }
    public string CeilingTexture { get; set; }
    public short LightLevel { get; set; }
    public short Special { get; set; }
    public int Tag { get; set; }
}
public class Thing
{
    public int Id { get; set; }
    public short X { get; set; }
    public short Y { get; set; }
    public short Angle { get; set; }
    public short Type { get; set; }
    public List<string> Flags { get; set; }
    public List<int> HexenArgs { get; set; }
}
#endregion

// =============================================================================
// WAD STRUCTURE AND WRITING
// =============================================================================
#region WAD Writer
public class Lump
{
    public string Name { get; set; }
    public byte[] Data { get; set; }
}

public static class WadWriter
{
    public static void WriteWad(string filePath, List<Lump> lumps)
    {
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            // Header
            writer.Write(Encoding.ASCII.GetBytes("PWAD")); // Identification
            writer.Write(lumps.Count);                     // Num lumps
            int directoryOffset = 12;
            foreach (var lump in lumps)
            {
                directoryOffset += lump.Data.Length;
            }
            writer.Write(directoryOffset);                 // Directory offset

            // Lumps data
            foreach (var lump in lumps)
            {
                writer.Write(lump.Data);
            }

            // Directory
            int currentLumpOffset = 12;
            foreach (var lump in lumps)
            {
                writer.Write(currentLumpOffset);
                writer.Write(lump.Data.Length);
                writer.Write(Encoding.ASCII.GetBytes(lump.Name.PadRight(8, '\0').Substring(0, 8)));
                currentLumpOffset += lump.Data.Length;
            }
        }
    }
}
#endregion

// =============================================================================
// MAP COMPILER (Orchestrator)
// =============================================================================
#region Map Compiler
public class MapCompiler
{
    private readonly Dhemap _dhemap;
    private readonly Dictionary<string, short> _linedefFlagsMap = new Dictionary<string, short> {
        {"impassable", 0x0001}, {"blockMonsters", 0x0002}, {"twoSided", 0x0004},
        {"upperUnpegged", 0x0008}, {"lowerUnpegged", 0x0010}, {"secret", 0x0020},
        {"blockSound", 0x0040}, {"neverShowOnMap", 0x0080}, {"alwaysShowOnMap", 0x0100}
    };
    private readonly Dictionary<string, short> _thingFlagsMap = new Dictionary<string, short> {
        {"skillEasy", 1}, {"skillNormal", 2}, {"skillHard", 4},
        {"ambush", 8}, {"multiplayerOnly", 16}
    };

    public MapCompiler(Dhemap dhemap) { _dhemap = dhemap; }

    public List<Lump> Compile()
    {
        var lumps = new List<Lump>();
        string mapName = GetMapLumpName();

        lumps.Add(new Lump { Name = mapName, Data = new byte[0] });

        // --- Simple Lumps (Direct translation) ---
        lumps.Add(new Lump { Name = "THINGS", Data = CreateThingsLump() });
        lumps.Add(new Lump { Name = "LINEDEFS", Data = CreateLinedefsLump() });
        lumps.Add(new Lump { Name = "SIDEDEFS", Data = CreateSidedefsLump() });
        lumps.Add(new Lump { Name = "VERTEXES", Data = CreateVertexesLump() });
        lumps.Add(new Lump { Name = "SECTORS", Data = CreateSectorsLump() });

        // --- Complex Lumps (Compilation required) ---
        Console.WriteLine("  - Building BSP Tree (NODES, SSECTORS, SEGS)...");
        var bsp = new BspBuilder(_dhemap);
        var bspResult = bsp.Build();
        lumps.Add(new Lump { Name = "SEGS", Data = bspResult.SegsLump });
        lumps.Add(new Lump { Name = "SSECTORS", Data = bspResult.SsectorsLump });
        lumps.Add(new Lump { Name = "NODES", Data = bspResult.NodesLump });

        Console.WriteLine("  - Building BLOCKMAP...");
        var blockmapBuilder = new BlockmapBuilder(_dhemap);
        lumps.Add(new Lump { Name = "BLOCKMAP", Data = blockmapBuilder.Build() });

        // REJECT lump (optional, but good practice)
        lumps.Add(new Lump { Name = "REJECT", Data = new byte[0] });

        // BEHAVIOR for Hexen
        if (_dhemap.MapInfo.Game.ToLower() == "hexen")
        {
            // For simplicity, we create an empty BEHAVIOR lump.
            // A real compiler would compile ACS scripts here.
            lumps.Add(new Lump { Name = "BEHAVIOR", Data = new byte[0] });
        }

        return lumps;
    }

    private string GetMapLumpName()
    {
        var game = _dhemap.MapInfo.Game.ToLower();
        if (game == "doom" || game == "heretic")
            return $"E{_dhemap.MapInfo.Episode}M{_dhemap.MapInfo.Map}";
        return $"MAP{_dhemap.MapInfo.Map:D2}";
    }

    private byte[] CreateThingsLump()
    {
        bool isHexen = _dhemap.MapInfo.Game.ToLower() == "hexen";
        int thingSize = isHexen ? 20 : 10;
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            foreach (var thing in _dhemap.Things)
            {
                short flags = thing.Flags.Aggregate<string, short>(0, (current, flag) => (short)(current | _thingFlagsMap.GetValueOrDefault<string, short>(flag.ToLower(), 0)));
                if (isHexen)
                {
                    writer.Write((short)thing.Id);
                    writer.Write(thing.X); writer.Write(thing.Y); writer.Write((short)0); // Z
                    writer.Write(thing.Angle); writer.Write(thing.Type); writer.Write(flags);
                    writer.Write((byte)0); // Special
                    for (int i = 0; i < 5; i++) writer.Write((byte)(thing.HexenArgs != null && i < thing.HexenArgs.Count ? thing.HexenArgs[i] : 0));
                }
                else
                {
                    writer.Write(thing.X); writer.Write(thing.Y); writer.Write(thing.Angle);
                    writer.Write(thing.Type); writer.Write(flags);
                }
            }
        }
        return ms.ToArray();
    }

    // Implement CreateLinedefsLump, CreateSidedefsLump, etc.
    // (These are mostly direct binary translations from the JSON model)
    private byte[] CreateLinedefsLump()
    {
        bool isHexen = _dhemap.MapInfo.Game.ToLower() == "hexen";
        int linedefSize = isHexen ? 16 : 14;
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            foreach (var line in _dhemap.Linedefs)
            {
                short flags = line.Flags.Aggregate<string, short>(0, (current, flag) => (short)(current | _linedefFlagsMap.GetValueOrDefault<string, short>(flag.ToLower(), 0)));
                writer.Write((short)line.StartVertex);
                writer.Write((short)line.EndVertex);
                writer.Write(flags);
                if (isHexen)
                {
                    writer.Write((byte)(line.Action?.Special ?? 0));
                    for (int i = 0; i < 5; i++) writer.Write((byte)(line.HexenArgs != null && i < line.HexenArgs.Count ? line.HexenArgs[i] : 0));
                }
                else
                {
                    writer.Write((short)(line.Action?.Special ?? 0));
                    writer.Write((short)(line.Action?.Tag ?? 0));
                }
                writer.Write((short)line.FrontSidedef);
                writer.Write(line.BackSidedef.HasValue ? (short)line.BackSidedef.Value : (short)-1);
            }
        }
        return ms.ToArray();
    }
    private byte[] CreateSidedefsLump()
    {
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            foreach (var side in _dhemap.Sidedefs)
            {
                writer.Write(side.OffsetX);
                writer.Write(side.OffsetY);
                writer.Write(Encoding.ASCII.GetBytes(side.TextureTop.ToUpper().PadRight(8, '\0').Substring(0, 8)));
                writer.Write(Encoding.ASCII.GetBytes(side.TextureBottom.ToUpper().PadRight(8, '\0').Substring(0, 8)));
                writer.Write(Encoding.ASCII.GetBytes(side.TextureMiddle.ToUpper().PadRight(8, '\0').Substring(0, 8)));
                writer.Write((short)side.Sector);
            }
        }
        return ms.ToArray();
    }
    private byte[] CreateVertexesLump()
    {
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            foreach (var vert in _dhemap.Vertices)
            {
                writer.Write(vert.X);
                writer.Write(vert.Y);
            }
        }
        return ms.ToArray();
    }
    private byte[] CreateSectorsLump()
    {
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            foreach (var sec in _dhemap.Sectors)
            {
                writer.Write(sec.FloorHeight);
                writer.Write(sec.CeilingHeight);
                writer.Write(Encoding.ASCII.GetBytes(sec.FloorTexture.ToUpper().PadRight(8, '\0').Substring(0, 8)));
                writer.Write(Encoding.ASCII.GetBytes(sec.CeilingTexture.ToUpper().PadRight(8, '\0').Substring(0, 8)));
                writer.Write(sec.LightLevel);
                writer.Write(sec.Special);
                writer.Write((short)sec.Tag);
            }
        }
        return ms.ToArray();
    }
}
#endregion

// =============================================================================
// BSP (Binary Space Partitioning) BUILDER
// =============================================================================
#region BSP Builder
public class BspBuilder
{
    // Internal classes for BSP construction
    private class BspNode
    {
        public Vector2 PartitionOrigin;
        public Vector2 PartitionDelta;
        public BoundingBox RightBox, LeftBox;
        public int RightChild, LeftChild;
    }
    private class Seg
    {
        public short StartVertexId, EndVertexId;
        public short Angle;
        public short LinedefId;
        public short Direction; // 0 = same as linedef, 1 = opposite
        public short Offset;
    }
    private class SubSector
    {
        public short SegCount;
        public short FirstSegId;
    }
    private class BoundingBox
    {
        public short Top, Bottom, Left, Right;
    }

    private readonly Dhemap _dhemap;
    private readonly List<Vertex> _vertices;
    private readonly List<Linedef> _linedefs;
    private readonly List<BspNode> _nodes = new List<BspNode>();
    private readonly List<Seg> _segs = new List<Seg>();
    private readonly List<SubSector> _subSectors = new List<SubSector>();

    public BspBuilder(Dhemap dhemap)
    {
        _dhemap = dhemap;
        _vertices = dhemap.Vertices;
        _linedefs = dhemap.Linedefs;
    }

    public (byte[] NodesLump, byte[] SsectorsLump, byte[] SegsLump) Build()
    {
        BuildBsp(Enumerable.Range(0, _linedefs.Count).ToList());
        return (WriteNodesLump(), WriteSsectorsLump(), WriteSegsLump());
    }

    private int BuildBsp(List<int> lineIndices)
    {
        if (lineIndices.Count == 0) return -1;

        // Simplified splitter selection: pick the first one
        int splitterIndex = lineIndices[0];
        var splitterLine = _linedefs[splitterIndex];

        var frontList = new List<int>();
        var backList = new List<int>();

        for (int i = 1; i < lineIndices.Count; i++)
        {
            int lineIndex = lineIndices[i];
            var line = _linedefs[lineIndex];
            ClassifyLine(line, splitterLine, out bool isFront, out bool isBack);
            if (isFront) frontList.Add(lineIndex);
            if (isBack) backList.Add(lineIndex);
        }

        int frontChild = BuildBsp(frontList);
        int backChild = BuildBsp(backList);

        // This is a naive way to build subsectors. A real BSP would detect convex polygons.
        // Here we just create one ssector per leaf.
        if (frontChild == -1) frontChild = CreateSubSector(splitterIndex, 0) | 0x8000;
        if (backChild == -1) backChild = CreateSubSector(splitterIndex, 1) | 0x8000;

        var node = new BspNode();
        var v1 = _vertices[splitterLine.StartVertex];
        var v2 = _vertices[splitterLine.EndVertex];
        node.PartitionOrigin = new Vector2(v1.X, v1.Y);
        node.PartitionDelta = new Vector2(v2.X - v1.X, v2.Y - v1.Y);
        node.LeftChild = frontChild; // Doom's 'left' is our 'front'
        node.RightChild = backChild;

        // Bounding boxes would be calculated here. For simplicity, we leave them zero.
        node.LeftBox = new BoundingBox();
        node.RightBox = new BoundingBox();

        _nodes.Add(node);
        return _nodes.Count - 1;
    }

    private int CreateSubSector(int linedefId, int side)
    {
        var line = _linedefs[linedefId];
        var v1 = _vertices[line.StartVertex];
        var v2 = _vertices[line.EndVertex];

        var seg = new Seg
        {
            LinedefId = (short)line.Id,
            Direction = (short)side,
            StartVertexId = (short)(side == 0 ? line.StartVertex : line.EndVertex),
            EndVertexId = (short)(side == 0 ? line.EndVertex : line.StartVertex),
        };
        // Angle and Offset calculation would go here.

        var ssector = new SubSector
        {
            SegCount = 1,
            FirstSegId = (short)_segs.Count
        };
        _segs.Add(seg);
        _subSectors.Add(ssector);
        return _subSectors.Count - 1;
    }

    // Naive classification. Doesn't handle splitting lines.
    private void ClassifyLine(Linedef line, Linedef splitter, out bool isFront, out bool isBack)
    {
        var splitterV1 = _vertices[splitter.StartVertex];
        var splitterV2 = _vertices[splitter.EndVertex];
        var lineV1 = _vertices[line.StartVertex];
        var lineV2 = _vertices[line.EndVertex];

        var side1 = Math.Sign((splitterV2.X - splitterV1.X) * (lineV1.Y - splitterV1.Y) - (splitterV2.Y - splitterV1.Y) * (lineV1.X - splitterV1.X));
        var side2 = Math.Sign((splitterV2.X - splitterV1.X) * (lineV2.Y - splitterV1.Y) - (splitterV2.Y - splitterV1.Y) * (lineV2.X - splitterV1.X));

        isFront = side1 >= 0 || side2 >= 0;
        isBack = side1 <= 0 || side2 <= 0;
    }

    private byte[] WriteNodesLump()
    {
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            foreach (var node in _nodes)
            {
                writer.Write(node.PartitionOrigin.X); writer.Write(node.PartitionOrigin.Y);
                writer.Write(node.PartitionDelta.X); writer.Write(node.PartitionDelta.Y);
                writer.Write(node.RightBox.Top); writer.Write(node.RightBox.Bottom); writer.Write(node.RightBox.Left); writer.Write(node.RightBox.Right);
                writer.Write(node.LeftBox.Top); writer.Write(node.LeftBox.Bottom); writer.Write(node.LeftBox.Left); writer.Write(node.LeftBox.Right);
                writer.Write((ushort)node.RightChild); writer.Write((ushort)node.LeftChild);
            }
        }
        return ms.ToArray();
    }
    private byte[] WriteSsectorsLump()
    {
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            foreach (var ssec in _subSectors)
            {
                writer.Write(ssec.SegCount); writer.Write(ssec.FirstSegId);
            }
        }
        return ms.ToArray();
    }
    private byte[] WriteSegsLump()
    {
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            foreach (var seg in _segs)
            {
                writer.Write(seg.StartVertexId); writer.Write(seg.EndVertexId);
                writer.Write(seg.Angle); writer.Write(seg.LinedefId);
                writer.Write(seg.Direction); writer.Write(seg.Offset);
            }
        }
        return ms.ToArray();
    }
}
#endregion

// =============================================================================
// BLOCKMAP BUILDER
// =============================================================================
#region Blockmap Builder
public class BlockmapBuilder
{
    private readonly Dhemap _dhemap;

    public BlockmapBuilder(Dhemap dhemap) { _dhemap = dhemap; }

    public byte[] Build()
    {
        // This is a simplified implementation that creates a valid but empty BLOCKMAP.
        // A real implementation would grid the map and list linedefs in each block.
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            // Header
            writer.Write((short)0); // Grid Origin X
            writer.Write((short)0); // Grid Origin Y
            writer.Write((short)0); // Number of columns
            writer.Write((short)0); // Number of rows
            // No offsets, since there are no blocks
        }
        return ms.ToArray();
    }
}
#endregion