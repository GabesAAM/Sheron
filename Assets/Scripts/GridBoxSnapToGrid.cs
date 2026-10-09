using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class GridBoxSnapToGrid : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("Keep this equal to the Cell Size on GridMapDimensions.")]
    private float cellSize = 1f;

    private GridMapDimensions mapGrid;
    private BoxCollider boxCollider;
    private float settledTime;

    private void OnEnable()
    {
        boxCollider = GetComponent<BoxCollider>();
        FindMapGrid();
        if (Application.isPlaying)
        {
            return;
        }

        SnapToGrid();
    }

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.01f, cellSize);
        boxCollider = GetComponent<BoxCollider>();
        FindMapGrid();
        SnapToGrid();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (mapGrid == null)
            {
                FindMapGrid();
            }

            SnapThrownBoxAfterLanding();
            return;
        }

        if (mapGrid == null)
        {
            FindMapGrid();
        }

        SnapToGrid();
    }

    private void FindMapGrid()
    {
        if (mapGrid == null)
        {
            mapGrid = FindObjectOfType<GridMapDimensions>();
        }
    }

    private void SnapToGrid()
    {
        if (!TryGetGridBounds(out Bounds mapBounds))
        {
            return;
        }

        Vector3 snappedPosition;
        if (!TryGetSnappedPosition(mapBounds, out snappedPosition))
        {
            return;
        }

        Bounds boxBounds = boxCollider.bounds;
        float bottomOffset = boxBounds.min.y - transform.position.y;
        snappedPosition.y = mapBounds.max.y - bottomOffset;

        if ((transform.position - snappedPosition).sqrMagnitude > 0.000001f)
        {
            transform.position = snappedPosition;
        }
    }

    private bool TryGetGridBounds(out Bounds mapBounds)
    {
        mapBounds = new Bounds();
        if (boxCollider == null || mapGrid == null)
        {
            return false;
        }

        BoxCollider mapCollider = mapGrid.GetComponent<BoxCollider>();
        if (mapCollider == null)
        {
            return false;
        }

        mapBounds = mapCollider.bounds;
        return true;
    }

    private bool TryGetSnappedPosition(Bounds mapBounds, out Vector3 snappedPosition)
    {
        snappedPosition = transform.position;
        if (boxCollider == null)
        {
            return false;
        }

        Bounds boxBounds = boxCollider.bounds;
        int mapCellsX = Mathf.Max(1, Mathf.RoundToInt(mapBounds.size.x / cellSize));
        int mapCellsZ = Mathf.Max(1, Mathf.RoundToInt(mapBounds.size.z / cellSize));
        int boxCellsX = Mathf.Max(1, Mathf.RoundToInt(boxBounds.size.x / cellSize));
        int boxCellsZ = Mathf.Max(1, Mathf.RoundToInt(boxBounds.size.z / cellSize));

        if (boxCellsX > mapCellsX || boxCellsZ > mapCellsZ)
        {
            return false;
        }

        float snappedBoxWidth = boxCellsX * cellSize;
        float snappedBoxDepth = boxCellsZ * cellSize;
        int cellX = Mathf.RoundToInt((transform.position.x - mapBounds.min.x - snappedBoxWidth * 0.5f) / cellSize);
        int cellZ = Mathf.RoundToInt((transform.position.z - mapBounds.min.z - snappedBoxDepth * 0.5f) / cellSize);
        cellX = Mathf.Clamp(cellX, 0, mapCellsX - boxCellsX);
        cellZ = Mathf.Clamp(cellZ, 0, mapCellsZ - boxCellsZ);

        snappedPosition.x = mapBounds.min.x + (cellX + boxCellsX * 0.5f) * cellSize;
        snappedPosition.z = mapBounds.min.z + (cellZ + boxCellsZ * 0.5f) * cellSize;

        return true;
    }

    private void SnapThrownBoxAfterLanding()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null || body.isKinematic || transform.parent != null)
        {
            settledTime = 0f;
            return;
        }

        bool hasSettled = body.velocity.sqrMagnitude < 0.0225f
            && body.angularVelocity.sqrMagnitude < 0.04f;
        settledTime = hasSettled ? settledTime + Time.deltaTime : 0f;
        if (settledTime < 0.2f || !TryGetGridBounds(out Bounds mapBounds))
        {
            return;
        }

        Vector3 snappedPosition;
        if (!TryGetSnappedPosition(mapBounds, out snappedPosition))
        {
            return;
        }

        Bounds boxBounds = boxCollider.bounds;
        float bottomOffset = boxBounds.min.y - transform.position.y;
        transform.position = snappedPosition;
        Physics.SyncTransforms();

        Bounds movedBounds = boxCollider.bounds;
        Vector3 rayOrigin = new Vector3(snappedPosition.x, movedBounds.max.y + 10f, snappedPosition.z);
        float rayDistance = Mathf.Max(0f, rayOrigin.y - mapBounds.min.y + 10f);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayDistance);
        float supportHeight = mapBounds.max.y;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == boxCollider || hitCollider.transform.IsChildOf(transform)
                || hitCollider.GetComponentInParent<TopDownCharacterController>() != null)
            {
                continue;
            }

            supportHeight = Mathf.Max(supportHeight, hits[i].point.y);
        }

        Vector3 finalPosition = transform.position;
        finalPosition.y = supportHeight - bottomOffset;
        transform.position = finalPosition;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        settledTime = 0f;
    }
}
