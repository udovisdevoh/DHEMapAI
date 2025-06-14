using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using DGraphBuilder.Models.DGraph;

namespace DGraphBuilder.Generation
{
    public class GridCell
    {
        public string RoomId { get; set; } = null;
        public bool IsCorridor { get; set; } = false;
    }

    public class LayoutEngine
    {
        private readonly DGraphFile _dgraph;
        private readonly Random _random;
        private const int GridSize = 50;
        private const int CellSize = 1024;

        public LayoutEngine(DGraphFile dgraph, Random random)
        {
            _dgraph = dgraph;
            _random = random;
        }

        public GridCell[,] CalculateGridLayout()
        {
            var grid = new GridCell[GridSize, GridSize];
            for (int i = 0; i < GridSize; i++) for (int j = 0; j < GridSize; j++) grid[i, j] = new GridCell();

            var rooms = _dgraph.Rooms.Where(r => r.ParentRoom == null).ToList();
            if (!rooms.Any()) return grid;

            var roomPositions = new Dictionary<string, Point>();

            // Placer la première pièce au centre
            var startRoom = rooms.First();
            var startPos = new Point(GridSize / 2, GridSize / 2);
            PlaceRoomOnGrid(grid, startRoom, startPos);
            roomPositions[startRoom.Id] = startPos;
            rooms.Remove(startRoom);

            var placedIds = new HashSet<string> { startRoom.Id };

            // Placer les autres pièces
            while (rooms.Any())
            {
                bool placed = false;
                foreach (var roomToPlace in rooms.ToList())
                {
                    var connections = _dgraph.Connections.Where(c => (c.FromRoom == roomToPlace.Id && placedIds.Contains(c.ToRoom)) ||
                                                                    (c.ToRoom == roomToPlace.Id && placedIds.Contains(c.FromRoom))).ToList();
                    if (connections.Any())
                    {
                        var anchorRoomId = connections[0].FromRoom == roomToPlace.Id ? connections[0].ToRoom : connections[0].FromRoom;
                        var anchorPos = roomPositions[anchorRoomId];

                        for (int i = 0; i < 20; i++) // 20 tentatives de placement
                        {
                            int dir = _random.Next(4);
                            int dist = _random.Next(2, 5);
                            Point targetPos = dir switch
                            {
                                0 => new Point(anchorPos.X + dist, anchorPos.Y), // Est
                                1 => new Point(anchorPos.X - dist, anchorPos.Y), // Ouest
                                2 => new Point(anchorPos.X, anchorPos.Y + dist), // Sud
                                _ => new Point(anchorPos.X, anchorPos.Y - dist), // Nord
                            };

                            if (CanPlace(grid, targetPos))
                            {
                                PlaceRoomOnGrid(grid, roomToPlace, targetPos);
                                roomPositions[roomToPlace.Id] = targetPos;
                                placedIds.Add(roomToPlace.Id);
                                rooms.Remove(roomToPlace);
                                placed = true;
                                break;
                            }
                        }
                    }
                }
                if (!placed && rooms.Any())
                {
                    // Si une pièce est isolée, la placer de force
                    var orphan = rooms.First();
                    PlaceRoomOnGrid(grid, orphan, new Point(_random.Next(GridSize), _random.Next(GridSize)));
                    roomPositions[orphan.Id] = new Point();
                    rooms.Remove(orphan);
                }
            }

            // Créer les couloirs
            CreateCorridors(grid, roomPositions);
            return grid;
        }

        private void PlaceRoomOnGrid(GridCell[,] grid, Room room, Point pos) => grid[pos.X, pos.Y].RoomId = room.Id;
        private bool CanPlace(GridCell[,] grid, Point pos) => pos.X >= 0 && pos.X < GridSize && pos.Y >= 0 && pos.Y < GridSize && grid[pos.X, pos.Y].RoomId == null;

        private void CreateCorridors(GridCell[,] grid, Dictionary<string, Point> roomPositions)
        {
            // Implémentation simplifiée : ne trace pas de chemin, suppose une connexion directe si possible.
            // Une version complète utiliserait A* ici.
            foreach (var connection in _dgraph.Connections)
            {
                if (roomPositions.TryGetValue(connection.FromRoom, out var p1) &&
                    roomPositions.TryGetValue(connection.ToRoom, out var p2))
                {
                    // Tracer un couloir en L
                    for (int x = Math.Min(p1.X, p2.X); x <= Math.Max(p1.X, p2.X); x++)
                        if (grid[x, p1.Y].RoomId == null) grid[x, p1.Y].IsCorridor = true;
                    for (int y = Math.Min(p1.Y, p2.Y); y <= Math.Max(p1.Y, p2.Y); y++)
                        if (grid[p2.X, y].RoomId == null) grid[p2.X, y].IsCorridor = true;
                }
            }
        }
    }
}