using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TopDownCharacterController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private BoxCollider mapCollider;
    [SerializeField] private float mapEdgePadding = 0.6f;

    private Rigidbody body;
    private PlayerStats playerStats;
    private Camera movementCamera;
    private Vector2 movementInput;
    private bool hasMoveDestination;
    private Vector3 moveDestination;
    private float destinationStoppingDistance;
    private Vector3 facingDirection = Vector3.forward;
    private Vector3 knockbackDirection;
    private float knockbackSpeed;
    private float knockbackTimeRemaining;

    public Vector3 FacingDirection
    {
        get { return facingDirection; }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        playerStats = GetComponent<PlayerStats>();
        movementCamera = Camera.main;
        if (movementCamera != null)
        {
            Vector3 directionToCamera = movementCamera.transform.position - transform.position;
            directionToCamera.y = 0f;
            if (directionToCamera.sqrMagnitude > 0.0001f)
            {
                facingDirection = directionToCamera.normalized;
            }
        }
    }

    private void Update()
    {
        movementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        if (movementInput.sqrMagnitude > 1f)
        {
            movementInput.Normalize();
        }
    }

    public void MoveTo(Vector3 position, float stoppingDistance)
    {
        moveDestination = position;
        destinationStoppingDistance = Mathf.Max(0f, stoppingDistance);
        hasMoveDestination = true;
    }

    public void CancelMoveTo()
    {
        hasMoveDestination = false;
    }

    public void ApplyKnockback(Vector3 direction, float distance, float duration)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f || distance <= 0f || duration <= 0f)
        {
            return;
        }

        CancelMoveTo();
        knockbackDirection = direction.normalized;
        knockbackSpeed = distance / duration;
        knockbackTimeRemaining = duration;
    }

    private void FixedUpdate()
    {
        Vector3 moveDirection;
        bool isKnockingBack = knockbackTimeRemaining > 0f;

        if (isKnockingBack)
        {
            moveDirection = knockbackDirection;
        }
        else if (hasMoveDestination)
        {
            Vector3 toDestination = moveDestination - body.position;
            toDestination.y = 0f;

            if (toDestination.sqrMagnitude <= destinationStoppingDistance * destinationStoppingDistance)
            {
                hasMoveDestination = false;
                moveDirection = Vector3.zero;
            }
            else
            {
                moveDirection = toDestination.normalized;
            }
        }
        else
        {
            moveDirection = new Vector3(movementInput.x, 0f, movementInput.y);

            if (movementCamera != null)
            {
                Vector3 cameraRight = Vector3.ProjectOnPlane(movementCamera.transform.right, Vector3.up).normalized;
                Vector3 cameraForward = Vector3.ProjectOnPlane(movementCamera.transform.forward, Vector3.up).normalized;
                moveDirection = cameraRight * movementInput.x + cameraForward * movementInput.y;
            }

            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }
        }

        float effectiveMoveSpeed = isKnockingBack
            ? knockbackSpeed
            : moveSpeed + (playerStats != null ? playerStats.MovementSpeedBonus : 0f);
        float movementStepTime = isKnockingBack
            ? Mathf.Min(Time.fixedDeltaTime, knockbackTimeRemaining)
            : Time.fixedDeltaTime;
        Vector3 targetPosition = body.position + moveDirection * effectiveMoveSpeed * movementStepTime;

        if (!isKnockingBack && moveDirection.sqrMagnitude > 0.0001f)
        {
            facingDirection = moveDirection.normalized;
        }

        if (mapCollider != null)
        {
            Bounds bounds = mapCollider.bounds;
            float xPadding = Mathf.Min(mapEdgePadding, Mathf.Max(0f, bounds.extents.x - 0.3f));
            float zPadding = Mathf.Min(mapEdgePadding, Mathf.Max(0f, bounds.extents.z - 0.3f));
            targetPosition.x = Mathf.Clamp(targetPosition.x, bounds.min.x + xPadding, bounds.max.x - xPadding);
            targetPosition.z = Mathf.Clamp(targetPosition.z, bounds.min.z + zPadding, bounds.max.z - zPadding);
        }

        body.MovePosition(targetPosition);

        if (isKnockingBack)
        {
            knockbackTimeRemaining = Mathf.Max(0f, knockbackTimeRemaining - Time.fixedDeltaTime);
        }
    }
}
