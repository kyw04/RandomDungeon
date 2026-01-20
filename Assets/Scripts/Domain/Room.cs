using UnityEngine;

public class Room
{
    public int Id { get; }
    public BoundsInt Bounds { get; }
    public RoomTypeSpec Type { get; }
    public Room Parent { get; }

    public Room(int id, BoundsInt bounds, RoomTypeSpec type, Room parent = null)
    {
        Id = id;
        Bounds = bounds;
        Type = type;
        Parent = parent;
    }

    public Vector3Int Center
    {
        get
        {
            return new Vector3Int(
                Bounds.x + Bounds.size.x / 2,
                0,
                Bounds.z + Bounds.size.z / 2
            );
        }
    }

    public Vector2Int ExitLeft
    {
        get { return new Vector2Int(Bounds.x, Bounds.z + Bounds.size.z / 2); }
    }

    public Vector2Int ExitRight
    {
        get { return new Vector2Int(Bounds.xMax - 1, Bounds.z + Bounds.size.z / 2); }
    }

    public bool ContainsCell(int x, int z)
    {
        return x >= Bounds.x && x < Bounds.xMax && z >= Bounds.z && z < Bounds.zMax;
    }
}
