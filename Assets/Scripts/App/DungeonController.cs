using UnityEngine;

public class DungeonController : MonoBehaviour
{
    public int width = 160;
    public int depth = 160;

    public int mainRoomCount = 7;
    public Vector2Int roomMin = new Vector2Int(12, 12);
    public Vector2Int roomMax = new Vector2Int(20, 20);

    public Material dungeonMaterial;
    public float wallHeight = 5f;
    public float wallThickness = 0.25f;

    public int corridorStepX = 8;
    
    public int corridorWidth = 3;
    public int zDrift = 12;

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
        renderer.Render(data, dungeonMaterial, wallHeight, wallThickness);
    }
}
