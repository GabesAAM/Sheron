using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public sealed class TeleportEndpoint : MonoBehaviour
{
    public enum EndpointSide
    {
        A,
        B
    }

    [SerializeField] private string pairId = "SampleSceneTeleportPair";
    [SerializeField] private EndpointSide endpointSide;
    [SerializeField, Min(0.01f)] private float cellSize = 1f;

    private readonly HashSet<int> playersBlockedUntilExit = new HashSet<int>();
    private BoxCollider endpointCollider;
    private GridMapDimensions mapGrid;
    private TeleportEndpoint destination;
    private bool destinationLookupFailed;

    private void Awake()
    {
        CacheReferences();
        if (Application.isPlaying)
        {
            ResolveDestination();
        }
    }

    private void OnEnable()
    {
        CacheReferences();
        if (endpointCollider != null)
        {
            endpointCollider.isTrigger = true;
        }

        SnapToGrid();
    }

    private void OnValidate()
    {
        cellSize = Mathf.Max(0.01f, cellSize);
        CacheReferences();
        if (endpointCollider != null)
        {
            endpointCollider.isTrigger = true;
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

    private void OnTriggerEnter(Collider other)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Rigidbody playerBody = GetPlayerBody(other);
        if (playerBody == null || playersBlockedUntilExit.Contains(playerBody.GetInstanceID()))
        {
            return;
        }

        if (destination == null && !destinationLookupFailed)
        {
            ResolveDestination();
        }

        if (destination == null)
        {
            return;
        }

        PlayerBoxInteraction boxInteraction = playerBody.GetComponent<PlayerBoxInteraction>();
        MagicSphereInteraction sphereInteraction = playerBody.GetComponent<MagicSphereInteraction>();
        Rigidbody carriedBody = boxInteraction != null && boxInteraction.HeldBody != null
            ? boxInteraction.HeldBody
            : sphereInteraction != null ? sphereInteraction.HeldBody : null;
        Transform carryParent = carriedBody != null ? carriedBody.transform.parent : null;
        Vector3 carryLocalPosition = carriedBody != null
            ? carriedBody.transform.localPosition
            : Vector3.zero;
        Quaternion carryLocalRotation = carriedBody != null
            ? carriedBody.transform.localRotation
            : Quaternion.identity;
        Vector3 carryLocalScale = carriedBody != null
            ? carriedBody.transform.localScale
            : Vector3.one;

        int playerId = playerBody.GetInstanceID();
        destination.playersBlockedUntilExit.Add(playerId);

        TopDownCharacterController movement = playerBody.GetComponent<TopDownCharacterController>();
        if (movement != null)
        {
            movement.CancelMoveTo();
        }

        if (boxInteraction != null)
        {
            boxInteraction.CancelForOtherInteraction();
        }

        if (sphereInteraction != null)
        {
            sphereInteraction.CancelForOtherInteraction();
        }

        Vector3 arrivalPosition = destination.transform.position;
        if (destination.endpointCollider != null)
        {
            arrivalPosition.y = destination.endpointCollider.bounds.min.y;
        }

        playerBody.transform.position = arrivalPosition;
        playerBody.position = arrivalPosition;
        playerBody.velocity = Vector3.zero;
        playerBody.angularVelocity = Vector3.zero;
        playerBody.WakeUp();

        if (carriedBody != null)
        {
            // Keep the existing carry hierarchy intact so the interaction script
            // continues to own the item after teleporting.
            if (carriedBody.transform.parent != carryParent)
            {
                carriedBody.transform.SetParent(carryParent, false);
            }

            carriedBody.transform.localPosition = carryLocalPosition;
            carriedBody.transform.localRotation = carryLocalRotation;
            carriedBody.transform.localScale = carryLocalScale;
            carriedBody.position = carriedBody.transform.position;
            carriedBody.velocity = Vector3.zero;
            carriedBody.angularVelocity = Vector3.zero;
            carriedBody.WakeUp();
        }

        Physics.SyncTransforms();
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody playerBody = GetPlayerBody(other);
        if (playerBody != null)
        {
            playersBlockedUntilExit.Remove(playerBody.GetInstanceID());
        }
    }

    private static Rigidbody GetPlayerBody(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null)
        {
            body = other.GetComponentInParent<Rigidbody>();
        }

        return body != null && body.GetComponent<TopDownCharacterController>() != null
            ? body
            : null;
    }

    private void ResolveDestination()
    {
        destinationLookupFailed = true;
        if (string.IsNullOrWhiteSpace(pairId))
        {
            Debug.LogError("A teleport endpoint needs a pair ID.", this);
            return;
        }

        TeleportEndpoint[] endpoints = FindObjectsOfType<TeleportEndpoint>();
        for (int i = 0; i < endpoints.Length; i++)
        {
            TeleportEndpoint candidate = endpoints[i];
            if (candidate == null || candidate == this || candidate.gameObject.scene != gameObject.scene
                || candidate.pairId != pairId || candidate.endpointSide == endpointSide)
            {
                continue;
            }

            if (destination != null)
            {
                destination = null;
                Debug.LogError("Teleport pair ID '" + pairId + "' has more than two endpoints.", this);
                return;
            }

            destination = candidate;
        }

        if (destination == null)
        {
            Debug.LogError("Could not find the other endpoint for teleport pair '" + pairId + "'.", this);
            return;
        }

        destinationLookupFailed = false;
    }

    private void CacheReferences()
    {
        if (endpointCollider == null)
        {
            endpointCollider = GetComponent<BoxCollider>();
        }

        if (mapGrid == null)
        {
            mapGrid = FindObjectOfType<GridMapDimensions>();
        }
    }

    private void SnapToGrid()
    {
        if (endpointCollider == null || mapGrid == null)
        {
            return;
        }

        BoxCollider mapCollider = mapGrid.GetComponent<BoxCollider>();
        if (mapCollider == null || !GridBoxSnapToGrid.TryGetGridAlignedPosition(
            mapGrid, endpointCollider, cellSize, transform.position, mapCollider.bounds.max.y,
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
