using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime (incremental) dungeon builder.
/// Keeps the same data model (DungeonData: Grid/Rooms/Doors), but allows adding rooms after start.
/// Responsibility split:
/// - Domain: Room/Door are facts.
/// - Generation: this class mutates the grid and emits new Room/Door facts.
/// - Presentation: re-renders from DungeonData when requested.
/// </summary>
public sealed class DungeonRuntimeBuilder
{
    private readonly int width;
    private readonly int depth;

    private readonly Vector2Int roomMin;
    private readonly Vector2Int roomMax;

    private readonly int corridorWidth;
    private readonly int corridorLength;

    private readonly RoomTypeSpec startSpec = new StartRoomSpec();
    private readonly RoomTypeSpec normalSpec = new NormalRoomSpec();

    private CellType[,] grid;
    private readonly List<Room> rooms = new List<Room>();
    private readonly List<Door> doors = new List<Door>();

    public DungeonRuntimeBuilder(
        int width,
        int depth,
        Vector2Int roomMin,
        Vector2Int roomMax,
        int corridorWidth,
        int corridorLength)
    {
        this.width = width;
        this.depth = depth;
        this.roomMin = roomMin;
        this.roomMax = roomMax;
        this.corridorWidth = Mathf.Max(1, corridorWidth);
        this.corridorLength = Mathf.Max(1, corridorLength);
    }

    public DungeonData Initialize()
    {
        grid = new CellType[width, depth];
        Fill(CellType.Empty);

        Vector2Int startSize = SampleRoomSize(startSpec);
        int w = startSize.x;
        int d = startSize.y;

        int x = Mathf.Clamp(width / 2 - w / 2, 2, width - w - 2);
        int z = Mathf.Clamp(depth / 2 - d / 2, 2, depth - d - 2);

        BoundsInt b = new BoundsInt(x, 0, z, w, 1, d);

        Room startRoom = new Room(0, b, startSpec, null);
        rooms.Add(startRoom);
        CarveRoomFloor(b);

        return BuildData();
    }

    public DungeonData GetData()
    {
        return BuildData();
    }

    public bool TryExpandFrom(int fromRoomId, Vector2Int dir, out Room created)
    {
        created = null;

        Room from = GetRoom(fromRoomId);
        if (from == null)
            return false;

        if (!IsCardinal(dir))
            return false;

        Vector2Int size = SampleRoomSize(normalSpec);
        int w = size.x;
        int d = size.y;

        Vector2Int fromDoor = GetEdgeDoorCell(from, dir);

        // Choose new room position biased around from room center to keep layout readable.
        int roomX, roomZ;
        Vector2Int toDoorNormal = -dir;
        Vector2Int toDoor;

        if (dir == Vector2Int.right)
        {
            // New room's left edge
            int toDoorX = fromDoor.x + corridorLength + 1;
            roomX = toDoorX;
            roomZ = Mathf.Clamp(from.Center.z - d / 2, 2, depth - d - 2);
            toDoor = new Vector2Int(roomX, Mathf.Clamp(fromDoor.y, roomZ + 1, roomZ + d - 2));
        }
        else if (dir == Vector2Int.left)
        {
            // New room's right edge
            int toDoorX = fromDoor.x - corridorLength - 1;
            roomX = toDoorX - (w - 1);
            roomZ = Mathf.Clamp(from.Center.z - d / 2, 2, depth - d - 2);
            toDoor = new Vector2Int(roomX + (w - 1), Mathf.Clamp(fromDoor.y, roomZ + 1, roomZ + d - 2));
        }
        else if (dir == Vector2Int.up)
        {
            // New room's bottom edge
            int toDoorZ = fromDoor.y + corridorLength + 1;
            roomZ = toDoorZ;
            roomX = Mathf.Clamp(from.Center.x - w / 2, 2, width - w - 2);
            toDoor = new Vector2Int(Mathf.Clamp(fromDoor.x, roomX + 1, roomX + w - 2), roomZ);
        }
        else // down
        {
            // New room's top edge
            int toDoorZ = fromDoor.y - corridorLength - 1;
            roomZ = toDoorZ - (d - 1);
            roomX = Mathf.Clamp(from.Center.x - w / 2, 2, width - w - 2);
            toDoor = new Vector2Int(Mathf.Clamp(fromDoor.x, roomX + 1, roomX + w - 2), roomZ + (d - 1));
        }

        BoundsInt b = new BoundsInt(roomX, 0, roomZ, w, 1, d);

        if (!IsInsideGrid(b))
            return false;

        if (OverlapsExistingRooms(b))
            return false;

        int id = rooms.Count;
        Room room = new Room(id, b, normalSpec, from);
        rooms.Add(room);

        // Carve floors
        CarveRoomFloor(b);
        CarveLShapedCorridorWide(fromDoor, toDoor);

        // Doors (store both directions; renderers can de-dup)
        AddDoorPair(from, room, fromDoor, dir, toDoor, toDoorNormal);

        created = room;
        return true;
    }

