using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ClickHighlightTarget : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.25f, 1f);
    [SerializeField, Range(0f, 1f)] private float highlightStrength = 0.65f;

    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Color originalColor = Color.white;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();

        if (targetRenderer.sharedMaterial != null && targetRenderer.sharedMaterial.HasProperty(ColorProperty))
        {
            originalColor = targetRenderer.sharedMaterial.GetColor(ColorProperty);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        Color displayColor = highlighted
            ? Color.Lerp(originalColor, highlightColor, highlightStrength)
            : originalColor;
        propertyBlock.SetColor(ColorProperty, displayColor);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
