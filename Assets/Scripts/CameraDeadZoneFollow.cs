using UnityEngine;

public class CameraDeadZoneFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float deadZoneRadius = 3f;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.25f;

    private Vector3 zoneCenter;
    private Vector3 followOffset;
    private Vector3 followVelocity;
    private bool isFollowing;

    private void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target == null)
        {
            enabled = false;
            Debug.LogWarning("CameraDeadZoneFollow needs a player target.", this);
            return;
        }

        zoneCenter = target.position;
        followOffset = transform.position - target.position;
    }

    private void LateUpdate()
    {
        Vector3 playerOffset = target.position - zoneCenter;
        playerOffset.y = 0f;
        if (!isFollowing && playerOffset.sqrMagnitude > deadZoneRadius * deadZoneRadius)
        {
            isFollowing = true;
        }

        if (isFollowing)
        {
            Vector3 desiredPosition = target.position + followOffset;
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref followVelocity, smoothTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = target == null
            ? transform.position
            : Application.isPlaying ? zoneCenter : target.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, deadZoneRadius);
    }
}
