using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Slides the existing HUD without disabling its data subscriptions or input owner.
public sealed class InventoryHudVisibility : MonoBehaviour
{
    [Serializable]
    private sealed class HudElement
    {
        public RectTransform root;
        public CanvasGroup canvasGroup;
        [NonSerialized] public Vector2 home;
        [NonSerialized] public bool interactable;
        [NonSerialized] public bool blocksRaycasts;
        [NonSerialized] public bool inputCaptured;
        [NonSerialized] public Tween tween;
    }

    [SerializeField] private HudElement[] elements = new HudElement[0];
    [SerializeField, Min(0f)] private float duration = 0.25f;
    [SerializeField, Min(0f)] private float hiddenPadding = 24f;
    [SerializeField] private Ease hideEase = Ease.InQuad;
    [SerializeField] private Ease showEase = Ease.OutQuad;

    private readonly Vector3[] corners = new Vector3[4];
    private bool initialized;
    private bool inventoryOpen;

    public void SetInventoryOpen(bool open, bool animate = true)
    {
        if (inventoryOpen == open) return;
        Canvas.ForceUpdateCanvases();
        Initialize();
        inventoryOpen = open;

        foreach (HudElement element in elements)
        {
            if (element?.root == null) continue;
            element.tween?.Kill();
            element.tween = null;

            // Capture once per open/close cycle, including interrupted transitions.
            if (open && !element.inputCaptured && element.canvasGroup != null)
            {
                element.interactable = element.canvasGroup.interactable;
                element.blocksRaycasts = element.canvasGroup.blocksRaycasts;
                element.inputCaptured = true;
            }
            if (element.canvasGroup != null)
            {
                element.canvasGroup.interactable = false;
                element.canvasGroup.blocksRaycasts = false;
            }

            Vector2 target = open ? GetHiddenPosition(element) : element.home;
            if (!animate || !isActiveAndEnabled || duration <= 0f)
            {
                element.root.anchoredPosition = target;
                if (!open) RestoreInput(element);
                continue;
            }

            element.tween = element.root.DOAnchorPos(target, duration)
                .SetEase(open ? hideEase : showEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    element.tween = null;
                    if (!inventoryOpen) RestoreInput(element);
                });
        }
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        foreach (HudElement element in elements)
        {
            if (element?.root == null) continue;
            element.home = element.root.anchoredPosition;
            if (element.canvasGroup == null) continue;
            element.interactable = element.canvasGroup.interactable;
            element.blocksRaycasts = element.canvasGroup.blocksRaycasts;
        }
    }

    private Vector2 GetHiddenPosition(HudElement element)
    {
        RectTransform root = element.root;
        Transform parent = root.parent;
        Canvas canvas = root.GetComponentInParent<Canvas>();
        RectTransform viewport = canvas != null
            ? canvas.rootCanvas.transform as RectTransform : parent as RectTransform;
        if (viewport == null) return element.home + Vector2.down * Screen.height;

        viewport.GetWorldCorners(corners);
        float bottom = parent.InverseTransformPoint(corners[0]).y;
        float top = float.NegativeInfinity;

        // HUD roots stretch over the full canvas. Measure their visual children,
        // rather than moving a full-screen root by an arbitrary fixed distance.
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            graphic.rectTransform.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
                top = Mathf.Max(top, parent.InverseTransformPoint(corner).y);
        }
        if (float.IsNegativeInfinity(top))
        {
            root.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
                top = Mathf.Max(top, parent.InverseTransformPoint(corner).y);
        }

        top += element.home.y - root.anchoredPosition.y;
        float distance = Mathf.Max(0f, top - bottom + hiddenPadding);
        return element.home + Vector2.down * distance;
    }

    private static void RestoreInput(HudElement element)
    {
        if (element.canvasGroup == null || !element.inputCaptured) return;
        element.canvasGroup.interactable = element.interactable;
        element.canvasGroup.blocksRaycasts = element.blocksRaycasts;
        element.inputCaptured = false;
    }

    private void OnDisable()
    {
        if (!initialized) return;
        foreach (HudElement element in elements)
        {
            if (element?.root == null) continue;
            element.tween?.Kill();
            element.tween = null;
            element.root.anchoredPosition = element.home;
            RestoreInput(element);
        }
        inventoryOpen = false;
    }
}
