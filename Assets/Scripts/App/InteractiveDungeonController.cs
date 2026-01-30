using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class InteractiveDungeonController : MonoBehaviour
{
    [Header("Grid")]
    public int width = 80;
    public int depth = 40;

    [Header("Room Size")]
    public Vector2Int roomMin = new Vector2Int(6, 6);
    public Vector2Int roomMax = new Vector2Int(12, 12);

    [Header("Corridor")]
    public int corridorWidth = 3;
    public int corridorLength = 6;

    [Header("Rendering")]
    public Material dungeonMaterial;
    public float wallHeight = 3f;
    public float wallThickness = 0.25f;

    [Header("Door Prefab")]
    public GameObject doorPrefab;
    public float doorPrefabBaseWidthUnits = 1f;
    public float doorYOffset = 0f;
    public float doorForwardOffset = 0f;

    [Header("UI")]
    public Font uiFont;
    
    [Header("Runtime")]
    public Transform player;

    private DungeonRuntimeBuilder builder;
    private DungeonData data;

    private int currentRoomId;

    private DungeonMeshRenderer meshRenderer;
    private DungeonDoorRenderer doorRenderer;

    private Text uiLabel;

    private void Start()
    {
        builder = new DungeonRuntimeBuilder(width, depth, roomMin, roomMax, corridorWidth, corridorLength);
        data = builder.Initialize();
        currentRoomId = 0;

        meshRenderer = gameObject.AddComponent<DungeonMeshRenderer>();

        if (doorPrefab != null)
        {
            doorRenderer = gameObject.AddComponent<DungeonDoorRenderer>();
            doorRenderer.Configure(doorPrefab, doorPrefabBaseWidthUnits, doorYOffset, doorForwardOffset);
        }

        BuildOverlayUI();
        RebuildVisuals();
        RefreshUILabel();
    }

    private void Update()
    {
        UpdateCurrentRoomFromPlayerIfAvailable();

        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame)
                CycleRoom();

            if (Keyboard.current.wKey.wasPressedThisFrame) TryExpand(Vector2Int.up);
            if (Keyboard.current.sKey.wasPressedThisFrame) TryExpand(Vector2Int.down);
            if (Keyboard.current.aKey.wasPressedThisFrame) TryExpand(Vector2Int.left);
            if (Keyboard.current.dKey.wasPressedThisFrame) TryExpand(Vector2Int.right);
        }

        RefreshUILabel();
    }

    private void TryExpand(Vector2Int dir)
    {
        if (builder == null)
            return;

        if (builder.TryExpandFrom(currentRoomId, dir, out _))
        {
            data = builder.GetData();
            RebuildVisuals();
        }
        else
        {
            Debug.Log("Expand failed: blocked by bounds or overlap.");
        }

        RefreshUILabel();
    }

    private void RebuildVisuals()
    {
        ClearChildren();

        int doorGapWidth = Mathf.Max(1, corridorWidth);
        if (doorGapWidth % 2 == 0) doorGapWidth += 1;

        meshRenderer.Render(data, dungeonMaterial, wallHeight, wallThickness, doorGapWidth);

        if (doorRenderer != null)
            doorRenderer.RenderDoors(data, doorGapWidth);
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    private void CycleRoom()
    {
        if (data == null || data.Rooms == null || data.Rooms.Count == 0)
            return;

        currentRoomId++;
        if (currentRoomId >= data.Rooms.Count)
            currentRoomId = 0;
    }

    private void UpdateCurrentRoomFromPlayerIfAvailable()
    {
        if (player == null || data == null || data.Rooms == null)
            return;

        int x = Mathf.FloorToInt(player.position.x);
        int z = Mathf.FloorToInt(player.position.z);

        for (int i = 0; i < data.Rooms.Count; i++)
        {
            if (data.Rooms[i].ContainsCell(x, z))
            {
                currentRoomId = data.Rooms[i].Id;
                return;
            }
        }
    }

    private void RefreshUILabel()
    {
        if (uiLabel == null)
            return;

        int count = (data != null && data.Rooms != null) ? data.Rooms.Count : 0;
        uiLabel.text = $"Current Room: {currentRoomId} / Rooms: {count}\nExpand: W/A/S/D   Cycle Room: Tab";
    }

    private void BuildOverlayUI()
    {
        EnsureEventSystem();

        GameObject canvasGo = new GameObject("RuntimeUI");
        canvasGo.transform.SetParent(null, false); // 씬 루트에 두는 게 가장 안전함

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvas.overrideSorting = true;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject panelGo = CreatePanel(canvasGo.transform, new Vector2(360, 240), new Vector2(20, -20));
        uiLabel = CreateLabel(panelGo.transform, new Vector2(340, 70), new Vector2(10, -10));

        CreateButton(panelGo.transform, "Up", new Vector2(120, 36), new Vector2(120, -90), () => TryExpand(Vector2Int.up));
        CreateButton(panelGo.transform, "Left", new Vector2(100, 36), new Vector2(20, -140), () => TryExpand(Vector2Int.left));
        CreateButton(panelGo.transform, "Right", new Vector2(100, 36), new Vector2(240, -140), () => TryExpand(Vector2Int.right));
        CreateButton(panelGo.transform, "Down", new Vector2(120, 36), new Vector2(120, -190), () => TryExpand(Vector2Int.down));
    }

    private void EnsureEventSystem()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es != null)
            return;

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();

        // Input System UI module
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private GameObject CreatePanel(Transform parent, Vector2 size, Vector2 topLeftOffset)
    {
        GameObject go = new GameObject("Panel");
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeftOffset;
        rt.sizeDelta = size;

        return go;
    }

    private Text CreateLabel(Transform parent, Vector2 size, Vector2 topLeftOffset)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);

        Text t = go.AddComponent<Text>();

        Font font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.font = font;

        t.fontSize = 18;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeftOffset;
        rt.sizeDelta = size;

        return t;
    }

    private void CreateButton(Transform parent, string text, Vector2 size, Vector2 topLeftOffset, System.Action onClick)
    {
        GameObject go = new GameObject($"Button_{text}");
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.85f);

        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeftOffset;
        rt.sizeDelta = size;

        GameObject labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);

        Text t = labelGo.AddComponent<Text>();

        Font font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.font = font;

        t.fontSize = 18;
        t.color = Color.black;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = text;

        RectTransform trt = labelGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }
}
