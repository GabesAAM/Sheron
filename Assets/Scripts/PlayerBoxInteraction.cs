using System.Collections;
using UnityEngine;

public class PlayerBoxInteraction : MonoBehaviour
{
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

    public void PickUp(ClickHighlightTarget target)
    {
        if (target == null || heldBody != null || carryAnchor == null || movement == null)
        {
            return;
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
        Transform boxTransform = heldBody.transform;
        Vector3 launchPosition = carryAnchor.position;

        boxTransform.SetParent(null, true);
        boxTransform.position = launchPosition;
        boxTransform.rotation = Quaternion.identity;
        heldCollider.enabled = true;
        heldBody.useGravity = true;
        heldBody.isKinematic = false;
        heldBody.velocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();

        Bounds boxBounds = heldCollider.bounds;
        Bounds targetBounds = targetCollider.bounds;
        Vector3 landingPosition = targetBounds.center;
        landingPosition.y = targetBounds.max.y + boxBounds.extents.y + 0.03f;

        Vector3 horizontalOffset = landingPosition - launchPosition;
        horizontalOffset.y = 0f;
        float flightTime = Mathf.Clamp(horizontalOffset.magnitude / throwSpeed, 0.45f, 1.1f);
        heldBody.velocity = (landingPosition - launchPosition
            - 0.5f * Physics.gravity * flightTime * flightTime) / flightTime;

        heldBody = null;
        heldCollider = null;
        heldTarget = null;
    }
}
