using UnityEngine;

public class GridBoxClickSelector : MonoBehaviour
{
    [SerializeField] private Camera selectionCamera;
    [SerializeField] private PlayerBoxInteraction playerInteraction;
    private MagicSphereInteraction magicSphereInteraction;
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

        if (playerInteraction != null)
        {
            playerInteraction.ApproachCancelled += ClearSelection;
            magicSphereInteraction = playerInteraction.GetComponent<MagicSphereInteraction>();
        }

        if (magicSphereInteraction != null)
        {
            magicSphereInteraction.ApproachCancelled += ClearSelection;
        }
    }

    private void OnDestroy()
    {
        if (playerInteraction != null)
        {
            playerInteraction.ApproachCancelled -= ClearSelection;
        }

        if (magicSphereInteraction != null)
        {
            magicSphereInteraction.ApproachCancelled -= ClearSelection;
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

        if (magicSphereInteraction != null && magicSphereInteraction.IsHoldingSphere)
        {
            if (clickedTarget != null && clickedTarget.GetComponent<GridBoxSnapToGrid>() != null)
            {
                magicSphereInteraction.ThrowAt(clickedTarget);
            }

            ResetClickSequence();
            return;
        }

        if (playerInteraction == null)
        {
            return;
        }

        if (playerInteraction.HeldTarget != null)
        {
            if (clickedTarget != null && clickedTarget != playerInteraction.HeldTarget
                && clickedTarget.GetComponent<GridBoxSnapToGrid>() != null)
            {
                playerInteraction.ThrowHeldOn(clickedTarget);
            }

            ResetClickSequence();
            return;
        }

        if (magicSphereInteraction != null && magicSphereInteraction.IsApproaching)
        {
            ResetClickSequence();
            return;
        }

        bool isDoubleClick = clickedTarget != null
            && clickedTarget == lastClickedTarget
            && Time.unscaledTime - lastClickTime <= doubleClickInterval;

        if (isDoubleClick)
        {
            lastClickedTarget = null;
            lastClickTime = -1f;
            MagicSphere sphere = clickedTarget.GetComponentInParent<MagicSphere>();
            if (sphere != null && magicSphereInteraction != null)
            {
                magicSphereInteraction.PickUp(sphere);
            }
            else if (clickedTarget.GetComponent<GridBoxSnapToGrid>() != null)
            {
                playerInteraction.PickUp(clickedTarget);
            }
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
            if (target != null && !target.IsHighlighted)
            {
                target.SetHighlighted(true);
            }

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

    private void ClearSelection()
    {
        UpdateHighlight(null);
        lastClickedTarget = null;
        lastClickTime = -1f;
    }

    private void ResetClickSequence()
    {
        lastClickedTarget = null;
        lastClickTime = -1f;
    }
}
