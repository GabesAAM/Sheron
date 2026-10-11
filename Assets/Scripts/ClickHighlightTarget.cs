using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ClickHighlightTarget : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.25f, 1f);
    [SerializeField, Range(0f, 1f)] private float highlightStrength = 0.65f;
    [SerializeField, Min(0.1f)] private float maxHighlightDuration = 2f;

    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Color originalColor = Color.white;
    private bool isHighlighted;
    private Coroutine highlightTimeout;

    public bool IsHighlighted
    {
        get { return isHighlighted; }
    }

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
        if (highlighted && !isHighlighted)
        {
            if (highlightTimeout != null)
            {
                StopCoroutine(highlightTimeout);
            }

            highlightTimeout = StartCoroutine(ClearHighlightAfterTimeout());
        }
        else if (!highlighted && highlightTimeout != null)
        {
            StopCoroutine(highlightTimeout);
            highlightTimeout = null;
        }

        isHighlighted = highlighted;
        UpdateDisplayedColor();
    }

    private IEnumerator ClearHighlightAfterTimeout()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, maxHighlightDuration));
        highlightTimeout = null;
        isHighlighted = false;
        UpdateDisplayedColor();
    }

    private void OnDisable()
    {
        if (highlightTimeout != null)
        {
            StopCoroutine(highlightTimeout);
            highlightTimeout = null;
        }

        if (isHighlighted)
        {
            isHighlighted = false;
            UpdateDisplayedColor();
        }
    }

    public void SetBaseMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        targetRenderer.sharedMaterial = material;
        if (material.HasProperty(ColorProperty))
        {
            originalColor = material.GetColor(ColorProperty);
        }

        UpdateDisplayedColor();
    }

    private void UpdateDisplayedColor()
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
        Color displayColor = isHighlighted
            ? Color.Lerp(originalColor, highlightColor, highlightStrength)
            : originalColor;
        propertyBlock.SetColor(ColorProperty, displayColor);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
