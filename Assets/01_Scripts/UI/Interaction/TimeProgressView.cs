using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeProgressView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text remainingTimeText;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        remainingTimeText.richText = true;
        progressFill.fillAmount = 0f;

        Hide();
    }

    public void Show(string status, float progress, float remainingTime)
    {
        statusText.text = status;
        progressFill.fillAmount = Mathf.Clamp01(progress);

        UpdateRemainingTime(remainingTime);

        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void UpdateRemainingTime(float remainingTime)
    {
        int totalMilliseconds = Mathf.CeilToInt(
            Mathf.Max(0f, remainingTime) * 1000f);

        int minutes = totalMilliseconds / 60000;
        int seconds = (totalMilliseconds / 1000) % 60;
        int milliseconds = totalMilliseconds % 1000;

        remainingTimeText.text =
            $"{minutes:00}:{seconds:00}" +
            $"<size=60%>.{milliseconds:000}</size>";
    }

    private void SetVisible(bool visible)
    {
        if (panel != null && panel.activeSelf != visible)
        {
            panel.SetActive(visible);
        }
    }

    private void OnDisable()
    {
        Hide();
    }
}
