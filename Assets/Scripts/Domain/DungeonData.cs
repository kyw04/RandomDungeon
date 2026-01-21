using System.Collections.Generic;

public class DungeonData
{
    public CellType[,] Grid { get; }
    public List<Room> Rooms { get; }
    public List<Door> Doors { get; }

    public DungeonData(CellType[,] grid, List<Room> rooms, List<Door> doors)
    {
        Grid = grid;
        Rooms = rooms;
        Doors = doors;
    }
}
