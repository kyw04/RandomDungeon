using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Instantiates Door prefabs at the same wall-boundary locations used by DungeonMeshRenderer's wall-skip logic.
/// Assumes the door prefab faces +Z (local forward) and its width is along local +X.
/// </summary>
public sealed class DungeonDoorRenderer : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject doorPrefab;

    [Tooltip("Prefab width in world units when localScale == 1. Used to scale to doorGapWidth.")]
    [SerializeField] private float prefabBaseWidthUnits = 1f;

    [Header("Placement Offsets")]
    [SerializeField] private float yOffset = 0f;

    [Tooltip("Offset along the door's forward (after rotation). Useful when the prefab pivot isn't centered in the wall thickness.")]
    [SerializeField] private float forwardOffset = 0f;

    public void Configure(GameObject prefab, float baseWidthUnits, float yOffset, float forwardOffset)
    {
        doorPrefab = prefab;
        prefabBaseWidthUnits = baseWidthUnits;
        this.yOffset = yOffset;
        this.forwardOffset = forwardOffset;
    }

    public void RenderDoors(DungeonData data, int doorGapWidth)
    {
        if (doorPrefab == null)
        {
            Debug.LogWarning($"{nameof(DungeonDoorRenderer)}: doorPrefab is null.");
            return;
        }

        if (data == null || data.Grid == null || data.Doors == null)
            return;

        doorGapWidth = Mathf.Max(1, doorGapWidth);
        if (doorGapWidth % 2 == 0) doorGapWidth += 1;

        Transform parent = new GameObject("DungeonDoors").transform;
        parent.SetParent(transform, false);

        // De-dup by resolved (boundaryCell + normal)
        HashSet<WallSkipKey> placed = new HashSet<WallSkipKey>(WallSkipKeyComparer.Instance);

        CellType[,] grid = data.Grid;

        for (int i = 0; i < data.Doors.Count; i++)
        {
            Door door = data.Doors[i];

            Vector2Int baseCell = ResolveWallBoundaryCell(grid, door.Cell, door.Normal);
            if (baseCell.x == int.MinValue)
                continue;

            WallSkipKey key = new WallSkipKey(baseCell, door.Normal);
            if (!placed.Add(key))
                continue;

            Vector3 n3 = new Vector3(door.Normal.x, 0f, door.Normal.y);
            Vector3 cellCenter = new Vector3(baseCell.x + 0.5f, 0f, baseCell.y + 0.5f);

            // Same plane as AddWallBox: center + normal * 0.5
            Vector3 pos = cellCenter + n3 * 0.5f;

            Quaternion rot = RotationFromNormal(door.Normal);

            GameObject go = Instantiate(doorPrefab, parent);
            go.name = $"Door_{baseCell.x}_{baseCell.y}_{door.Normal.x}_{door.Normal.y}";

            Vector3 forward = rot * Vector3.forward;
            go.transform.SetPositionAndRotation(pos + Vector3.up * yOffset + forward * forwardOffset, rot);

            float baseW = Mathf.Max(0.0001f, prefabBaseWidthUnits);
            float scaleX = doorGapWidth / baseW;

            Vector3 s = go.transform.localScale;
            go.transform.localScale = new Vector3(s.x * scaleX, s.y, s.z);
        }
    }

    private static Quaternion RotationFromNormal(Vector2Int normal)
    {
        if (normal == Vector2Int.up) return Quaternion.Euler(0f, 0f, 0f);
        if (normal == Vector2Int.down) return Quaternion.Euler(0f, 180f, 0f);
        if (normal == Vector2Int.right) return Quaternion.Euler(0f, 90f, 0f);
        if (normal == Vector2Int.left) return Quaternion.Euler(0f, -90f, 0f);

        Vector3 f = new Vector3(normal.x, 0f, normal.y);
        if (f.sqrMagnitude < 0.0001f) f = Vector3.forward;
        return Quaternion.LookRotation(f.normalized, Vector3.up);
    }

    private static Vector2Int ResolveWallBoundaryCell(CellType[,] grid, Vector2Int cell, Vector2Int normal)
    {
        if (IsWallBoundary(grid, cell, normal))
            return cell;

        Vector2Int back = cell - normal;
        if (IsWallBoundary(grid, back, normal))
            return back;

        return new Vector2Int(int.MinValue, int.MinValue);
    }

    private static bool IsWallBoundary(CellType[,] grid, Vector2Int cell, Vector2Int normal)
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
}
