using UnityEngine;
using UnityEngine.UI;

public class CrosshairView : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform cursorRoot;

    [SerializeField] private Image normalCrosshair;
    [SerializeField] private Image hitMarker;

    [SerializeField] private GameObject reloadRoot;
    [SerializeField] private Image progressCircle;

    public void SetScreenPosition(Vector2 screenPosition)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPosition))
        {
            cursorRoot.anchoredPosition = localPosition;
        }
    }

    public void ShowCrosshair(
        bool visible,
        bool reloading,
        float hitAlpha,
        float progress)
    {
        cursorRoot.gameObject.SetActive(visible);

        normalCrosshair.enabled = visible && !reloading;
        reloadRoot.SetActive(visible && reloading);

        Color color = hitMarker.color;
        color.a = hitAlpha;
        hitMarker.color = color;
        hitMarker.enabled = visible && hitAlpha > 0f;

        progressCircle.fillAmount = Mathf.Clamp01(progress);
    }

    public void Hide()
    {
        if (cursorRoot != null) cursorRoot.gameObject.SetActive(false);
    }
}
