using UnityEngine;

public sealed class JunctionRoomSpec : RoomTypeSpec
{
    public override string Id => "Junction";
    public override Color DebugColor => new Color(0.85f, 0.85f, 0.85f);

    public override int PreferredMinDegree => 2;
    public override int PreferredMaxDegree => 3;
}
