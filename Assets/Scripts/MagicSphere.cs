using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public sealed class MagicSphere : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("Minimum distance between the sphere's bottom and the map surface while idle.")]
    private float hoverHeight = 0.25f;
    [SerializeField, Min(0f), Tooltip("Maximum bob distance above the configured hover height.")]
    private float bobbingAmplitude = 0.08f;
    [SerializeField, Min(0f), Tooltip("Bobbing rate in radians per second.")]
    private float bobbingSpeed = 2f;

    private SphereCollider sphereCollider;
    private Rigidbody body;
    private Renderer sphereRenderer;
    private GridMapDimensions mapGrid;
    private BoxCollider mapCollider;
    private float fallbackGroundSurfaceY;
    private GridBoxSnapToGrid targetBox;
    private bool isProjectile;
    private bool consumed;

    public bool IsAvailable
    {
        get { return !consumed && !isProjectile && transform.parent == null; }
    }

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        body = GetComponent<Rigidbody>();
        sphereRenderer = GetComponent<Renderer>();
        mapGrid = FindObjectOfType<GridMapDimensions>();
        mapCollider = mapGrid != null ? mapGrid.GetComponent<BoxCollider>() : null;
        fallbackGroundSurfaceY = sphereCollider.bounds.min.y;
        sphereCollider.isTrigger = false;
    }

    private void Update()
    {
        if (!Application.isPlaying || !IsAvailable || sphereCollider == null)
        {
            return;
        }

        float groundSurfaceY = mapCollider != null
            ? mapCollider.bounds.max.y
            : fallbackGroundSurfaceY;
        Vector3 position = transform.position;
        float colliderCenterOffset = transform.TransformPoint(sphereCollider.center).y - position.y;
        float worldRadius = sphereCollider.radius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z));
        float bob = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmplitude;
        bob = Mathf.Max(-hoverHeight, bob);
        position.y = groundSurfaceY + hoverHeight + bob + worldRadius - colliderCenterOffset;
        transform.position = position;
    }

    public void CarryBy(Transform carryAnchor)
    {
        if (!IsAvailable || carryAnchor == null)
        {
            return;
        }

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.isKinematic = true;
        sphereCollider.enabled = false;
        transform.SetParent(carryAnchor, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void LaunchAt(GridBoxSnapToGrid target, Vector3 velocity)
    {
        if (consumed || target == null)
        {
            return;
        }

        targetBox = target;
        isProjectile = true;
        transform.SetParent(null, true);
        sphereCollider.enabled = true;
        sphereCollider.isTrigger = true;
        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.velocity = velocity;
    }

    public void ReleaseAt(Vector3 worldPosition)
    {
        if (consumed || isProjectile)
        {
            return;
        }

        transform.SetParent(null, true);
        transform.position = worldPosition;
        transform.rotation = Quaternion.identity;
        sphereCollider.enabled = true;
        sphereCollider.isTrigger = false;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.isKinematic = true;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        Physics.SyncTransforms();
    }

    public void ApplyTo(GridBoxSnapToGrid box)
    {
        if (consumed || box == null)
        {
            return;
        }

        ClickHighlightTarget boxHighlight = box.GetComponent<ClickHighlightTarget>();
        if (boxHighlight != null)
        {
            boxHighlight.SetHighlighted(false);
        }

        Material material = sphereRenderer != null ? sphereRenderer.sharedMaterial : null;
        if (material != null)
        {
            ClickHighlightTarget highlightTarget = box.GetComponent<ClickHighlightTarget>();
            if (highlightTarget != null)
            {
                highlightTarget.SetBaseMaterial(material);
            }
            else
            {
                Renderer boxRenderer = box.GetComponent<Renderer>();
                if (boxRenderer != null)
                {
                    boxRenderer.sharedMaterial = material;
                }
            }

            box.CheckForMatchingStack();
        }

        consumed = true;
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isProjectile || targetBox == null)
        {
            return;
        }

        GridBoxSnapToGrid hitBox = other.GetComponentInParent<GridBoxSnapToGrid>();
        if (hitBox == targetBox)
        {
            ApplyTo(targetBox);
        }
    }
}
