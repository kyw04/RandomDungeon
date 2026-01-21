using UnityEngine;

public class Door
{
    public int A { get; }
    public int B { get; }

    public Vector2Int Cell { get; }
    public Vector2Int Normal { get; }

    public Door(int a, int b, Vector2Int cell, Vector2Int normal)
    {
        A = a;
        B = b;
        Cell = cell;
        Normal = normal;
    }
}