    private DungeonData BuildData()
    {
        return new DungeonData(grid, new List<Room>(rooms), new List<Door>(doors));
    }

    private Room GetRoom(int id)
    {
        if (id < 0 || id >= rooms.Count)
            return null;
        return rooms[id];
    }

    private bool IsCardinal(Vector2Int d)
    {
        return d == Vector2Int.up || d == Vector2Int.down || d == Vector2Int.left || d == Vector2Int.right;
    }

    private Vector2Int GetEdgeDoorCell(Room room, Vector2Int dir)
    {
        if (dir == Vector2Int.right) return room.ExitRight;
        if (dir == Vector2Int.left) return room.ExitLeft;

        if (dir == Vector2Int.up)
            return new Vector2Int(room.Bounds.x + room.Bounds.size.x / 2, room.Bounds.zMax - 1);

        // down
        return new Vector2Int(room.Bounds.x + room.Bounds.size.x / 2, room.Bounds.z);
    }

    private void Fill(CellType type)
    {
        for (int z = 0; z < depth; z++)
        for (int x = 0; x < width; x++)
            grid[x, z] = type;
    }

    private Vector2Int SampleRoomSize(RoomTypeSpec spec)
    {
        int minW = Mathf.Max(roomMin.x, spec.MinSize.x);
        int minD = Mathf.Max(roomMin.y, spec.MinSize.y);

        int maxW = Mathf.Min(roomMax.x, spec.MaxSize.x);
        int maxD = Mathf.Min(roomMax.y, spec.MaxSize.y);

        if (maxW < minW) maxW = minW;
        if (maxD < minD) maxD = minD;

        int w = Random.Range(minW, maxW + 1);
        int d = Random.Range(minD, maxD + 1);

        return new Vector2Int(w, d);
    }

    private void CarveRoomFloor(BoundsInt b)
    {
        for (int z = b.z; z < b.zMax; z++)
        for (int x = b.x; x < b.xMax; x++)
            grid[x, z] = CellType.Floor;
    }

    private void CarveLShapedCorridorWide(Vector2Int a, Vector2Int b)
    {
        int xMin = Mathf.Min(a.x, b.x);
        int xMax = Mathf.Max(a.x, b.x);

        for (int x = xMin; x <= xMax; x++)
            CarveCellWide(x, a.y);

        int zMin = Mathf.Min(a.y, b.y);
        int zMax = Mathf.Max(a.y, b.y);

        for (int z = zMin; z <= zMax; z++)
            CarveCellWide(b.x, z);
    }

    private void CarveCellWide(int x, int z)
    {
        int r = corridorWidth / 2;

        for (int dz = -r; dz <= r; dz++)
        for (int dx = -r; dx <= r; dx++)
        {
            int nx = x + dx;
            int nz = z + dz;

            if (InBounds(nx, nz))
                grid[nx, nz] = CellType.Floor;
        }
    }

    private void AddDoorPair(Room a, Room b, Vector2Int aCell, Vector2Int aNormal, Vector2Int bCell, Vector2Int bNormal)
    {
        doors.Add(new Door(a.Id, b.Id, aCell, aNormal));
        doors.Add(new Door(b.Id, a.Id, bCell, bNormal));
    }

    private bool InBounds(int x, int z)
    {
        return x >= 0 && x < width && z >= 0 && z < depth;
    }

    private bool IsInsideGrid(BoundsInt b)
    {
        if (b.x < 1 || b.z < 1)
            return false;

        if (b.xMax >= width - 1 || b.zMax >= depth - 1)
            return false;

        return true;
    }

    private bool OverlapsExistingRooms(BoundsInt b)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            BoundsInt r = rooms[i].Bounds;
            BoundsInt expanded = new BoundsInt(r.x - 1, 0, r.z - 1, r.size.x + 2, 1, r.size.z + 2);
            if (IntersectsXZ(expanded, b))
                return true;
        }
        return false;
    }

    private bool IntersectsXZ(BoundsInt a, BoundsInt b)
    {
        bool xOverlap = a.xMin < b.xMax && a.xMax > b.xMin;
        bool zOverlap = a.zMin < b.zMax && a.zMax > b.zMin;
        return xOverlap && zOverlap;
    }
}
