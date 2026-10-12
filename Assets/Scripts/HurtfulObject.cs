using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public sealed class HurtfulObject : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float cellSize = 1f;

    private BoxCollider trapCollider;
    private GridMapDimensions mapGrid;
    private bool snapPending = true;

    private void OnEnable()
    {
        snapPending = true;
    }

    private void OnValidate()
    {
        // OnValidate may run during scene restoration. Defer all scene/component
        // access and collider-bound queries until Update has a stable scene.
        cellSize = Mathf.Max(0.01f, cellSize);
    }

    private void Update()
    {
        if (Application.isPlaying && !snapPending)
        {
            return;
        }

        CacheReferences();
        if (trapCollider != null)
        {
            trapCollider.isTrigger = false;
        }

        SnapToGrid();
        snapPending = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerHealth playerHealth = collision.collider.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage();
        }
    }

    private void CacheReferences()
    {
        if (trapCollider == null)
        {
            trapCollider = GetComponent<BoxCollider>();
        }

        if (mapGrid == null)
        {
            mapGrid = FindObjectOfType<GridMapDimensions>();
        }
    }

    private void SnapToGrid()
    {
        if (trapCollider == null)
        {
            return;
        }

        if (mapGrid == null)
        {
            CacheReferences();
        }

        if (mapGrid == null)
        {
            return;
        }

        BoxCollider mapCollider = mapGrid.GetComponent<BoxCollider>();
        if (mapCollider == null
            || !GridBoxSnapToGrid.TryGetGridAlignedPosition(
                mapGrid, trapCollider, cellSize, transform.position, mapCollider.bounds.max.y,
                out Vector3 snappedPosition))
        {
            return;
        }

        if ((transform.position - snappedPosition).sqrMagnitude > 0.000001f)
        {
            transform.position = snappedPosition;
        }
    }
}
