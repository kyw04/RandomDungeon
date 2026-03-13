using UnityEngine;
using System.Collections.Generic;

namespace old
{
    public enum CellType
    {
        Wall,
        Floor,
        Door
    }public class Room
    {
        public BoundsInt bounds;

        public Vector3Int Center =>
            new Vector3Int(
                bounds.x + bounds.size.x / 2,
                bounds.y,
                bounds.z + bounds.size.z / 2
            );
    }
    

    public class DungeonGenerator3D : MonoBehaviour
    {
        [Header("Dungeon Size")]
        public int width = 60;
        public int depth = 60;

        [Header("Rooms")]
        public int roomCount = 10;
        public Vector2Int roomMin = new(6, 6);
        public Vector2Int roomMax = new(12, 12);

        [Header("Prefabs")]
        public GameObject floorPrefab;
        public GameObject wallPrefab;

        CellType[,] grid;
        List<Room> rooms = new();

        void Start()
        {
            Generate();
        }
        void Generate()
        {
            grid = new CellType[width, depth];
            Fill(CellType.Wall);

            GenerateRooms();
            ConnectRooms();
            GenerateRoomCells();
            FixConnectivity();
            InstantiateDungeon();
        }void GenerateRooms()
        {
            for (int i = 0; i < roomCount; i++)
            {
                int w = Random.Range(roomMin.x, roomMax.x);
                int d = Random.Range(roomMin.y, roomMax.y);

                int x = Random.Range(2, width - w - 2);
                int z = Random.Range(2, depth - d - 2);

                BoundsInt b = new BoundsInt(x, 0, z, w, 1, d);
                rooms.Add(new Room { bounds = b });
            }
        }void ConnectRooms()
        {
            for (int i = 1; i < rooms.Count; i++)
            {
                Vector3Int a = rooms[i - 1].Center;
                Vector3Int b = rooms[i].Center;

                if (Random.value < 0.5f)
                {
                    CarveX(a.x, b.x, a.z);
                    CarveZ(a.z, b.z, b.x);
                }
                else
                {
                    CarveZ(a.z, b.z, a.x);
                    CarveX(a.x, b.x, b.z);
                }
            }
        }

        void CarveX(int x1, int x2, int z)
        {
            for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
                grid[x, z] = CellType.Floor;
        }

        void CarveZ(int z1, int z2, int x)
        {
            for (int z = Mathf.Min(z1, z2); z <= Mathf.Max(z1, z2); z++)
                grid[x, z] = CellType.Floor;
        }CellType GetMarkov(CellType left, CellType up)
        {
            if (left == CellType.Floor && up == CellType.Floor)
                return Random.value < 0.85f ? CellType.Floor : CellType.Wall;

            if (left == CellType.Wall && up == CellType.Wall)
                return Random.value < 0.9f ? CellType.Wall : CellType.Floor;

            return Random.value < 0.6f ? CellType.Floor : CellType.Wall;
        }void GenerateRoomCells()
        {
            foreach (var r in rooms)
            {
                for (int z = r.bounds.z; z < r.bounds.zMax; z++)
                for (int x = r.bounds.x; x < r.bounds.xMax; x++)
                {
                    CellType left = x > r.bounds.x ? grid[x - 1, z] : CellType.Wall;
                    CellType up   = z > r.bounds.z ? grid[x, z - 1] : CellType.Wall;

                    grid[x, z] = GetMarkov(left, up);
                }
            }
        }void FixConnectivity()
        {
            bool[,] visited = new bool[width, depth];
            FloodFill(rooms[0].Center, visited);

            for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                if (!visited[x, z] && grid[x, z] == CellType.Floor)
                    grid[x, z] = CellType.Wall;
        }void InstantiateDungeon()
        {
            for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                Vector3 pos = new Vector3(x, 0, z);

                if (grid[x, z] == CellType.Floor)
                    Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                else
                    Instantiate(wallPrefab, pos + Vector3.up * 0.5f, Quaternion.identity, transform);
            }
        }
        void Fill(CellType type)
        {
            for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
                grid[x, z] = type;
        }static readonly Vector2Int[] dirs =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };
        void FloodFill(Vector3Int start, bool[,] visited)
        {
            Queue<Vector3Int> q = new();
            q.Enqueue(start);

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (visited[p.x, p.z]) continue;

                visited[p.x, p.z] = true;

                foreach (var d in dirs)
                {
                    Vector3Int n = new(p.x + d.x, 0, p.z + d.y);

                    if (!InBounds(n)) continue;
                    if (visited[n.x, n.z]) continue;
                    if (grid[n.x, n.z] != CellType.Floor) continue;

                    q.Enqueue(n);
                }
            }
        }bool InBounds(Vector3Int p)
        {
            return p.x >= 0 && p.x < width &&
                   p.z >= 0 && p.z < depth;
        }
        
    }
    }