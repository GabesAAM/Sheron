using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

    private CanvasGroup canvasGroup;
    private Text loadingText;
    private bool isLoading;

    public static void LoadScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError("Cannot load a scene with an empty path.");
            return;
        }

        if (instance == null)
        {
            GameObject transitionObject = new GameObject("Scene Transition");
            instance = transitionObject.AddComponent<SceneTransition>();
        }

        if (!instance.isLoading)
        {
            instance.StartCoroutine(instance.LoadSceneRoutine(scenePath));
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateLoadingScreen();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void CreateLoadingScreen()
    {
        GameObject canvasObject = new GameObject("Loading Screen Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        GameObject background = new GameObject("Black Background",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(canvasObject.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = Color.black;
        backgroundImage.raycastTarget = true;

        GameObject labelObject = new GameObject("Loading Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(background.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, -80f);
        labelRect.sizeDelta = new Vector2(360f, 54f);

        loadingText = labelObject.GetComponent<Text>();
        loadingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        loadingText.fontSize = 24;
        loadingText.fontStyle = FontStyle.Bold;
        loadingText.alignment = TextAnchor.MiddleCenter;
        loadingText.color = Color.white;
        loadingText.raycastTarget = false;
        loadingText.text = "LOADING...";
    }

    private IEnumerator LoadSceneRoutine(string scenePath)
    {
        isLoading = true;
        canvasGroup.blocksRaycasts = true;
        loadingText.text = "LOADING...";

        yield return FadeCanvas(1f);
        yield return null;

        AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scenePath);
        if (operation == null)
        {
            Debug.LogError("Unity could not start loading scene: " + scenePath);
            isLoading = false;
            canvasGroup.blocksRaycasts = false;
            yield return FadeCanvas(0f);
            yield break;
        }

        operation.allowSceneActivation = false;
        while (operation.progress < 0.9f)
        {
            int percent = Mathf.Clamp(Mathf.RoundToInt(operation.progress / 0.9f * 100f), 0, 99);
            loadingText.text = "LOADING " + percent + "%";
            yield return null;
        }

        loadingText.text = "LOADING 100%";
        yield return null;
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();
        yield return FadeCanvas(0f);
        canvasGroup.blocksRaycasts = false;
        Destroy(gameObject);
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
