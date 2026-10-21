using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SampleSceneUI : MonoBehaviour
{
    private const string WorldMapScenePath = "Assets/Scenes/WorldMap.unity";
    private const int StagesPerWorld = 5;

    [SerializeField] private Vector2 screenMargin = new Vector2(28f, 28f);
    [SerializeField] private Vector2 panelSize = new Vector2(230f, 128f);
    [SerializeField, Min(12)] private int timerFontSize = 34;
    [SerializeField, Min(8)] private int titleFontSize = 15;
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField, Min(0)] private int remainingBoxCount;

    private Text timerText;
    private Text boxCountText;
    private Text healthHeartsText;
    private Text completionLevelText;
    private Text completionTimeText;
    private GameObject congratulationsOverlay;
    private PlayerHealth playerHealth;
    private float elapsedTime;
    private int lastDisplayedSecond = -1;
    private bool timerRunning = true;
    private bool completionRecorded;

    public int RemainingBoxCount { get { return remainingBoxCount; } }

    private void Awake()
    {
        GridBoxSnapToGrid[] gridBoxes = FindObjectsOfType<GridBoxSnapToGrid>();
        remainingBoxCount = 0;
        for (int i = 0; i < gridBoxes.Length; i++)
        {
            if (gridBoxes[i] != null && gridBoxes[i].ParticipatesInMatchMechanic)
            {
                remainingBoxCount++;
            }
        }
        GridBoxSnapToGrid.MatchedBoxesRemoved += OnMatchedBoxesRemoved;
        CreateClockUi();
        playerHealth = FindObjectOfType<PlayerHealth>();
        UpdateClockDisplay();
        UpdateBoxCountDisplay();

        if (remainingBoxCount == 0)
        {
            ShowCongratulations();
        }
    }

    private void Update()
    {
        if (!timerRunning)
        {
            return;
        }

        elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        int currentSecond = Mathf.FloorToInt(elapsedTime);
        if (currentSecond != lastDisplayedSecond)
        {
            UpdateClockDisplay();
        }
    }

    private void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += OnPlayerHealthChanged;
            UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        GridBoxSnapToGrid.MatchedBoxesRemoved -= OnMatchedBoxesRemoved;
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= OnPlayerHealthChanged;
        }
    }

    private void CreateClockUi()
    {
        GameObject canvasObject = new GameObject("Gameplay Clock Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject("Clock Panel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.anchoredPosition = new Vector2(-screenMargin.x, -screenMargin.y);
        panelRect.sizeDelta = panelSize;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.035f, 0.055f, 0.09f, 0.88f);
        panelImage.raycastTarget = false;

        boxCountText = CreateLabel(panelObject.transform, "Remaining Boxes", "BOXES: 0", 22,
            FontStyle.Bold, Color.white);
        RectTransform boxCountRect = boxCountText.rectTransform;
        boxCountRect.anchorMin = new Vector2(0f, 1f);
        boxCountRect.anchorMax = new Vector2(1f, 1f);
        boxCountRect.pivot = new Vector2(0.5f, 1f);
        boxCountRect.anchoredPosition = new Vector2(0f, -7f);
        boxCountRect.sizeDelta = new Vector2(-18f, 32f);

        Text title = CreateLabel(panelObject.transform, "Clock Title", "TIME", titleFontSize,
            FontStyle.Bold, new Color(0.53f, 0.82f, 1f));
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -42f);
        titleRect.sizeDelta = new Vector2(-20f, 22f);

        timerText = CreateLabel(panelObject.transform, "Clock Value", "00:00", timerFontSize,
            FontStyle.Bold, Color.white);
        RectTransform timerRect = timerText.rectTransform;
        timerRect.anchorMin = new Vector2(0f, 0f);
        timerRect.anchorMax = new Vector2(1f, 0f);
        timerRect.pivot = new Vector2(0.5f, 0f);
        timerRect.anchoredPosition = new Vector2(0f, 7f);
        timerRect.sizeDelta = new Vector2(-16f, 46f);

        CreateHealthPanel(canvasObject.transform);
        CreateCongratulationsOverlay(canvasObject.transform);
    }

    private void CreateHealthPanel(Transform canvasTransform)
    {
        GameObject panelObject = new GameObject("Health Panel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasTransform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(screenMargin.x, -screenMargin.y);
        panelRect.sizeDelta = new Vector2(190f, 78f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.035f, 0.055f, 0.09f, 0.88f);
        panelImage.raycastTarget = false;

        Text title = CreateLabel(panelObject.transform, "Health Title", "HEALTH", titleFontSize,
            FontStyle.Bold, new Color(1f, 0.58f, 0.64f));
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -5f);
        titleRect.sizeDelta = new Vector2(-12f, 22f);

        healthHeartsText = CreateLabel(panelObject.transform, "Health Hearts", string.Empty, 38,
            FontStyle.Bold, Color.white);
        healthHeartsText.supportRichText = true;
        RectTransform heartsRect = healthHeartsText.rectTransform;
        heartsRect.anchorMin = new Vector2(0f, 0f);
        heartsRect.anchorMax = new Vector2(1f, 1f);
        heartsRect.offsetMin = new Vector2(6f, 3f);
        heartsRect.offsetMax = new Vector2(-6f, -24f);
    }

    private void CreateCongratulationsOverlay(Transform canvasTransform)
    {
        congratulationsOverlay = new GameObject("Congratulations Overlay",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        congratulationsOverlay.transform.SetParent(canvasTransform, false);
        RectTransform overlayRect = congratulationsOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = congratulationsOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.48f);
        overlayImage.raycastTarget = false;

        GameObject messagePanel = new GameObject("Congratulations Panel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        messagePanel.transform.SetParent(congratulationsOverlay.transform, false);
        RectTransform messagePanelRect = messagePanel.GetComponent<RectTransform>();
        messagePanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        messagePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        messagePanelRect.pivot = new Vector2(0.5f, 0.5f);
        messagePanelRect.anchoredPosition = Vector2.zero;
        messagePanelRect.sizeDelta = new Vector2(620f, 300f);

        Image messagePanelImage = messagePanel.GetComponent<Image>();
        messagePanelImage.color = new Color(0.035f, 0.055f, 0.09f, 0.96f);
        messagePanelImage.raycastTarget = false;

        completionLevelText = CreateLabel(messagePanel.transform, "Completed Level",
            "WORLD 1-1", 28, FontStyle.Bold, new Color(0.53f, 0.82f, 1f));
        SetOverlayLabelRect(completionLevelText, new Vector2(0f, 96f), new Vector2(560f, 44f));

        Text message = CreateLabel(messagePanel.transform, "Congratulations Message",
            "Congratulations!", 44, FontStyle.Bold, Color.white);
        SetOverlayLabelRect(message, new Vector2(0f, 38f), new Vector2(580f, 64f));

        completionTimeText = CreateLabel(messagePanel.transform, "Completion Time",
            "TIME: 00:00", 28, FontStyle.Bold, Color.white);
        SetOverlayLabelRect(completionTimeText, new Vector2(0f, -30f), new Vector2(560f, 44f));

        Text returnMessage = CreateLabel(messagePanel.transform, "Return To World Map Message",
            "Returning to the World Map...", 18, FontStyle.Normal,
            new Color(0.78f, 0.82f, 0.88f));
        SetOverlayLabelRect(returnMessage, new Vector2(0f, -103f), new Vector2(560f, 34f));

        congratulationsOverlay.SetActive(false);
    }

    private static void SetOverlayLabelRect(Text label, Vector2 position, Vector2 size)
    {
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Text CreateLabel(Transform parent, string objectName, string content,
        int fontSize, FontStyle style, Color color)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text));
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

    private void UpdateClockDisplay()
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        lastDisplayedSecond = totalSeconds;
    }

    private void OnMatchedBoxesRemoved(int removedCount)
    {
        remainingBoxCount = Mathf.Max(0, remainingBoxCount - removedCount);
        UpdateBoxCountDisplay();

        if (remainingBoxCount == 0)
        {
            ShowCongratulations();
        }
    }

    private void UpdateBoxCountDisplay()
    {
        if (boxCountText != null)
        {
            boxCountText.text = "BOXES: " + remainingBoxCount.ToString("00");
        }
    }

    private void OnPlayerHealthChanged(int currentHealth, int maximumHealth)
    {
        UpdateHealthDisplay(currentHealth, maximumHealth);
    }

    private void UpdateHealthDisplay(int currentHealth, int maximumHealth)
    {
        if (healthHeartsText == null)
        {
            return;
        }

        string hearts = string.Empty;
        for (int i = 0; i < maximumHealth; i++)
        {
            string color = i < currentHealth ? "#F05268" : "#59616E";
            hearts += "<color=" + color + ">♥</color> ";
        }

        healthHeartsText.text = hearts.TrimEnd();
    }

    private void ShowCongratulations()
    {
        if (completionRecorded)
        {
            return;
        }

        completionRecorded = true;
        timerRunning = false;
        UpdateClockDisplay();

        int completedStageNumber = PlayerPrefs.GetInt("WorldMap.SelectedStageNumber",
            PlayerPrefs.GetInt("WorldMap.SelectedStage", 0) + 1);
        int zeroBasedStage = Mathf.Max(1, completedStageNumber) - 1;
        int worldNumber = zeroBasedStage / StagesPerWorld + 1;
        int levelNumber = zeroBasedStage % StagesPerWorld + 1;
        if (completionLevelText != null)
        {
            completionLevelText.text = "WORLD " + worldNumber + "-" + levelNumber;
        }
        if (completionTimeText != null && timerText != null)
        {
            completionTimeText.text = "TIME: " + timerText.text;
        }

        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
        if (playerHealth != null)
        {
            playerHealth.SetInvincible(true);
        }

        PlayerProgressSave.CompleteStage(completedStageNumber, 0, elapsedTime);
        PauseMenuController.LockGameplayForCompletion();
        Time.timeScale = 0f;

        if (congratulationsOverlay != null)
        {
            congratulationsOverlay.SetActive(true);
        }

        StartCoroutine(ReturnToWorldMapAfterDelay());
    }

    private IEnumerator ReturnToWorldMapAfterDelay()
    {
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        PauseMenuController.ClearCompletionLockForSceneTransition();
        SceneTransition.LoadScene(WorldMapScenePath);
    }
}
