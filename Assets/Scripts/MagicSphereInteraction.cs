using System;
using System.Collections;
using UnityEngine;

public sealed class MagicSphereInteraction : MonoBehaviour
{
    public event Action ApproachCancelled;

    [SerializeField] private TopDownCharacterController movement;
    [SerializeField] private PlayerBoxInteraction boxInteraction;
    [SerializeField] private Transform carryAnchor;
    [SerializeField, Min(0.01f)] private float approachGap = 0.55f;
    [SerializeField, Min(0.1f)] private float approachTimeout = 12f;
    [SerializeField, Min(0.1f)] private float throwSpeed = 6f;

    private MagicSphere heldSphere;
    private Coroutine activeCommand;

    public bool IsHoldingSphere
    {
        get { return heldSphere != null; }
    }

    public bool IsApproaching
    {
        get { return activeCommand != null; }
    }

    private void Awake()
    {
        if (movement == null)
        {
            movement = GetComponent<TopDownCharacterController>();
        }

        if (boxInteraction == null)
        {
            boxInteraction = GetComponent<PlayerBoxInteraction>();
        }
    }

    private void Update()
    {
        bool spacePressed = Input.GetKeyDown(KeyCode.Space);
        bool movementKeyPressed = Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.S)
            || Input.GetKeyDown(KeyCode.D);

        if (activeCommand != null && (spacePressed || movementKeyPressed))
        {
            CancelCurrentCommand();
        }

