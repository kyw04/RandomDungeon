using UnityEngine;

public sealed class StartRoomSpec : RoomTypeSpec
{
    public override string Id => "Start";
    public override Color DebugColor => Color.green;

    public override int PreferredMinDegree => 1;
    public override int PreferredMaxDegree => 1;
}
