using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class GridBoxSnapToGrid : MonoBehaviour
{
    public static event System.Action<int> MatchedBoxesRemoved;

    private static readonly HashSet<int> BoxesResolvingMatch = new HashSet<int>();
    [SerializeField, Min(0.01f), Tooltip("Keep this equal to the Cell Size on GridMapDimensions.")]
    private float cellSize = 1f;

    private GridMapDimensions mapGrid;
    private BoxCollider boxCollider;
    private float settledTime;
    private bool targetedThrowPending;
    private bool matchResolutionStarted;

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

        Bounds boxBounds = GetWorldBounds(boxCollider);
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

        Bounds boxBounds = GetWorldBounds(boxCollider);
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

    public bool TryFindNearestEmptyGridPosition(
        BoxCollider movingBox,
        Vector3 desiredPosition,
        Vector3 playerPosition,
        Vector3 facingDirection,
        out Vector3 placementPosition)
    {
        placementPosition = Vector3.zero;
        if (movingBox == null)
        {
            return false;
        }

        if (mapGrid == null)
        {
            FindMapGrid();
        }

        if (!TryGetGridBounds(out Bounds mapBounds))
        {
            return false;
        }

        Bounds movingBounds = GetWorldBounds(movingBox);
        int mapCellsX = Mathf.Max(1, Mathf.RoundToInt(mapBounds.size.x / cellSize));
        int mapCellsZ = Mathf.Max(1, Mathf.RoundToInt(mapBounds.size.z / cellSize));
        int boxCellsX = Mathf.Max(1, Mathf.RoundToInt(movingBounds.size.x / cellSize));
        int boxCellsZ = Mathf.Max(1, Mathf.RoundToInt(movingBounds.size.z / cellSize));
        if (boxCellsX > mapCellsX || boxCellsZ > mapCellsZ)
        {
            return false;
        }

        float boxWidth = boxCellsX * cellSize;
        float boxDepth = boxCellsZ * cellSize;
        float bestScore = float.PositiveInfinity;
        bool foundEmptyCell = false;
        Vector3 horizontalFacing = facingDirection;
        horizontalFacing.y = 0f;
        if (horizontalFacing.sqrMagnitude < 0.0001f)
        {
            horizontalFacing = Vector3.forward;
        }
        horizontalFacing.Normalize();
        Vector3 desiredOffset = desiredPosition - playerPosition;
        desiredOffset.y = 0f;
        float minimumForwardDistance = Vector3.Dot(desiredOffset, horizontalFacing);
        BoxCollider mapCollider = mapGrid.GetComponent<BoxCollider>();
        BoxCollider[] allBoxes = FindObjectsOfType<BoxCollider>();

        for (int cellX = 0; cellX <= mapCellsX - boxCellsX; cellX++)
        {
            for (int cellZ = 0; cellZ <= mapCellsZ - boxCellsZ; cellZ++)
            {
                Vector3 candidate = new Vector3(
                    mapBounds.min.x + (cellX + boxCellsX * 0.5f) * cellSize,
                    0f,
                    mapBounds.min.z + (cellZ + boxCellsZ * 0.5f) * cellSize);
                Bounds footprint = new Bounds(candidate, new Vector3(boxWidth, 0.02f, boxDepth));
                if (IsFootprintOccupied(footprint, movingBox, mapCollider, allBoxes))
                {
                    continue;
                }

                Vector3 horizontalDifference = candidate - desiredPosition;
                horizontalDifference.y = 0f;
                float score = horizontalDifference.sqrMagnitude;
                Vector3 fromPlayer = candidate - playerPosition;
                fromPlayer.y = 0f;
                if (Vector3.Dot(fromPlayer, horizontalFacing) < minimumForwardDistance)
                {
                    continue;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    placementPosition = candidate;
                    foundEmptyCell = true;
                }
            }
        }

        if (!foundEmptyCell)
        {
            return false;
        }

        float colliderCenterOffset = movingBounds.center.y - movingBox.transform.position.y;
        placementPosition.y = mapBounds.max.y + movingBounds.extents.y - colliderCenterOffset;
        return true;
    }

    public bool TryGetGridAlignedPosition(
        BoxCollider movingBox,
        Vector3 desiredPosition,
        float supportSurfaceY,
        out Vector3 placementPosition)
    {
        placementPosition = desiredPosition;
        if (movingBox == null)
        {
            return false;
        }

        if (mapGrid == null)
        {
            FindMapGrid();
        }

        if (!TryGetGridBounds(out Bounds mapBounds))
        {
            return false;
        }

        Bounds movingBounds = GetWorldBounds(movingBox);
        int mapCellsX = Mathf.Max(1, Mathf.RoundToInt(mapBounds.size.x / cellSize));
        int mapCellsZ = Mathf.Max(1, Mathf.RoundToInt(mapBounds.size.z / cellSize));
        int boxCellsX = Mathf.Max(1, Mathf.RoundToInt(movingBounds.size.x / cellSize));
        int boxCellsZ = Mathf.Max(1, Mathf.RoundToInt(movingBounds.size.z / cellSize));
        if (boxCellsX > mapCellsX || boxCellsZ > mapCellsZ)
        {
            return false;
        }

        int cellX = Mathf.RoundToInt(
            (desiredPosition.x - mapBounds.min.x - boxCellsX * cellSize * 0.5f) / cellSize);
        int cellZ = Mathf.RoundToInt(
            (desiredPosition.z - mapBounds.min.z - boxCellsZ * cellSize * 0.5f) / cellSize);
        cellX = Mathf.Clamp(cellX, 0, mapCellsX - boxCellsX);
        cellZ = Mathf.Clamp(cellZ, 0, mapCellsZ - boxCellsZ);

        placementPosition.x = mapBounds.min.x + (cellX + boxCellsX * 0.5f) * cellSize;
        placementPosition.z = mapBounds.min.z + (cellZ + boxCellsZ * 0.5f) * cellSize;
        float colliderCenterOffset = movingBounds.center.y - movingBox.transform.position.y;
        placementPosition.y = supportSurfaceY + movingBounds.extents.y - colliderCenterOffset;
        return true;
    }

    public void BeginTargetedThrow()
    {
        targetedThrowPending = true;
        settledTime = 0f;
    }

    public void EndTargetedThrow()
    {
        targetedThrowPending = false;
        settledTime = 0f;
    }

    public static Bounds GetWorldBounds(BoxCollider collider)
    {
        Transform colliderTransform = collider.transform;
        Vector3 scale = colliderTransform.lossyScale;
        Vector3 halfSize = Vector3.Scale(collider.size, new Vector3(
            Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f;
        Vector3 axisX = colliderTransform.right;
        Vector3 axisY = colliderTransform.up;
        Vector3 axisZ = colliderTransform.forward;
        Vector3 extents = new Vector3(
            Mathf.Abs(axisX.x) * halfSize.x + Mathf.Abs(axisY.x) * halfSize.y + Mathf.Abs(axisZ.x) * halfSize.z,
            Mathf.Abs(axisX.y) * halfSize.x + Mathf.Abs(axisY.y) * halfSize.y + Mathf.Abs(axisZ.y) * halfSize.z,
            Mathf.Abs(axisX.z) * halfSize.x + Mathf.Abs(axisY.z) * halfSize.y + Mathf.Abs(axisZ.z) * halfSize.z);

        return new Bounds(colliderTransform.TransformPoint(collider.center), extents * 2f);
    }

    private static bool IsFootprintOccupied(
        Bounds footprint,
        BoxCollider movingBox,
        BoxCollider mapCollider,
        BoxCollider[] allBoxes)
    {
        for (int i = 0; i < allBoxes.Length; i++)
        {
            BoxCollider other = allBoxes[i];
            if (other == null || !other.enabled || other == movingBox || other == mapCollider
                || other.transform.IsChildOf(movingBox.transform)
                || other.GetComponentInParent<TopDownCharacterController>() != null)
            {
                continue;
            }

            Bounds otherBounds = other.bounds;
            bool overlapsX = Mathf.Min(footprint.max.x, otherBounds.max.x)
                - Mathf.Max(footprint.min.x, otherBounds.min.x) > 0.01f;
            bool overlapsZ = Mathf.Min(footprint.max.z, otherBounds.max.z)
                - Mathf.Max(footprint.min.z, otherBounds.min.z) > 0.01f;
            if (overlapsX && overlapsZ)
            {
                return true;
            }
        }

        return false;
    }

    public int GetStackHeightInCells(BoxCollider baseBox, BoxCollider ignoredBox, out float stackTop)
    {
        stackTop = baseBox != null ? baseBox.bounds.max.y : 0f;
        if (baseBox == null)
        {
            return 1;
        }

        if (mapGrid == null)
        {
            FindMapGrid();
        }

        if (!TryGetGridBounds(out Bounds mapBounds))
        {
            return 1;
        }

        BoxCollider mapCollider = mapGrid.GetComponent<BoxCollider>();
        Bounds baseBounds = GetWorldBounds(baseBox);
        float sampleX = baseBounds.center.x;
        float sampleZ = baseBounds.center.z;
        BoxCollider[] allBoxes = FindObjectsOfType<BoxCollider>();
        stackTop = mapBounds.max.y;

        for (int i = 0; i < allBoxes.Length; i++)
        {
            BoxCollider candidate = allBoxes[i];
            if (candidate == null || !candidate.enabled || candidate == mapCollider || candidate == ignoredBox
                || candidate.GetComponentInParent<TopDownCharacterController>() != null)
            {
                continue;
            }

            Bounds candidateBounds = candidate == baseBox
                ? baseBounds
                : candidate.bounds;
            bool coversSample = sampleX >= candidateBounds.min.x - 0.01f
                && sampleX <= candidateBounds.max.x + 0.01f
                && sampleZ >= candidateBounds.min.z - 0.01f
                && sampleZ <= candidateBounds.max.z + 0.01f;
            if (coversSample)
            {
                stackTop = Mathf.Max(stackTop, candidateBounds.max.y);
            }
        }

        return Mathf.Max(1, Mathf.RoundToInt((stackTop - mapBounds.max.y) / cellSize));
    }

    public ClickHighlightTarget GetTopmostBoxInStack(BoxCollider baseBox)
    {
        if (baseBox == null)
        {
            return null;
        }

        ClickHighlightTarget topmostTarget = baseBox.GetComponent<ClickHighlightTarget>();
        if (topmostTarget == null)
        {
            topmostTarget = baseBox.GetComponentInParent<ClickHighlightTarget>();
        }

        if (mapGrid == null)
        {
            FindMapGrid();
        }

        if (!TryGetGridBounds(out Bounds mapBounds))
        {
            return topmostTarget;
        }

        BoxCollider mapCollider = mapGrid.GetComponent<BoxCollider>();
        Bounds baseBounds = GetWorldBounds(baseBox);
        float highestTop = baseBounds.max.y;
        float sampleX = baseBounds.center.x;
        float sampleZ = baseBounds.center.z;
        BoxCollider[] allBoxes = FindObjectsOfType<BoxCollider>();

        for (int i = 0; i < allBoxes.Length; i++)
        {
            BoxCollider candidate = allBoxes[i];
            if (candidate == null || !candidate.enabled || candidate == baseBox || candidate == mapCollider
                || candidate.GetComponentInParent<TopDownCharacterController>() != null)
            {
                continue;
            }

            Bounds candidateBounds = candidate.bounds;
            bool coversBaseCenter = sampleX >= candidateBounds.min.x - 0.01f
                && sampleX <= candidateBounds.max.x + 0.01f
                && sampleZ >= candidateBounds.min.z - 0.01f
                && sampleZ <= candidateBounds.max.z + 0.01f;
            bool sitsOnBase = candidateBounds.min.y >= baseBounds.max.y - 0.05f;
            if (!coversBaseCenter || !sitsOnBase || candidateBounds.max.y <= highestTop)
            {
                continue;
            }

            ClickHighlightTarget candidateTarget = candidate.GetComponent<ClickHighlightTarget>();
            if (candidateTarget == null)
            {
                candidateTarget = candidate.GetComponentInParent<ClickHighlightTarget>();
            }

            if (candidateTarget != null)
            {
                highestTop = candidateBounds.max.y;
                topmostTarget = candidateTarget;
            }
        }

        return topmostTarget;
    }

    private void SnapThrownBoxAfterLanding()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (targetedThrowPending || body == null || body.isKinematic || transform.parent != null)
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

        Bounds boxBounds = GetWorldBounds(boxCollider);
        float bottomOffset = boxBounds.min.y - transform.position.y;
        transform.position = snappedPosition;
        Physics.SyncTransforms();

        Bounds movedBounds = GetWorldBounds(boxCollider);
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
        CheckForMatchingStack();
    }

    public void CheckForMatchingStack()
    {
        if (matchResolutionStarted || boxCollider == null)
        {
            return;
        }

        Renderer boxRenderer = GetComponent<Renderer>();
        if (boxRenderer == null || boxRenderer.sharedMaterial == null)
        {
            return;
        }

        int colorProperty = Shader.PropertyToID("_Color");
        if (!boxRenderer.sharedMaterial.HasProperty(colorProperty))
        {
            return;
        }

        Color boxColor = boxRenderer.sharedMaterial.GetColor(colorProperty);
        Bounds boxBounds = GetWorldBounds(boxCollider);
        List<GridBoxSnapToGrid> column = new List<GridBoxSnapToGrid>();
        GridBoxSnapToGrid[] allBoxes = FindObjectsOfType<GridBoxSnapToGrid>();
        for (int i = 0; i < allBoxes.Length; i++)
        {
            GridBoxSnapToGrid candidate = allBoxes[i];
            if (candidate == null || candidate.boxCollider == null || !candidate.boxCollider.enabled
                || candidate.transform.parent != null || candidate.GetComponent<Renderer>() == null)
            {
                continue;
            }

            Bounds candidateBounds = GetWorldBounds(candidate.boxCollider);
            bool sameColumn = Mathf.Abs(candidateBounds.center.x - boxBounds.center.x) < 0.05f
                && Mathf.Abs(candidateBounds.center.z - boxBounds.center.z) < 0.05f;
            if (sameColumn)
            {
                column.Add(candidate);
            }
        }

        column.Sort((a, b) => GetWorldBounds(a.boxCollider).min.y.CompareTo(GetWorldBounds(b.boxCollider).min.y));
        int clickedIndex = column.IndexOf(this);
        if (clickedIndex < 0)
        {
            return;
        }

        int first = clickedIndex;
        int last = clickedIndex;
        while (first > 0 && IsMatchingAdjacent(column[first - 1], column[first], boxColor)) first--;
        while (last + 1 < column.Count && IsMatchingAdjacent(column[last], column[last + 1], boxColor)) last++;
        if (last - first + 1 < 3)
        {
            return;
        }

        List<GridBoxSnapToGrid> matchedBoxes = column.GetRange(first, last - first + 1);
        for (int i = 0; i < matchedBoxes.Count; i++)
        {
            if (matchedBoxes[i].matchResolutionStarted
                || BoxesResolvingMatch.Contains(matchedBoxes[i].gameObject.GetInstanceID()))
            {
                return;
            }
        }

        for (int i = 0; i < matchedBoxes.Count; i++)
        {
            BoxesResolvingMatch.Add(matchedBoxes[i].gameObject.GetInstanceID());
            matchedBoxes[i].matchResolutionStarted = true;
        }

        StartCoroutine(BlinkAndRemove(matchedBoxes));
    }

    private static bool IsMatchingAdjacent(GridBoxSnapToGrid lower, GridBoxSnapToGrid upper, Color expectedColor)
    {
        if (lower == null || upper == null || lower.boxCollider == null || upper.boxCollider == null)
        {
            return false;
        }

        Bounds lowerBounds = GetWorldBounds(lower.boxCollider);
        Bounds upperBounds = GetWorldBounds(upper.boxCollider);
        if (Mathf.Abs(lowerBounds.max.y - upperBounds.min.y) > 0.08f)
        {
            return false;
        }

        Renderer renderer = upper.GetComponent<Renderer>();
        Renderer lowerRenderer = lower.GetComponent<Renderer>();
        int colorProperty = Shader.PropertyToID("_Color");
        return renderer != null && lowerRenderer != null
            && renderer.sharedMaterial != null && lowerRenderer.sharedMaterial != null
            && renderer.sharedMaterial.HasProperty(colorProperty)
            && lowerRenderer.sharedMaterial.HasProperty(colorProperty)
            && ColorsMatch(renderer.sharedMaterial.GetColor(colorProperty), expectedColor)
            && ColorsMatch(lowerRenderer.sharedMaterial.GetColor(colorProperty), expectedColor);
    }

    private static bool ColorsMatch(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f
            && Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.01f;
    }

    private IEnumerator BlinkAndRemove(List<GridBoxSnapToGrid> matchedBoxes)
    {
        const int blinkCount = 3;
        const float blinkInterval = 0.12f;
        for (int blink = 0; blink < blinkCount; blink++)
        {
            SetMatchedRenderersVisible(matchedBoxes, false);
            yield return new WaitForSeconds(blinkInterval);
            SetMatchedRenderersVisible(matchedBoxes, true);
            yield return new WaitForSeconds(blinkInterval);
        }

        int removedCount = 0;
        for (int i = 0; i < matchedBoxes.Count; i++)
        {
            GridBoxSnapToGrid matchedBox = matchedBoxes[i];
            if (matchedBox != null)
            {
                BoxesResolvingMatch.Remove(matchedBox.gameObject.GetInstanceID());
                Destroy(matchedBox.gameObject);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            MatchedBoxesRemoved?.Invoke(removedCount);
        }
    }

    private static void SetMatchedRenderersVisible(List<GridBoxSnapToGrid> matchedBoxes, bool visible)
    {
        for (int i = 0; i < matchedBoxes.Count; i++)
        {
            if (matchedBoxes[i] == null) continue;
            Renderer renderer = matchedBoxes[i].GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = visible;
        }
    }
}
