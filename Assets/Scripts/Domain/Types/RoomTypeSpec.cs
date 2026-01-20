using UnityEngine;

public abstract class RoomTypeSpec
{
    public abstract string Id { get; }
    public abstract Color DebugColor { get; }

    public virtual bool IsMainPathRequired => true;

    public virtual int PreferredMinDegree => 2;
    public virtual int PreferredMaxDegree => 2;

    public virtual Vector2Int MinSize => new Vector2Int(1, 1);
    public virtual Vector2Int MaxSize => new Vector2Int(9999, 9999);
}