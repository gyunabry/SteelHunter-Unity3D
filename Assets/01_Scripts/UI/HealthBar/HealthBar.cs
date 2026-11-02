using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private CharacterHealth health;
    [SerializeField] private HealthBarHitFeedback feedback;
    [SerializeField] private Image fill;
    [SerializeField] private Image lateFill;

    [Header("피격 UI 잔상")]
    [SerializeField, Min(0.01f)] private float minimumTrailSpeed = 0.45f;
    [SerializeField, Min(0.01f)] private float trailCatchupTime = 0.2f;

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<CharacterHealth>();
    }

    private void OnEnable()
    {
        health.HealthChanged += HandleHealthChanged;
        health.HitFeedbackRequested += HandleHitFeedback;
        SyncFill();
    }

    private void Start()
    {
        SyncFill();
    }

    private void OnDisable()
    {
        if (health != null) 
        {
            health.HealthChanged -= HandleHealthChanged;
            health.HitFeedbackRequested -= HandleHitFeedback;
        }
    }

    private void Update()
    {
        float gap = Mathf.Max(0f, lateFill.fillAmount - fill.fillAmount);

        float speed = Mathf.Max(minimumTrailSpeed, gap / trailCatchupTime);

        lateFill.fillAmount = Mathf.MoveTowards(lateFill.fillAmount, fill.fillAmount, speed * Time.deltaTime);
    }

    private void HandleHealthChanged(float current, float max)
    {
        float ratio = GetRatio(current, max);
        float previousRatio = fill.fillAmount;

        fill.fillAmount = ratio;

        if (ratio > previousRatio)
        {
            // 회복은 실제 바와 잔상을 함께 처리
            lateFill.fillAmount = ratio;
        }
    }

    private void HandleHitFeedback(float previous, float current, float max)
    {
        if (feedback != null) feedback.Play(previous, current, max);
    }

    private void SyncFill()
    {
        float ratio = GetRatio(health.CurrentHealth, health.MaxHealth);

        fill.fillAmount = ratio;
        lateFill.fillAmount = ratio;
    }

    private float GetRatio(float current, float max)
    {
        return max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }
}
