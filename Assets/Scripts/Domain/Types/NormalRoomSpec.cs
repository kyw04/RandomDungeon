using UnityEngine;

public sealed class NormalRoomSpec : RoomTypeSpec
{
    public override string Id => "Normal";
    public override Color DebugColor => Color.gray;

    public override int PreferredMinDegree => 2;
    public override int PreferredMaxDegree => 2;
}
