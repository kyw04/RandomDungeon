using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator
{
    private readonly int width;
    private readonly int depth;

    private readonly int mainRoomCount;
    private readonly Vector2Int roomMin;
    private readonly Vector2Int roomMax;

    private readonly int corridorStepX;
    private readonly int corridorWidth;
    private readonly int zDrift;

    private CellType[,] grid;
    private List<Room> rooms;
    private List<Door> doors;

    private readonly RoomTypeSpec startSpec = new StartRoomSpec();
    private readonly RoomTypeSpec normalSpec = new NormalRoomSpec();
    private readonly RoomTypeSpec shopSpec = new ShopRoomSpec();
    private readonly RoomTypeSpec bossSpec = new BossRoomSpec();
    private readonly RoomTypeSpec bonusSpec = new BonusRoomSpec();
    private readonly RoomTypeSpec junctionSpec = new JunctionRoomSpec();

    public DungeonGenerator(
        int width,
        int depth,
        int mainRoomCount,
        Vector2Int roomMin,
        Vector2Int roomMax,
        int corridorStepX = 4,
        int corridorWidth = 3,
        int zDrift = 6)
    {
        this.width = width;
        this.depth = depth;
        this.mainRoomCount = mainRoomCount;
        this.roomMin = roomMin;
        this.roomMax = roomMax;
        this.corridorStepX = corridorStepX;

        this.corridorWidth = Mathf.Max(1, corridorWidth);
        this.zDrift = Mathf.Max(0, zDrift);
    }

    public DungeonData Generate()
    {
        grid = new CellType[width, depth];
        rooms = new List<Room>();
        doors = new List<Door>();

        Fill(CellType.Empty);

        int plannedCount = mainRoomCount;
        int shopIndex = plannedCount / 2;
        bool wantBonus = plannedCount >= 4;

        int junctionIndex = -1;
        if (wantBonus)
        {
            junctionIndex = PickJunctionIndex(plannedCount, shopIndex);
        }

        List<Room> mainPath = GenerateMainPathRooms(plannedCount, shopIndex, junctionIndex);

        CreateMainPathDoorsAndCorridors(mainPath);

        if (wantBonus && junctionIndex >= 0 && junctionIndex < mainPath.Count)
        {
            Room parent = mainPath[junctionIndex];
            Room bonus = TryGenerateBonusRoom(parent);
            if (bonus != null)
            {
                Vector2Int n = (bonus.Center.z >= parent.Center.z) ? Vector2Int.up : Vector2Int.down;

Vector2Int from = (n == Vector2Int.up)
    ? new Vector2Int(parent.Center.x, parent.Bounds.zMax - 1)
    : new Vector2Int(parent.Center.x, parent.Bounds.z);

Vector2Int to = (n == Vector2Int.up)
    ? new Vector2Int(bonus.Center.x, bonus.Bounds.z)
    : new Vector2Int(bonus.Center.x, bonus.Bounds.zMax - 1);

AddDoorPair(parent, bonus, from, n, to, -n);
CarveLShapedCorridorWide(from, to);}
        }

        return new DungeonData(grid, rooms, doors);
    }

    private int PickJunctionIndex(int plannedCount, int shopIndex)
    {
        int min = 1;
        int maxExclusive = plannedCount - 1;

        if (maxExclusive <= min)
            return -1;

        for (int tries = 0; tries < 20; tries++)
        {
            int idx = Random.Range(min, maxExclusive);
            if (idx == shopIndex) continue;
            if (idx == plannedCount - 1) continue;
            return idx;
        }

        return (shopIndex == 1) ? 2 : 1;
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

    private List<Room> GenerateMainPathRooms(int plannedCount, int shopIndex, int junctionIndex)
    {
        List<Room> mainPath = new List<Room>();
        int cursorX = 2;

        for (int i = 0; i < plannedCount; i++)
        {
            RoomTypeSpec spec;
            if (i == 0) spec = startSpec;
            else if (i == plannedCount - 1) spec = bossSpec;
            else if (i == shopIndex) spec = shopSpec;
            else if (i == junctionIndex) spec = junctionSpec;
            else spec = normalSpec;

            Vector2Int size = SampleRoomSize(spec);
            int w = size.x;
            int d = size.y;

            if (cursorX + w + 2 >= width)
                break;

            int zMinBase = 2;
            int zMaxBase = depth - d - 2;
            if (zMinBase >= zMaxBase)
                break;

            int prevZCenter = (mainPath.Count == 0) ? (depth / 2) : mainPath[mainPath.Count - 1].Center.z;
            int targetZ = prevZCenter - (d / 2);

            int zMin = Mathf.Clamp(targetZ - zDrift, zMinBase, zMaxBase);
            int zMax = Mathf.Clamp(targetZ + zDrift, zMinBase, zMaxBase);

            int z = Random.Range(zMin, zMax + 1);

            BoundsInt b = new BoundsInt(cursorX, 0, z, w, 1, d);

            int id = rooms.Count;
            Room room = new Room(id, b, spec);
            rooms.Add(room);
            mainPath.Add(room);

            CarveRoomFloor(b);

            cursorX += w + corridorStepX;
        }

        if (mainPath.Count >= 2)
        {
            EnsureLastIsBoss(mainPath);
        }

        return mainPath;
    }

    private void EnsureLastIsBoss(List<Room> mainPath)
    {
        int lastIndex = mainPath.Count - 1;
        Room last = mainPath[lastIndex];

        if (last.Type is BossRoomSpec)
            return;

        Room replaced = new Room(last.Id, last.Bounds, bossSpec);
        rooms[last.Id] = replaced;
        mainPath[lastIndex] = replaced;
    }

    private Room TryGenerateBonusRoom(Room parent)
    {
        Vector2Int size = SampleRoomSize(bonusSpec);
        int w = size.x;
        int d = size.y;

        bool placeUp = Random.value > 0.5f;

        int gap = 3;
        int offsetZ = parent.Bounds.size.z / 2 + d / 2 + gap;

        int centerX = parent.Center.x;
        int centerZ = parent.Center.z + (placeUp ? offsetZ : -offsetZ);

        BoundsInt b = new BoundsInt(centerX - w / 2, 0, centerZ - d / 2, w, 1, d);

        if (!IsInsideGrid(b))
            return null;

        if (OverlapsExistingRooms(b))
            return null;

        int id = rooms.Count;
        Room bonus = new Room(id, b, bonusSpec, parent);
        rooms.Add(bonus);

        CarveRoomFloor(b);

        return bonus;
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
    
    private void CarveRoomFloor(BoundsInt b)
    {
        for (int z = b.z; z < b.zMax; z++)
        for (int x = b.x; x < b.xMax; x++)
            grid[x, z] = CellType.Floor;
    }


    private void CreateMainPathDoorsAndCorridors(List<Room> mainPath)
    {
        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            Room a = mainPath[i];
            Room b = mainPath[i + 1];

            Vector2Int from = a.ExitRight;
            Vector2Int to = b.ExitLeft;

            AddDoorPair(a, b, from, Vector2Int.right, to, Vector2Int.left);
            CarveLShapedCorridorWide(from, to);
        }
    }

    private void AddDoorPair(Room a, Room b, Vector2Int aCell, Vector2Int aNormal, Vector2Int bCell, Vector2Int bNormal)
    {
        doors.Add(new Door(a.Id, b.Id, aCell, aNormal));
        doors.Add(new Door(b.Id, a.Id, bCell, bNormal));
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
}
