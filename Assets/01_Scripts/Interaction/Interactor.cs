using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Interactor : MonoBehaviour
{
    [SerializeField] private InteractionPromptView view;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private float range = 5f;

    private IInteractable target;
    private MonoBehaviour targetComponent;
    private bool blocked;

    public event Action<IInteractable> Interacted;

    private bool HasTarget =>
        targetComponent != null &&
        targetComponent.isActiveAndEnabled &&
        target != null;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        view.SetVisible(false);
    }

    private void Update()
    {
        ClearTarget();

        if (blocked)
            return;

        FindTarget();

        if (HasTarget &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            target.OnInteract();

            Interacted?.Invoke(target);
        }
    }

    private void LateUpdate()
    {
        if (blocked || !HasTarget || target.PromptAnchor == null)
        {
            view.SetVisible(false);
            return;
        }

        view.Show(target.PromptAnchor.position, worldCamera, target.PromptText);
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            range,
            interactableMask,
            QueryTriggerInteraction.Collide);

        float nearestDistance = float.PositiveInfinity;

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform))
                continue;

            IInteractable candidate = hit.GetComponentInParent<IInteractable>();

            MonoBehaviour component = candidate as MonoBehaviour;

            if (component == null ||
                !component.isActiveAndEnabled ||
                !candidate.CanInteract())
            {
                continue;
            }

            float distance = (hit.ClosestPoint(transform.position) - transform.position).sqrMagnitude;

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            target = candidate;
            targetComponent = component;
        }
    }

    public bool IsInRange(IInteractable candidate)
    {
        MonoBehaviour component = candidate as MonoBehaviour;
        if (component == null || !component.isActiveAndEnabled ||
            !candidate.CanInteract())
            return false;

        Collider[] hits = Physics.OverlapSphere(
            transform.position, range, interactableMask,
            QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;
            if (ReferenceEquals(hit.GetComponentInParent<IInteractable>(), candidate))
                return true;
        }
        return false;
    }

    public void SetBlock(bool value)
    {
        blocked = value;

        if (blocked)
        {
            ClearTarget();
            view.SetVisible(false);
        }
    }

    private void ClearTarget()
    {
        target = null;
        targetComponent = null;
    }

    private void OnDisable()
    {
        ClearTarget();

        if (view != null)
            view.SetVisible(false);
    }
}