using UnityEngine;

public sealed class BossRoomSpec : RoomTypeSpec
{
    public override string Id => "Boss";
    public override Color DebugColor => Color.red;

    public override int PreferredMinDegree => 1;
    public override int PreferredMaxDegree => 1;

    public override Vector2Int MinSize => new Vector2Int(10, 10);
}