        if (spacePressed && heldSphere != null)
        {
            DropHeldSphereInFront();
        }
    }

    public void PickUp(MagicSphere sphere)
    {
        if (sphere == null || !sphere.IsAvailable || heldSphere != null
            || activeCommand != null || carryAnchor == null || movement == null
            || (boxInteraction != null && boxInteraction.IsHoldingObject))
        {
            return;
        }

        if (boxInteraction != null)
        {
            boxInteraction.CancelForOtherInteraction();
        }

        movement.CancelMoveTo();
        activeCommand = StartCoroutine(ApproachAndPickUp(sphere));
    }

    public void ThrowAt(ClickHighlightTarget clickedTarget)
    {
        if (clickedTarget == null || heldSphere == null || carryAnchor == null)
        {
            return;
        }

        GridBoxSnapToGrid targetBox = clickedTarget.GetComponent<GridBoxSnapToGrid>();
        Collider targetCollider = clickedTarget.GetComponent<Collider>();
        if (targetBox == null || targetCollider == null)
        {
            return;
        }

        Vector3 launchPosition = carryAnchor.position;
        Vector3 landingPosition = targetCollider.bounds.center;
        float flightTime = CalculateFlightTime(launchPosition, landingPosition);
        Vector3 launchVelocity = (landingPosition - launchPosition
            - 0.5f * Physics.gravity * flightTime * flightTime) / flightTime;

        MagicSphere sphere = heldSphere;
        heldSphere = null;
        sphere.transform.position = launchPosition;
        sphere.LaunchAt(targetBox, launchVelocity);
        StartCoroutine(ApplyAtTargetAfterFlight(sphere, targetBox, flightTime + 0.2f));
    }

    public void CancelForDamage()
    {
        CancelCurrentCommand();
    }

    private IEnumerator ApproachAndPickUp(MagicSphere sphere)
    {
        yield return null;
        Collider sphereCollider = sphere != null ? sphere.GetComponent<Collider>() : null;
        if (sphereCollider == null || !sphereCollider.enabled)
        {
            activeCommand = null;
            yield break;
        }

        movement.MoveTo(sphereCollider.bounds.center, GetStoppingDistance(sphereCollider));
        float elapsed = 0f;
        while (sphere != null && sphereCollider != null && !IsCloseEnough(sphereCollider))
        {
            elapsed += Time.deltaTime;
            if (elapsed >= approachTimeout)
            {
                movement.CancelMoveTo();
                activeCommand = null;
                Debug.LogWarning("The player could not reach the magic sphere.", sphere);
                yield break;
            }

            yield return null;
        }

        movement.CancelMoveTo();
        if (sphere != null && sphere.IsAvailable)
        {
            ClickHighlightTarget highlightTarget = sphere.GetComponent<ClickHighlightTarget>();
            if (highlightTarget != null)
            {
                highlightTarget.SetHighlighted(false);
            }

            heldSphere = sphere;
            heldSphere.CarryBy(carryAnchor);
        }

        activeCommand = null;
    }

    private IEnumerator ApplyAtTargetAfterFlight(
        MagicSphere sphere, GridBoxSnapToGrid targetBox, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sphere != null && targetBox != null)
        {
            sphere.ApplyTo(targetBox);
        }
    }

    private void DropHeldSphereInFront()
    {
        GridMapDimensions mapGrid = FindObjectOfType<GridMapDimensions>();
        BoxCollider mapCollider = mapGrid != null ? mapGrid.GetComponent<BoxCollider>() : null;
        SphereCollider sphereCollider = heldSphere != null ? heldSphere.GetComponent<SphereCollider>() : null;
        if (mapCollider == null || sphereCollider == null || movement == null)
        {
            return;
        }

        Vector3 facingDirection = movement.FacingDirection;
        facingDirection.y = 0f;
        if (facingDirection.sqrMagnitude < 0.0001f)
        {
            facingDirection = Vector3.forward;
        }
        facingDirection.Normalize();

        Bounds mapBounds = mapCollider.bounds;
        GridBoxSnapToGrid gridBox = FindObjectOfType<GridBoxSnapToGrid>();
        float cellSize = gridBox != null ? gridBox.CellSize : 1f;
        float radius = sphereCollider.radius * Mathf.Max(
            Mathf.Abs(heldSphere.transform.lossyScale.x),
            Mathf.Abs(heldSphere.transform.lossyScale.y),
            Mathf.Abs(heldSphere.transform.lossyScale.z));
        Collider playerCollider = GetComponent<Collider>();
        float playerRadius = 0.35f;
        if (playerCollider != null)
        {
            Bounds playerBounds = playerCollider.bounds;
            playerRadius = Mathf.Abs(facingDirection.x) * playerBounds.extents.x
                + Mathf.Abs(facingDirection.z) * playerBounds.extents.z;
        }

        Vector3 desiredPosition = transform.position
            + facingDirection * (playerRadius + radius + 0.1f);
        int cellsX = Mathf.Max(1, Mathf.RoundToInt(mapBounds.size.x / cellSize));
        int cellsZ = Mathf.Max(1, Mathf.RoundToInt(mapBounds.size.z / cellSize));
        float minimumForwardDistance = Vector3.Dot(desiredPosition - transform.position, facingDirection);
        float bestScore = float.PositiveInfinity;
        Vector3 placementPosition = Vector3.zero;
        bool found = false;

        for (int cellX = 0; cellX < cellsX; cellX++)
        {
            for (int cellZ = 0; cellZ < cellsZ; cellZ++)
            {
                Vector3 candidate = new Vector3(
                    mapBounds.min.x + (cellX + 0.5f) * cellSize,
                    mapBounds.max.y + radius,
                    mapBounds.min.z + (cellZ + 0.5f) * cellSize);
                Vector3 fromPlayer = candidate - transform.position;
                fromPlayer.y = 0f;
                if (Vector3.Dot(fromPlayer, facingDirection) < minimumForwardDistance)
                {
                    continue;
                }

                Collider[] overlaps = Physics.OverlapBox(
                    candidate, new Vector3(radius, radius, radius) * 0.9f,
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                bool occupied = false;
                for (int i = 0; i < overlaps.Length; i++)
                {
                    Collider overlap = overlaps[i];
                    if (overlap == null || overlap == sphereCollider || overlap == mapCollider
                        || overlap.GetComponentInParent<TopDownCharacterController>() != null)
                    {
                        continue;
                    }

                    occupied = true;
                    break;
                }

                if (occupied)
                {
                    continue;
                }

                Vector3 difference = candidate - desiredPosition;
                difference.y = 0f;
                float score = difference.sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    placementPosition = candidate;
                    found = true;
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning("There is no empty grid space in front of the player for the magic sphere.", this);
            return;
        }

        MagicSphere sphere = heldSphere;
        heldSphere = null;
        sphere.ReleaseAt(placementPosition);
    }

    private float CalculateFlightTime(Vector3 launchPosition, Vector3 landingPosition)
    {
        Vector3 horizontalOffset = landingPosition - launchPosition;
        horizontalOffset.y = 0f;
        float flightTime = Mathf.Max(0.45f, horizontalOffset.magnitude / throwSpeed);
        float gravityStrength = -Physics.gravity.y;
        if (gravityStrength <= 0.001f)
        {
            return flightTime;
        }

        float verticalDisplacement = landingPosition.y - launchPosition.y;
        float heightToApex = Mathf.Max(0.65f, verticalDisplacement + 0.65f);
        float verticalSpeedAtApex = Mathf.Sqrt(2f * gravityStrength * heightToApex);
        float descendingDiscriminant = Mathf.Max(0f,
            verticalSpeedAtApex * verticalSpeedAtApex - 2f * gravityStrength * verticalDisplacement);
        float descendingTime = (verticalSpeedAtApex + Mathf.Sqrt(descendingDiscriminant)) / gravityStrength;
        return Mathf.Max(flightTime, descendingTime);
    }

    private float GetStoppingDistance(Collider sphereCollider)
    {
        Bounds bounds = sphereCollider.bounds;
        Vector3 direction = bounds.center - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }
        else
        {
            direction.Normalize();
        }

        float targetRadius = Mathf.Abs(direction.x) * bounds.extents.x
            + Mathf.Abs(direction.z) * bounds.extents.z;
        Collider playerCollider = GetComponent<Collider>();
        float playerRadius = 0f;
        if (playerCollider != null)
        {
            Bounds playerBounds = playerCollider.bounds;
            playerRadius = Mathf.Abs(direction.x) * playerBounds.extents.x
                + Mathf.Abs(direction.z) * playerBounds.extents.z;
        }

        return targetRadius + playerRadius + approachGap;
    }

    private bool IsCloseEnough(Collider sphereCollider)
    {
        Vector3 difference = sphereCollider.bounds.center - transform.position;
        difference.y = 0f;
        return difference.magnitude <= GetStoppingDistance(sphereCollider) + 0.08f;
    }

    private void CancelCurrentCommand()
    {
        if (activeCommand == null)
        {
            return;
        }

        StopCoroutine(activeCommand);
        activeCommand = null;
        if (movement != null)
        {
            movement.CancelMoveTo();
        }

        ApproachCancelled?.Invoke();
    }
}
