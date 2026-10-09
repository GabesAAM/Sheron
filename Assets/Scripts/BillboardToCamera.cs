using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 directionToCamera = targetCamera.transform.position - transform.position;
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(directionToCamera, Vector3.up);
        }
    }
}
