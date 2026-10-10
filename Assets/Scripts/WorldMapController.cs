using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldMapController : MonoBehaviour
{
    [SerializeField] private Camera mapCamera;
    [SerializeField] private Transform playerToken;
    [SerializeField] private WorldMapStageNode[] stages;
    [SerializeField] private LineRenderer stagePath;
    [SerializeField, Min(0.1f)] private float travelSpeed = 4f;

    private Button startButton;
    private GameObject stageOptionsPanel;
    private Text stageTitleText;
    private float routeProgress;
    private int targetStageIndex;

    public void ConfigureSceneReferences(Camera camera, Transform token,
        WorldMapStageNode[] stageNodes, LineRenderer path)
    {
        mapCamera = camera;
        playerToken = token;
        stages = stageNodes;
        stagePath = path;
    }

    private void Start()
    {
        if (mapCamera == null || playerToken == null || stages == null || stages.Length < 2)
        {
            Debug.LogError("World Map needs a camera, player token, and stage nodes. Open the World Map scene and use World Map > Build Editable World Map.", this);
            enabled = false;
            return;
        }

        targetStageIndex = Mathf.Clamp(PlayerPrefs.GetInt("WorldMap.SelectedStage", 0), 0, stages.Length - 1);
        routeProgress = targetStageIndex;
        RefreshPathPositions();
        CreateInterface();
        UpdatePlayerPosition();
        UpdateStageInterface();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.D)
            || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            targetStageIndex = Mathf.Min(stages.Length - 1, targetStageIndex + 1);
            UpdateStageInterface();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            targetStageIndex = Mathf.Max(0, targetStageIndex - 1);
            UpdateStageInterface();
        }

        float previousProgress = routeProgress;
        int edgeIndex = targetStageIndex >= routeProgress
            ? Mathf.FloorToInt(routeProgress)
            : Mathf.CeilToInt(routeProgress) - 1;
        edgeIndex = Mathf.Clamp(edgeIndex, 0, stages.Length - 2);
        float edgeLength = Vector3.Distance(stages[edgeIndex].transform.position,
            stages[edgeIndex + 1].transform.position);
        float routeStep = edgeLength > 0.001f ? travelSpeed * Time.deltaTime / edgeLength : 1f;
        routeProgress = Mathf.MoveTowards(routeProgress, targetStageIndex, routeStep);
        UpdatePlayerPosition();

        if (!Mathf.Approximately(previousProgress, routeProgress))
        {
            UpdateStageInterface();
        }
    }

    private void LateUpdate()
    {
        if (mapCamera == null)
        {
            return;
        }

        if (playerToken != null)
        {
            playerToken.rotation = Quaternion.LookRotation(mapCamera.transform.position - playerToken.position, Vector3.up);
        }

        if (stages != null)
        {
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] != null)
                {
                    stages[i].FaceCamera(mapCamera);
                }
            }
        }
    }

    private void CreateInterface()
    {
        GameObject canvasObject = new GameObject("World Map Interface");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("Event System");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        Text header = CreateText(canvas.transform, "World Map Heading", "WORLD MAP",
            38, TextAnchor.MiddleCenter, Color.white);
        SetRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -46f), new Vector2(520f, 60f));
        header.fontStyle = FontStyle.Bold;

        Text controls = CreateText(canvas.transform, "World Map Controls",
            "W / D or Right Arrow: next     A / S or Left Arrow: previous", 20,
            TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.92f));
        SetRect(controls.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -92f), new Vector2(760f, 38f));

        stageOptionsPanel = new GameObject("Stage Options Panel", typeof(RectTransform), typeof(Image));
        stageOptionsPanel.transform.SetParent(canvas.transform, false);
        SetRect(stageOptionsPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 134f), new Vector2(500f, 220f));
        stageOptionsPanel.GetComponent<Image>().color = new Color(0.07f, 0.12f, 0.18f, 0.92f);

        stageTitleText = CreateText(stageOptionsPanel.transform, "Selected Stage Title", "Stage",
            28, TextAnchor.MiddleCenter, Color.white);
        SetRect(stageTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -38f), new Vector2(460f, 48f));
        stageTitleText.fontStyle = FontStyle.Bold;

        startButton = CreateButton(stageOptionsPanel.transform, "Start Stage", "START STAGE",
            new Color(0.22f, 0.66f, 0.3f));
        SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(-115f, 50f), new Vector2(210f, 58f));
        startButton.onClick.AddListener(StartSelectedStage);

        Button futureButton = CreateButton(stageOptionsPanel.transform, "Future Option", "MORE OPTIONS (LATER)",
            new Color(0.35f, 0.38f, 0.42f));
        SetRect(futureButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(115f, 50f), new Vector2(230f, 58f));
        futureButton.interactable = false;
    }

    private void UpdatePlayerPosition()
    {
        int fromStage = Mathf.Clamp(Mathf.FloorToInt(routeProgress), 0, stages.Length - 1);
        int toStage = Mathf.Min(fromStage + 1, stages.Length - 1);
        float edgeProgress = routeProgress - fromStage;
        Vector3 groundPosition = Vector3.Lerp(stages[fromStage].transform.position,
            stages[toStage].transform.position, edgeProgress);
        playerToken.position = groundPosition + Vector3.up * 1.45f;

        for (int i = 0; i < stages.Length; i++)
        {
            stages[i].SetSelected(i == targetStageIndex && IsAtStage(targetStageIndex));
        }
    }

    private void RefreshPathPositions()
    {
        if (stagePath == null || stages == null)
        {
            return;
        }

        stagePath.useWorldSpace = false;
        stagePath.positionCount = stages.Length;
        for (int i = 0; i < stages.Length; i++)
        {
            stagePath.SetPosition(i, stagePath.transform.InverseTransformPoint(
                stages[i].transform.position + Vector3.up * 0.18f));
        }
    }

    private void UpdateStageInterface()
    {
        if (stageTitleText == null)
        {
            return;
        }

        bool arrived = IsAtStage(targetStageIndex);
        stageTitleText.text = "Stage " + (targetStageIndex + 1) + ": " + stages[targetStageIndex].DisplayName;
        startButton.interactable = arrived;
        startButton.GetComponentInChildren<Text>().text = "START STAGE " + (targetStageIndex + 1);
        stageOptionsPanel.SetActive(arrived);

        for (int i = 0; i < stages.Length; i++)
        {
            stages[i].SetSelected(i == targetStageIndex && arrived);
        }
    }

    private bool IsAtStage(int stageIndex)
    {
        return Mathf.Abs(routeProgress - stageIndex) < 0.001f;
    }

    private void StartSelectedStage()
    {
        if (!IsAtStage(targetStageIndex))
        {
            return;
        }

        WorldMapStageNode selectedStage = stages[targetStageIndex];
        if (string.IsNullOrEmpty(selectedStage.ScenePath))
        {
            Debug.LogError("Assign a Scene Asset to this stage in the World Map Inspector.", selectedStage);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(selectedStage.ScenePath))
        {
            Debug.LogError("The stage scene must be enabled in Build Settings: " + selectedStage.ScenePath, selectedStage);
            return;
        }

        PlayerPrefs.SetInt("WorldMap.SelectedStage", targetStageIndex);
        PlayerPrefs.Save();
        SceneTransition.LoadScene(selectedStage.ScenePath);
    }

    private static Text CreateText(Transform parent, string objectName, string content,
        int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(Transform parent, string objectName, string label, Color color)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        Text labelText = CreateText(buttonObject.transform, objectName + " Label", label,
            18, TextAnchor.MiddleCenter, Color.white);
        labelText.fontStyle = FontStyle.Bold;
        SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }
}
