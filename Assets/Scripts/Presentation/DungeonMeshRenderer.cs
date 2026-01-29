using UnityEngine;
using System.Collections.Generic;

public class DungeonMeshRenderer : MonoBehaviour
{
    private static readonly Vector2Int[] DIRS =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public void Render(DungeonData data, Material material, float wallHeight, float wallThickness, int doorGapWidth)
    {
        if (data == null || data.Grid == null)
            return;

        doorGapWidth = Mathf.Max(1, doorGapWidth);
        if (doorGapWidth % 2 == 0) doorGapWidth += 1;

        CellType[,] grid = data.Grid;
        int width = grid.GetLength(0);
        int depth = grid.GetLength(1);
List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, z] != CellType.Floor)
                    continue;

                Room room = FindRoomAt(data.Rooms, x, z);
                Color floorColor = room != null ? room.Type.DebugColor : Color.gray;

                AddFloorColored(vertices, triangles, colors, x, z, floorColor);

                foreach (Vector2Int d in DIRS)
                {                    int nx = x + d.x;
                    int nz = z + d.y;

                    if (nx < 0 || nz < 0 || nx >= width || nz >= depth ||
                        grid[nx, nz] == CellType.Empty)
                    {
                        AddWallBox(vertices, triangles, colors, x, z, d, wallHeight, wallThickness, Color.white);
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat =
            vertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        mesh.name = "DungeonMesh";

        GameObject go = new GameObject("DungeonMesh");
        go.transform.SetParent(transform, false);

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().material = material;

        MeshCollider mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = false;
    }

    // NOTE: Door openings are handled by floor carving + door prefabs.
    // WallSkipKey-based wall cutting is disabled to avoid opening outer boundary walls.
    private HashSet<WallSkipKey> BuildWallSkipSet(DungeonData data, CellType[,] grid, int doorGapWidth)
    {
        HashSet<WallSkipKey> wallSkipSet = new HashSet<WallSkipKey>(WallSkipKeyComparer.Instance);

        if (data.Doors == null)
            return wallSkipSet;

        for (int i = 0; i < data.Doors.Count; i++)
        {
            Door d = data.Doors[i];

            Vector2Int baseCell = ResolveWallBoundaryCell(grid, d.Cell, d.Normal);
            if (baseCell.x == int.MinValue)
                continue;

            AddDoorGapFiltered(wallSkipSet, grid, baseCell, d.Normal, doorGapWidth);
        }

        return wallSkipSet;
    }

    private Vector2Int ResolveWallBoundaryCell(CellType[,] grid, Vector2Int cell, Vector2Int normal)
    {
        // We want a floor cell whose neighbor in 'normal' direction is empty/outside,
        // because that's where a wall would normally be generated.
        if (IsWallBoundary(grid, cell, normal))
            return cell;

        Vector2Int back = cell - normal;
        if (IsWallBoundary(grid, back, normal))
            return back;

        // Not a valid boundary. Returning sentinel prevents accidental wall removal.
        return new Vector2Int(int.MinValue, int.MinValue);
    }

    private bool IsWallBoundary(CellType[,] grid, Vector2Int cell, Vector2Int normal)
    {
        int w = grid.GetLength(0);
        int d = grid.GetLength(1);

        if (cell.x < 0 || cell.y < 0 || cell.x >= w || cell.y >= d)
            return false;

        if (grid[cell.x, cell.y] != CellType.Floor)
            return false;

        int nx = cell.x + normal.x;
        int nz = cell.y + normal.y;

        if (nx < 0 || nz < 0 || nx >= w || nz >= d)
            return true;

        return grid[nx, nz] == CellType.Empty;
    }

    private void AddDoorGapFiltered(HashSet<WallSkipKey> wallSkipSet, CellType[,] grid, Vector2Int cell, Vector2Int normal, int gapWidth)
    {
        int r = gapWidth / 2;

        Vector2Int perp;
        if (normal == Vector2Int.left || normal == Vector2Int.right)
            perp = Vector2Int.up;
        else
            perp = Vector2Int.right;

        for (int i = -r; i <= r; i++)
        {
            Vector2Int c = cell + perp * i;
            if (!IsWallBoundary(grid, c, normal))
                continue;

            wallSkipSet.Add(new WallSkipKey(c, normal));
        }
    }

    private void AddDoorGap(HashSet<WallSkipKey> wallSkipSet, Vector2Int cell, Vector2Int normal, int gapWidth)
    {
        int r = gapWidth / 2;

        Vector2Int perp;
        if (normal == Vector2Int.left || normal == Vector2Int.right)
            perp = Vector2Int.up;
        else
            perp = Vector2Int.right;

        for (int i = -r; i <= r; i++)
        {
            Vector2Int c = cell + perp * i;
            wallSkipSet.Add(new WallSkipKey(c, normal));
        }
    }

    private Room FindRoomAt(List<Room> rooms, int x, int z)
    {
        if (rooms == null)
            return null;

        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].ContainsCell(x, z))
                return rooms[i];
        }
        return null;
    }

    private void AddFloorColored(List<Vector3> v, List<int> t, List<Color> c, int x, int z, Color color)
    {
        int i = v.Count;

        v.Add(new Vector3(x, 0, z));
        v.Add(new Vector3(x + 1, 0, z));
        v.Add(new Vector3(x + 1, 0, z + 1));
        v.Add(new Vector3(x, 0, z + 1));

        c.Add(color);
        c.Add(color);
        c.Add(color);
        c.Add(color);

        t.Add(i + 0); t.Add(i + 2); t.Add(i + 1);
        t.Add(i + 0); t.Add(i + 3); t.Add(i + 2);
    }

    private void AddWallBox(
        List<Vector3> v,
        List<int> t,
        List<Color> c,
        int x,
        int z,
        Vector2Int d,
        float height,
        float thickness,
        Color wallColor)
    {
        Vector3 dir = new Vector3(d.x, 0, d.y).normalized;
        Vector3 right = new Vector3(-dir.z, 0, dir.x);

        float half = 0.5f;
        float tHalf = thickness * 0.5f;

        float cx = x + 0.5f;
        float cz = z + 0.5f;

        Vector3 center = new Vector3(cx, 0, cz) + dir * half;

        Vector3 front = center + dir * tHalf;
        Vector3 back = center - dir * tHalf;

        Vector3 f0 = front - right * half;
        Vector3 f1 = front + right * half;
        Vector3 f2 = f1 + Vector3.up * height;
        Vector3 f3 = f0 + Vector3.up * height;

        Vector3 b0 = back - right * half;
        Vector3 b1 = back + right * half;
        Vector3 b2 = b1 + Vector3.up * height;
        Vector3 b3 = b0 + Vector3.up * height;

        AddQuad(v, t, c, f0, f1, f2, f3, wallColor);
        AddQuad(v, t, c, b1, b0, b3, b2, wallColor);
        AddQuad(v, t, c, b0, f0, f3, b3, wallColor);
        AddQuad(v, t, c, f1, b1, b2, f2, wallColor);
        AddQuad(v, t, c, f3, f2, b2, b3, wallColor);
    }

    private void AddQuad(List<Vector3> v, List<int> t, List<Color> c,
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Color color)
    {
        int i = v.Count;

        v.Add(p0);
        v.Add(p1);
        v.Add(p2);
        v.Add(p3);

        c.Add(color);
        c.Add(color);
        c.Add(color);
        c.Add(color);

        t.Add(i + 0); t.Add(i + 2); t.Add(i + 1);
        t.Add(i + 0); t.Add(i + 3); t.Add(i + 2);
    }
}
