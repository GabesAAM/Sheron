using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private string worldMapScenePath = "Assets/Scenes/WorldMap.unity";

    private GameObject menuOverlay;
    private bool isPaused;
    private float previousTimeScale = 1f;
    private static bool completionLocked;

    public static bool IsPaused { get; private set; }

    private void Awake()
    {
        IsPaused = completionLocked;
        completionLocked = false;
        CreateMenu();
    }

    private void Update()
    {
        if (IsPaused && !isPaused)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPaused(!isPaused);
        }
    }

    public static void LockGameplayForCompletion()
    {
        completionLocked = true;
        IsPaused = true;
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            Time.timeScale = previousTimeScale;
            IsPaused = false;
        }
    }

    private void CreateMenu()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("Event System");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObject = new GameObject("Pause Menu Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        menuOverlay = new GameObject("Pause Menu Overlay",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        menuOverlay.transform.SetParent(canvasObject.transform, false);
        RectTransform overlayRect = menuOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = menuOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.68f);
        overlayImage.raycastTarget = true;

        GameObject panelObject = new GameObject("Pause Menu Panel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(menuOverlay.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        SetCenteredRect(panelRect, Vector2.zero, new Vector2(470f, 500f));
        panelObject.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.98f);

        Text title = CreateLabel(panelObject.transform, "Pause Menu Title", "PAUSED", 38,
            FontStyle.Bold, Color.white);
        SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(400f, 60f));

        CreateMenuButton(panelObject.transform, "Resume Button", "RESUME", 92f,
            new Color(0.2f, 0.55f, 0.72f), true, ResumeGame);
        CreateMenuButton(panelObject.transform, "Restart Level Button", "RESTART LEVEL", 12f,
            new Color(0.22f, 0.66f, 0.3f), true, RestartLevel);
        CreateMenuButton(panelObject.transform, "Options Button", "OPTIONS (COMING SOON)", -68f,
            new Color(0.35f, 0.38f, 0.42f), false, null);
        CreateMenuButton(panelObject.transform, "Quit Level Button", "QUIT LEVEL", -148f,
            new Color(0.72f, 0.25f, 0.28f), true, QuitLevel);

        menuOverlay.SetActive(false);
    }

    private void CreateMenuButton(Transform parent, string objectName, string label,
        float verticalPosition, Color color, bool interactable, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        SetAnchoredRect(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, verticalPosition), new Vector2(330f, 58f));

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.interactable = interactable;
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        Text buttonText = CreateLabel(buttonObject.transform, objectName + " Label", label,
            22, FontStyle.Bold, Color.white);
        SetAnchoredRect(buttonText.rectTransform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        buttonText.rectTransform.offsetMin = new Vector2(8f, 4f);
        buttonText.rectTransform.offsetMax = new Vector2(-8f, -4f);
    }

    private static Text CreateLabel(Transform parent, string objectName, string content,
        int fontSize, FontStyle style, Color color)
    {
        GameObject labelObject = new GameObject(objectName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.text = content;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        SetAnchoredRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void SetPaused(bool paused)
    {
        if (isPaused == paused)
        {
            return;
        }

        if (paused)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }

        isPaused = paused;
        IsPaused = paused;
        menuOverlay.SetActive(paused);
        if (paused && EventSystem.current != null)
        {
            Button resumeButton = menuOverlay.transform.Find("Pause Menu Panel/Resume Button")
                .GetComponent<Button>();
            resumeButton.Select();
        }
    }

    private void ResumeGame()
    {
        SetPaused(false);
    }

    private void RestartLevel()
    {
        string activeScenePath = SceneManager.GetActiveScene().path;
        LoadScene(activeScenePath);
    }

    private void QuitLevel()
    {
        LoadScene(worldMapScenePath);
    }

    private void LoadScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError("The requested scene is not enabled in Build Settings: " + scenePath, this);
            return;
        }

        SetPaused(false);
        Time.timeScale = 1f;
        SceneTransition.LoadScene(scenePath);
    }
}
