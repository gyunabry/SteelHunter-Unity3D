using TMPro;
using UnityEngine;

public class InteractionPromptView : MonoBehaviour
{
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TMP_Text promptText;

    [SerializeField] private Vector2 screenOffset = new(0f, 12f);

    public void Show(Vector3 worldPosition, Camera worldCamera, string text)
    {
        if (worldCamera == null)
        {
            Debug.LogWarning("Camera가 없습니다.");
            SetVisible(false);
            return;
        }

        Vector3 viewport = worldCamera.WorldToViewportPoint(worldPosition);

        // 화면 밖이면 비활성화
        if (viewport.z <= 0f ||
            viewport.x < 0f || viewport.x > 1f ||
            viewport.y < 0f || viewport.y > 1f)
        {
            SetVisible(false);
            return;
        }

        Vector2 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);

        screenPosition += screenOffset;

        RectTransform parent = (RectTransform)promptRoot.parent;

        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            screenPosition,
            null,
            out Vector2 localPosition);

        if (!converted)
        {
            SetVisible(false);
            return;
        }

        promptRoot.position = parent.TransformPoint(
            new Vector3(localPosition.x, localPosition.y, 0f));

        if (promptText != null)
            promptText.text = text ?? string.Empty;

        SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        if (promptRoot.gameObject.activeSelf != visible)
        {
            promptRoot.gameObject.SetActive(visible);
        }
    }
}