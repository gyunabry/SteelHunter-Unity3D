using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusUI : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField] private CharacterHealth health;
    [SerializeField] private CharacterSurvival survival;

    [Header("HP UI")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpText;

    [Header("수분도 UI")]
    [SerializeField] private Image hydrationFill;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Bind(CharacterHealth newHealth, CharacterSurvival newSurvival)
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (health != null) health.HealthChanged += HandleHealthChanged;
        if (survival != null) survival.HydrationChanged += HandleHydrationChanged;
    }

    private void Unsubscribe()
    {
        if (health != null) health.HealthChanged -= HandleHealthChanged;
        if (survival != null) survival.HydrationChanged -= HandleHydrationChanged;
    }

    private void Refresh()
    {
        HandleHealthChanged(health.CurrentHealth, health.MaxHealth);
        HandleHydrationChanged(survival.Hydration, survival.MaxHydration);
    }

    private void HandleHealthChanged(float current, float max)
    {
        float ratio = Mathf.Clamp01(current / max);

        if (hpFill != null) hpFill.fillAmount = ratio;
        if (hpText != null) hpText.text = $"{current:0.0} / {max:0.0}";
    }

    private void HandleHydrationChanged(float current, float max)
    {
        float ratio = Mathf.Clamp01(current / max);

        if (hydrationFill != null) hydrationFill.fillAmount = ratio;
    }
}
