using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TopDownCharacterController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private BoxCollider mapCollider;
    [SerializeField] private float mapEdgePadding = 0.6f;

    private Rigidbody body;
    private Camera movementCamera;
    private Vector2 movementInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        movementCamera = Camera.main;
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

    private void FixedUpdate()
    {
        Vector3 moveDirection = new Vector3(movementInput.x, 0f, movementInput.y);

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

        Vector3 targetPosition = body.position + moveDirection * moveSpeed * Time.fixedDeltaTime;

        if (mapCollider != null)
        {
            Bounds bounds = mapCollider.bounds;
            targetPosition.x = Mathf.Clamp(targetPosition.x, bounds.min.x + mapEdgePadding, bounds.max.x - mapEdgePadding);
            targetPosition.z = Mathf.Clamp(targetPosition.z, bounds.min.z + mapEdgePadding, bounds.max.z - mapEdgePadding);
        }

        body.MovePosition(targetPosition);
    }
}
