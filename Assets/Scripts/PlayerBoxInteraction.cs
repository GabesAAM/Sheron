using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBoxInteraction : MonoBehaviour
{
    public event System.Action ApproachCancelled;

    [SerializeField] private TopDownCharacterController movement;
    [SerializeField] private Transform carryAnchor;
    [SerializeField] private float approachGap = 0.55f;
    [SerializeField] private float approachTimeout = 12f;
    [SerializeField] private float throwSpeed = 6f;

    private Rigidbody heldBody;
    private Collider heldCollider;
    private ClickHighlightTarget heldTarget;
    private Coroutine activeCommand;
    private bool approachSucceeded;

    public ClickHighlightTarget HeldTarget
    {
        get { return heldTarget; }
    }

    private void Awake()
    {
        if (movement == null)
        {
            movement = GetComponent<TopDownCharacterController>();
        }
    }

    private void Update()
    {
        bool spacePressed = Input.GetKeyDown(KeyCode.Space);
        bool movementKeyPressed = Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.S)
            || Input.GetKeyDown(KeyCode.D);

        if (spacePressed || movementKeyPressed)
        {
            CancelCurrentCommand();
        }

        if (heldBody != null && spacePressed)
        {
            DropHeldInFront();
        }
    }

    private void CancelCurrentCommand()
    {
        bool hadActiveCommand = activeCommand != null;
        if (activeCommand != null)
        {
            StopCoroutine(activeCommand);
            activeCommand = null;
        }

        if (movement != null)
        {
            movement.CancelMoveTo();
        }

        approachSucceeded = false;
        if (hadActiveCommand && ApproachCancelled != null)
        {
            ApproachCancelled();
        }
    }

    public void PickUp(ClickHighlightTarget target)
    {
        if (target == null || heldBody != null || carryAnchor == null || movement == null)
        {
            return;
        }

        GridBoxSnapToGrid gridSnap = target.GetComponent<GridBoxSnapToGrid>();
        BoxCollider clickedBox = target.GetComponent<BoxCollider>();
        if (gridSnap != null && clickedBox != null)
        {
            ClickHighlightTarget topmostTarget = gridSnap.GetTopmostBoxInStack(clickedBox);
            if (topmostTarget != null)
            {
                target = topmostTarget;
            }
        }

        StartCommand(ApproachAndPickUp(target));
    }

    public void ThrowHeldOn(ClickHighlightTarget target)
    {
        if (target == null || target == heldTarget || heldBody == null || movement == null)
        {
            return;
        }

        StartCommand(ApproachAndThrow(target));
    }

    private void DropHeldInFront()
    {
        BoxCollider boxCollider = heldCollider as BoxCollider;
        GridBoxSnapToGrid gridSnap = heldTarget != null
            ? heldTarget.GetComponent<GridBoxSnapToGrid>()
            : null;
        if (boxCollider == null || gridSnap == null || movement == null)
        {
            return;
        }

        CancelCurrentCommand();

        Vector3 facingDirection = movement.FacingDirection;
        facingDirection.y = 0f;
        if (facingDirection.sqrMagnitude < 0.0001f)
        {
            facingDirection = Vector3.forward;
        }
        facingDirection.Normalize();

        Bounds boxBounds = GridBoxSnapToGrid.GetWorldBounds(boxCollider);
        float boxRadius = Mathf.Abs(facingDirection.x) * boxBounds.extents.x
            + Mathf.Abs(facingDirection.z) * boxBounds.extents.z;
        Collider playerCollider = GetComponent<Collider>();
        float playerRadius = 0.35f;
        if (playerCollider != null)
        {
            Bounds playerBounds = playerCollider.bounds;
            playerRadius = Mathf.Abs(facingDirection.x) * playerBounds.extents.x
                + Mathf.Abs(facingDirection.z) * playerBounds.extents.z;
        }

        Vector3 desiredPosition = transform.position
            + facingDirection * (playerRadius + boxRadius + 0.1f);
        Vector3 placementPosition;
        if (!gridSnap.TryFindNearestEmptyGridPosition(
            boxCollider, desiredPosition, transform.position, facingDirection, out placementPosition))
        {
            Debug.LogWarning("There is no empty grid space available for the held box.", this);
            return;
        }

        Transform boxTransform = heldBody.transform;
        boxTransform.SetParent(null, true);
        boxTransform.position = placementPosition;
        boxTransform.rotation = Quaternion.identity;
        heldCollider.enabled = true;
        heldBody.velocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        heldBody.useGravity = false;
        heldBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        heldBody.interpolation = RigidbodyInterpolation.Interpolate;
        heldBody.isKinematic = true;
        Physics.SyncTransforms();

        heldBody = null;
        heldCollider = null;
        heldTarget = null;
        gridSnap.CheckForMatchingStack();
    }

    private void StartCommand(IEnumerator command)
    {
        if (activeCommand != null)
        {
            StopCoroutine(activeCommand);
        }

        movement.CancelMoveTo();
        activeCommand = StartCoroutine(command);
    }

    private IEnumerator ApproachAndPickUp(ClickHighlightTarget target)
    {
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider == null || target.GetComponent<Rigidbody>() == null)
        {
            Debug.LogWarning("The selected box needs a Collider and Rigidbody to be picked up.", target);
            yield break;
        }

        yield return ApproachTarget(target, targetCollider);
        if (!approachSucceeded || target == null)
        {
            yield break;
        }

        Rigidbody boxBody = target.GetComponent<Rigidbody>();
        heldTarget = target;
        heldBody = boxBody;
        heldCollider = targetCollider;

        heldBody.velocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        heldBody.useGravity = false;
        heldBody.isKinematic = true;
        heldBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        heldBody.interpolation = RigidbodyInterpolation.None;
        heldCollider.enabled = false;
        heldBody.transform.SetParent(carryAnchor, false);
        heldBody.transform.localPosition = Vector3.zero;
        heldBody.transform.localRotation = Quaternion.identity;
    }

    private IEnumerator ApproachAndThrow(ClickHighlightTarget target)
    {
        Collider targetCollider = target.GetComponent<Collider>();
        if (targetCollider == null)
        {
            Debug.LogWarning("The target box needs a Collider.", target);
            yield break;
        }

        yield return ApproachTarget(target, targetCollider);
        if (!approachSucceeded || target == null || heldBody == null)
        {
            yield break;
        }

        ThrowOnTarget(targetCollider);
    }

    private IEnumerator ApproachTarget(ClickHighlightTarget target, Collider targetCollider)
    {
        approachSucceeded = false;
        Bounds initialBounds = targetCollider.bounds;
        float stoppingDistance = GetStoppingDistance(initialBounds);
        movement.MoveTo(initialBounds.center, stoppingDistance);

        float elapsed = 0f;
        while (target != null && targetCollider != null && !IsCloseEnough(targetCollider))
        {
            elapsed += Time.deltaTime;
            if (elapsed >= approachTimeout)
            {
                movement.CancelMoveTo();
                Debug.LogWarning("The player could not reach the selected box. Check for blocked paths.", target);
                yield break;
            }

            yield return null;
        }

        movement.CancelMoveTo();
        approachSucceeded = target != null && targetCollider != null && IsCloseEnough(targetCollider);
    }

    private float GetStoppingDistance(Bounds targetBounds)
    {
        Vector3 direction = targetBounds.center - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }
        else
        {
            direction.Normalize();
        }

        float targetRadius = Mathf.Abs(direction.x) * targetBounds.extents.x
            + Mathf.Abs(direction.z) * targetBounds.extents.z;
        return targetRadius + approachGap;
    }

    private bool IsCloseEnough(Collider targetCollider)
    {
        Bounds bounds = targetCollider.bounds;
        Vector3 difference = bounds.center - transform.position;
        difference.y = 0f;
        return difference.magnitude <= GetStoppingDistance(bounds) + 0.08f;
    }

    private void ThrowOnTarget(Collider targetCollider)
    {
        Rigidbody thrownBody = heldBody;
        BoxCollider thrownBox = heldCollider as BoxCollider;
        GridBoxSnapToGrid thrownGrid = heldTarget != null
            ? heldTarget.GetComponent<GridBoxSnapToGrid>()
            : null;
        if (thrownGrid != null)
        {
            thrownGrid.BeginTargetedThrow();
        }

        Transform boxTransform = thrownBody.transform;
        Vector3 launchPosition = carryAnchor.position;

        boxTransform.SetParent(null, true);
        boxTransform.position = launchPosition;
        boxTransform.rotation = Quaternion.identity;
        heldCollider.enabled = true;
        heldBody.useGravity = true;
        heldBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        heldBody.interpolation = RigidbodyInterpolation.Interpolate;
        heldBody.isKinematic = false;
        heldBody.velocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();

        Bounds boxBounds = heldCollider.bounds;
        Bounds targetBounds = targetCollider.bounds;
        Vector3 landingPosition = targetBounds.center;
        float stackTop = targetBounds.max.y;
        int stackHeightInCells = 1;
        GridBoxSnapToGrid targetGrid = targetCollider.GetComponent<GridBoxSnapToGrid>();
        BoxCollider targetBox = targetCollider as BoxCollider;
        if (targetGrid != null && targetBox != null)
        {
            stackHeightInCells = targetGrid.GetStackHeightInCells(targetBox, thrownBox, out stackTop);
        }

        landingPosition.y = stackTop + boxBounds.extents.y + 0.03f;
        if (thrownGrid != null && thrownBox != null)
        {
            Vector3 gridLandingPosition;
            if (thrownGrid.TryGetGridAlignedPosition(thrownBox, landingPosition, stackTop, out gridLandingPosition))
            {
                landingPosition = gridLandingPosition;
            }
        }

        Vector3 horizontalOffset = landingPosition - launchPosition;
        horizontalOffset.y = 0f;
        float flightTime = Mathf.Max(0.45f, horizontalOffset.magnitude / throwSpeed);
        float gravityStrength = -Physics.gravity.y;
        if (gravityStrength > 0.001f)
        {
            float verticalDisplacement = landingPosition.y - launchPosition.y;
            float arcClearance = 0.5f + Mathf.Max(0, stackHeightInCells - 1) * 0.45f;
            float heightToApex = Mathf.Max(0.5f, verticalDisplacement + arcClearance);
            float verticalSpeedAtApex = Mathf.Sqrt(2f * gravityStrength * heightToApex);
            float descendingDiscriminant = Mathf.Max(
                0f,
                verticalSpeedAtApex * verticalSpeedAtApex - 2f * gravityStrength * verticalDisplacement);
            float descendingTime = (verticalSpeedAtApex + Mathf.Sqrt(descendingDiscriminant))
                / gravityStrength;
            flightTime = Mathf.Max(flightTime, descendingTime);
        }

        thrownBody.velocity = (landingPosition - launchPosition
            - 0.5f * Physics.gravity * flightTime * flightTime) / flightTime;

        List<Collider> ignoredStackColliders = IgnoreTargetColumnCollisions(thrownBox, targetCollider);

        StartCoroutine(StopBoxAtTargetTop(
            thrownBody, thrownBox, targetCollider, thrownGrid, targetGrid, flightTime, landingPosition, stackTop,
            ignoredStackColliders));

        heldBody = null;
        heldCollider = null;
        heldTarget = null;
    }

    private IEnumerator StopBoxAtTargetTop(
        Rigidbody thrownBody,
        BoxCollider thrownBox,
        Collider targetCollider,
        GridBoxSnapToGrid thrownGrid,
        GridBoxSnapToGrid targetGrid,
        float flightTime,
        Vector3 landingPosition,
        float stackTop,
        List<Collider> ignoredStackColliders)
    {
        yield return new WaitForSeconds(flightTime);
        if (thrownBody == null || thrownBox == null)
        {
            yield break;
        }

        if (targetCollider != null)
        {
            Bounds targetBounds = targetCollider.bounds;
            landingPosition.x = targetBounds.center.x;
            landingPosition.z = targetBounds.center.z;
            stackTop = targetBounds.max.y;
            BoxCollider targetBox = targetCollider as BoxCollider;
            if (targetGrid != null && targetBox != null)
            {
                targetGrid.GetStackHeightInCells(targetBox, thrownBox, out stackTop);
            }
        }

        Vector3 finalPosition = landingPosition;
        if (thrownGrid != null
            && !thrownGrid.TryGetGridAlignedPosition(thrownBox, landingPosition, stackTop, out finalPosition))
        {
            finalPosition = landingPosition;
        }

        thrownBody.transform.position = finalPosition;
        thrownBody.transform.rotation = Quaternion.identity;
        thrownBody.velocity = Vector3.zero;
        thrownBody.angularVelocity = Vector3.zero;
        thrownBody.useGravity = false;
        thrownBody.isKinematic = true;
        Physics.SyncTransforms();

        RestoreTargetColumnCollisions(thrownBox, ignoredStackColliders);

        if (thrownGrid != null)
        {
            thrownGrid.EndTargetedThrow();
            thrownGrid.CheckForMatchingStack();
        }
    }

    private static List<Collider> IgnoreTargetColumnCollisions(BoxCollider thrownBox, Collider targetCollider)
    {
        List<Collider> ignoredColliders = new List<Collider>();
        if (thrownBox == null || targetCollider == null)
        {
            return ignoredColliders;
        }

        Bounds targetBounds = targetCollider.bounds;
        GridBoxSnapToGrid[] boxes = FindObjectsOfType<GridBoxSnapToGrid>();
        for (int i = 0; i < boxes.Length; i++)
        {
            GridBoxSnapToGrid gridBox = boxes[i];
            if (gridBox == null || gridBox == thrownBox.GetComponent<GridBoxSnapToGrid>())
            {
                continue;
            }

            BoxCollider stackBox = gridBox.GetComponent<BoxCollider>();
            if (stackBox == null || !stackBox.enabled)
            {
                continue;
            }

            Bounds stackBounds = GridBoxSnapToGrid.GetWorldBounds(stackBox);
            bool sameColumn = Mathf.Abs(stackBounds.center.x - targetBounds.center.x) < 0.05f
                && Mathf.Abs(stackBounds.center.z - targetBounds.center.z) < 0.05f;
            if (!sameColumn)
            {
                continue;
            }

            Physics.IgnoreCollision(thrownBox, stackBox, true);
            ignoredColliders.Add(stackBox);
        }

        return ignoredColliders;
    }

    private static void RestoreTargetColumnCollisions(BoxCollider thrownBox, List<Collider> ignoredColliders)
    {
        if (thrownBox == null || ignoredColliders == null)
        {
            return;
        }

        for (int i = 0; i < ignoredColliders.Count; i++)
        {
            Collider ignoredCollider = ignoredColliders[i];
            if (ignoredCollider != null)
            {
                Physics.IgnoreCollision(thrownBox, ignoredCollider, false);
            }
        }
    }
}
