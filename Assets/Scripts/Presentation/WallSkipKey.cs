using System.Collections.Generic;
using UnityEngine;

public readonly struct WallSkipKey
{
    public readonly Vector2Int Cell;
    public readonly Vector2Int Dir;

    public WallSkipKey(Vector2Int cell, Vector2Int dir)
    {
        Cell = cell;
        Dir = dir;
    }
}

public sealed class WallSkipKeyComparer : IEqualityComparer<WallSkipKey>
{
    public static readonly WallSkipKeyComparer Instance = new WallSkipKeyComparer();

    private WallSkipKeyComparer() { }

    public bool Equals(WallSkipKey x, WallSkipKey y)
    {
        return x.Cell == y.Cell && x.Dir == y.Dir;
    }

    public int GetHashCode(WallSkipKey obj)
    {
        unchecked
        {
            int h1 = obj.Cell.GetHashCode();
            int h2 = obj.Dir.GetHashCode();
            return (h1 * 397) ^ h2;
        }
    }
}
