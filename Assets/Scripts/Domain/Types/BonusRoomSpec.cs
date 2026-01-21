using UnityEngine;

public sealed class BonusRoomSpec : RoomTypeSpec
{
    public override string Id => "Bonus";
    public override Color DebugColor => Color.cyan;

    public override bool IsMainPathRequired => false;

    public override int PreferredMinDegree => 1;
    public override int PreferredMaxDegree => 1;

    public override Vector2Int MaxSize => new Vector2Int(8, 8);
}
