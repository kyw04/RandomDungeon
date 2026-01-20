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

    private CellType[,] grid;
    private List<Room> rooms;
    private List<Door> doors;

    private readonly RoomTypeSpec startSpec = new StartRoomSpec();
    private readonly RoomTypeSpec normalSpec = new NormalRoomSpec();
    private readonly RoomTypeSpec shopSpec = new ShopRoomSpec();
    private readonly RoomTypeSpec bossSpec = new BossRoomSpec();
    private readonly RoomTypeSpec bonusSpec = new BonusRoomSpec();
    private readonly RoomTypeSpec junctionSpec = new JunctionRoomSpec();
    private readonly int corridorWidth;
    private readonly int zDrift;

    private Dictionary<int, int> degree;

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
        degree = new Dictionary<int, int>();

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

        CreateMainPathDoors(mainPath);

        Room bonus = null;
        if (wantBonus && junctionIndex != -1 && junctionIndex < mainPath.Count)
        {
            Room parent = mainPath[junctionIndex];
            bonus = TryGenerateBonusRoom(parent);
            if (bonus != null)
            {
                TryAddDoor(parent, bonus, new Vector2Int(parent.Center.x, parent.Center.z),
                    BonusNormalFrom(parent, bonus));
                CarveLShapedCorridor(
                    new Vector2Int(parent.Center.x, parent.Center.z),
                    new Vector2Int(bonus.Center.x, bonus.Center.z)
                );
            }
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
            
            int prevZCenter = (mainPath.Count == 0) ? (depth / 2) : mainPath[mainPath.Count - 1].Center.z;

            int zMinBase = 2;
            int zMaxBase = depth - d - 2;
            if (zMinBase >= zMaxBase)
                break;

            int drift = zDrift;
            int targetZ = prevZCenter - (d / 2);

            int zMin = Mathf.Clamp(targetZ - drift, zMinBase, zMaxBase);
            int zMax = Mathf.Clamp(targetZ + drift, zMinBase, zMaxBase);

            int z = Random.Range(zMin, zMax + 1);

            BoundsInt b = new BoundsInt(cursorX, 0, z, w, 1, d);

            int id = rooms.Count;
            Room room = new Room(id, b, spec);
            rooms.Add(room);
            mainPath.Add(room);

            degree[id] = 0;

            CarveRoomFloor(b);

            cursorX += w + corridorStepX;
        }

        if (mainPath.Count >= 2)
        {
            EnsureLastIsBoss(mainPath);
        }

        return mainPath;
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

    private void CarveRoomFloor(BoundsInt b)
    {
        for (int z = b.z; z < b.zMax; z++)
        for (int x = b.x; x < b.xMax; x++)
            grid[x, z] = CellType.Floor;
    }

    private void CreateMainPathDoors(List<Room> mainPath)
    {
        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            Room a = mainPath[i];
            Room b = mainPath[i + 1];

            Vector2Int from = a.ExitRight;
            Vector2Int to = b.ExitLeft;

            TryAddDoor(a, b, from, Vector2Int.right);
            TryAddDoor(b, a, to, Vector2Int.left);

            CarveLShapedCorridor(from, to);
        }
    }

    private Room TryGenerateBonusRoom(Room parent)
    {
        int w = Random.Range(roomMin.x, roomMax.x + 1);
        int d = Random.Range(roomMin.y, roomMax.y + 1);

        bool placeUp = Random.value > 0.5f;

        int gap = 3;
        int offsetZ = parent.Bounds.size.z / 2 + d / 2 + gap;

        int centerX = parent.Center.x;
        int centerZ = parent.Center.z + (placeUp ? offsetZ : -offsetZ);

        BoundsInt b = new BoundsInt(centerX - w / 2, 0, centerZ - d / 2, w, 1, d);

        if (!IsInsideGrid(b))
            return null;

        int id = rooms.Count;
        Room bonus = new Room(id, b, bonusSpec, parent);
        rooms.Add(bonus);
        degree[id] = 0;

        CarveRoomFloor(b);

        return bonus;
    }

    private Vector2Int BonusNormalFrom(Room parent, Room bonus)
    {
        int dz = bonus.Center.z - parent.Center.z;
        if (dz >= 0) return Vector2Int.up;
        return Vector2Int.down;
    }

    private bool TryAddDoor(Room from, Room to, Vector2Int cell, Vector2Int normal)
    {
        if (!CanAddConnection(from))
            return false;

        if (!CanAddConnection(to))
            return false;

        if (IsAlreadyConnected(from.Id, to.Id))
            return false;

        doors.Add(new Door(from.Id, to.Id, cell, normal));

        degree[from.Id] = degree[from.Id] + 1;
        degree[to.Id] = degree[to.Id] + 1;

        return true;
    }

    private bool IsAlreadyConnected(int a, int b)
    {
        for (int i = 0; i < doors.Count; i++)
        {
            Door d = doors[i];
            if ((d.A == a && d.B == b) || (d.A == b && d.B == a))
                return true;
        }
        return false;
    }

    private bool CanAddConnection(Room room)
    {
        int cur = degree.ContainsKey(room.Id) ? degree[room.Id] : 0;
        return cur < room.Type.PreferredMaxDegree;
    }

    private void CarveLShapedCorridor(Vector2Int a, Vector2Int b)
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
