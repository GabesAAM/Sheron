using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public sealed class HurtfulObject : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float cellSize = 1f;

    private BoxCollider trapCollider;
    private GridMapDimensions mapGrid;

    private void OnEnable()
    {
        CacheReferences();
        if (trapCollider != null)
        {
            trapCollider.isTrigger = false;
        }
        SnapToGrid();
    }

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.01f, cellSize);
        CacheReferences();
        if (trapCollider != null)
        {
            trapCollider.isTrigger = false;
        }
        SnapToGrid();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            CacheReferences();
            SnapToGrid();
        }
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
