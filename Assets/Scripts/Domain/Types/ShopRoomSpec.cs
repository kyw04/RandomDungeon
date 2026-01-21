using UnityEngine;

public sealed class ShopRoomSpec : RoomTypeSpec
{
    public override string Id => "Shop";
    public override Color DebugColor => Color.yellow;

    public override int PreferredMinDegree => 2;
    public override int PreferredMaxDegree => 2;

    public override Vector2Int MinSize => new Vector2Int(7, 7);
}
