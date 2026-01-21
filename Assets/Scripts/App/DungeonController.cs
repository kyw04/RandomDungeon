using UnityEngine;

public class DungeonController : MonoBehaviour
{
    public int width = 80;
    public int depth = 40;

    public int mainRoomCount = 7;
    public Vector2Int roomMin = new Vector2Int(6, 6);
    public Vector2Int roomMax = new Vector2Int(12, 12);

    public Material dungeonMaterial;
    public float wallHeight = 3f;
    public float wallThickness = 0.25f;

    public int corridorStepX = 4;
    public int corridorWidth = 3;
    public int zDrift = 6;

    public int doorGapWidth = 0;

    [Header("Doors")]
    public GameObject doorPrefab;
    public float doorPrefabBaseWidthUnits = 1f;
    public float doorYOffset = 0f;
    public float doorForwardOffset = 0f;

    private void Start()
    {
        DungeonGenerator generator = new DungeonGenerator(
            width,
            depth,
            mainRoomCount,
            roomMin,
            roomMax,
            corridorStepX,
            corridorWidth,
            zDrift
        );

        DungeonData data = generator.Generate();

        DungeonMeshRenderer renderer = gameObject.AddComponent<DungeonMeshRenderer>();

        int gap = doorGapWidth <= 0 ? corridorWidth : doorGapWidth;
        gap = Mathf.Max(1, gap);
        if (gap % 2 == 0) gap += 1;

        renderer.Render(data, dungeonMaterial, wallHeight, wallThickness, gap);

        if (doorPrefab != null)
        {
            DungeonDoorRenderer doorRenderer = gameObject.AddComponent<DungeonDoorRenderer>();
            doorRenderer.Configure(doorPrefab, doorPrefabBaseWidthUnits, doorYOffset, doorForwardOffset);
            doorRenderer.RenderDoors(data, gap);
        }
    }
}
