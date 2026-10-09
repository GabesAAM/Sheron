using UnityEngine;

public class GridBoxClickSelector : MonoBehaviour
{
    [SerializeField] private Camera selectionCamera;
    [SerializeField] private PlayerBoxInteraction playerInteraction;
    [SerializeField] private float rayDistance = 500f;
    [SerializeField] private float doubleClickInterval = 0.35f;

    private ClickHighlightTarget selectedTarget;
    private ClickHighlightTarget lastClickedTarget;
    private float lastClickTime = -1f;

    private void Awake()
    {
        if (selectionCamera == null)
        {
            selectionCamera = GetComponent<Camera>();
        }
    }

    private void Update()
    {
        if (selectionCamera == null || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        Ray ray = selectionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        ClickHighlightTarget clickedTarget = null;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            clickedTarget = hit.collider.GetComponentInParent<ClickHighlightTarget>();
        }

        UpdateHighlight(clickedTarget);

        if (playerInteraction == null)
        {
            return;
        }

        if (playerInteraction.HeldTarget != null)
        {
            if (clickedTarget != null && clickedTarget != playerInteraction.HeldTarget)
            {
                playerInteraction.ThrowHeldOn(clickedTarget);
            }

            lastClickedTarget = null;
            lastClickTime = -1f;
            return;
        }

        bool isDoubleClick = clickedTarget != null
            && clickedTarget == lastClickedTarget
            && Time.unscaledTime - lastClickTime <= doubleClickInterval;

        if (isDoubleClick)
        {
            lastClickedTarget = null;
            lastClickTime = -1f;
            playerInteraction.PickUp(clickedTarget);
        }
        else
        {
            lastClickedTarget = clickedTarget;
            lastClickTime = clickedTarget != null ? Time.unscaledTime : -1f;
        }
    }

    private void UpdateHighlight(ClickHighlightTarget target)
    {
        if (target == selectedTarget)
        {
            return;
        }

        if (selectedTarget != null)
        {
            selectedTarget.SetHighlighted(false);
        }

        selectedTarget = target;

        if (selectedTarget != null)
        {
            selectedTarget.SetHighlighted(true);
        }
    }
}
