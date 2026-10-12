using UnityEngine;

public class WorldMapStageNode : MonoBehaviour
{
    [SerializeField] private int stageNumber = 1;
    [SerializeField] private string displayName = "New Stage";
    [SerializeField, Tooltip("Optional sprite shown above this stage marker.")] private Sprite stageSprite;
    [SerializeField] private Transform selectionRing;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private TextMesh numberLabel;
    [SerializeField, Tooltip("Filled automatically from the Scene Asset field.")]
    private string scenePath;
#if UNITY_EDITOR
    [SerializeField, Tooltip("Drag the scene this stage should load.")]
    private UnityEditor.SceneAsset sceneAsset;
#endif

    public int StageNumber { get { return stageNumber; } }
    public string DisplayName { get { return displayName; } }
    public string ScenePath { get { return scenePath; } }
    public Sprite StageSprite { get { return stageSprite; } }
    public SpriteRenderer IconRenderer { get { return iconRenderer; } }
    public Transform SelectionRing { get { return selectionRing; } }

    private Renderer markerRenderer;
    private Renderer platformRenderer;
    private MaterialPropertyBlock lockedMaterialProperties;
    private Color originalIconColor = Color.white;
    private Color originalNumberColor = Color.white;
    private Color originalMarkerColor = Color.white;
    private Color originalPlatformColor = Color.white;
    private bool lockedVisualsCached;

    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    public void Configure(int number, string title, Transform ring, SpriteRenderer icon, TextMesh label)
    {
        stageNumber = number;
        displayName = title;
        selectionRing = ring;
        iconRenderer = icon;
        numberLabel = label;
        if (numberLabel != null)
        {
            numberLabel.text = number.ToString();
        }
    }

    public void SetDisplayName(string title)
    {
        displayName = title;
    }

    public void SetSelected(bool selected)
    {
        if (selectionRing != null && selectionRing.gameObject.activeSelf != selected)
        {
            selectionRing.gameObject.SetActive(selected);
        }
    }

    public void SetLocked(bool locked)
    {
        CacheLockedVisuals();

        if (iconRenderer != null)
        {
            iconRenderer.color = locked
                ? Color.Lerp(originalIconColor, Color.gray, 0.72f)
                : originalIconColor;
        }

        if (numberLabel != null)
        {
            numberLabel.color = locked
                ? Color.Lerp(originalNumberColor, Color.gray, 0.72f)
                : originalNumberColor;
        }

        ApplyLockedTint(markerRenderer, originalMarkerColor, locked);
        ApplyLockedTint(platformRenderer, originalPlatformColor, locked);
    }

    private void CacheLockedVisuals()
    {
        if (lockedVisualsCached)
        {
            return;
        }

        if (iconRenderer != null)
        {
            originalIconColor = iconRenderer.color;
        }
        if (numberLabel != null)
        {
            originalNumberColor = numberLabel.color;
        }

        Transform marker = transform.Find("Marker");
        if (marker != null)
        {
            markerRenderer = marker.GetComponent<Renderer>();
        }
        Transform platform = transform.Find("Platform");
        if (platform != null)
        {
            platformRenderer = platform.GetComponent<Renderer>();
        }
        if (markerRenderer != null && markerRenderer.sharedMaterial != null
            && markerRenderer.sharedMaterial.HasProperty(ColorProperty))
        {
            originalMarkerColor = markerRenderer.sharedMaterial.GetColor(ColorProperty);
        }
        if (platformRenderer != null && platformRenderer.sharedMaterial != null
            && platformRenderer.sharedMaterial.HasProperty(ColorProperty))
        {
            originalPlatformColor = platformRenderer.sharedMaterial.GetColor(ColorProperty);
        }

        lockedVisualsCached = true;
    }

    private void ApplyLockedTint(Renderer targetRenderer, Color originalColor, bool locked)
    {
        if (targetRenderer == null || targetRenderer.sharedMaterial == null
            || !targetRenderer.sharedMaterial.HasProperty(ColorProperty))
        {
            return;
        }

        if (lockedMaterialProperties == null)
        {
            lockedMaterialProperties = new MaterialPropertyBlock();
        }

        targetRenderer.GetPropertyBlock(lockedMaterialProperties);
        Color tint = locked ? Color.Lerp(originalColor, Color.gray, 0.72f) : originalColor;
        lockedMaterialProperties.SetColor(ColorProperty, tint);
        targetRenderer.SetPropertyBlock(lockedMaterialProperties);
    }

    public void FaceCamera(Camera targetCamera)
    {
        if (targetCamera == null)
        {
            return;
        }

        if (iconRenderer != null && iconRenderer.sprite != null)
        {
            iconRenderer.transform.rotation = Quaternion.LookRotation(
                targetCamera.transform.position - iconRenderer.transform.position, Vector3.up);
        }

        if (numberLabel != null)
        {
            numberLabel.transform.rotation = Quaternion.LookRotation(
                targetCamera.transform.position - numberLabel.transform.position, Vector3.up);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (iconRenderer != null)
        {
            iconRenderer.sprite = stageSprite;
        }

        if (numberLabel != null)
        {
            numberLabel.text = Mathf.Max(1, stageNumber).ToString();
        }

        if (sceneAsset == null)
        {
            return;
        }

        scenePath = UnityEditor.AssetDatabase.GetAssetPath(sceneAsset);
        if (string.IsNullOrEmpty(scenePath))
        {
            return;
        }

        UnityEditor.EditorBuildSettingsScene[] existingScenes = UnityEditor.EditorBuildSettings.scenes;
        for (int i = 0; i < existingScenes.Length; i++)
        {
            if (existingScenes[i].path == scenePath && existingScenes[i].enabled)
            {
                return;
            }
        }

        System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene> buildScenes =
            new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(existingScenes);
        bool found = false;
        for (int i = 0; i < buildScenes.Count; i++)
        {
            if (buildScenes[i].path == scenePath)
            {
                buildScenes[i] = new UnityEditor.EditorBuildSettingsScene(scenePath, true);
                found = true;
                break;
            }
        }

        if (!found)
        {
            buildScenes.Add(new UnityEditor.EditorBuildSettingsScene(scenePath, true));
        }

        UnityEditor.EditorBuildSettings.scenes = buildScenes.ToArray();
    }

    public void SetSceneAsset(UnityEditor.SceneAsset asset)
    {
        sceneAsset = asset;
        scenePath = asset != null ? UnityEditor.AssetDatabase.GetAssetPath(asset) : string.Empty;
    }
#endif
}
