using System;
using UnityEngine;

public sealed class CharacterHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float defaultMaxHealth = 100f;

    private bool initialized;

    public event Action<float, float> HealthChanged;
    // 피해 전 체력, 피해 후 체력, 최대 체력
    public event Action<float, float, float> HitFeedbackRequested;
    public event Action Died;
    public event Action<Vector3, Vector3> Damaged;

    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    public bool IsAlive => initialized && CurrentHealth > 0f;

    private void Awake()
    {
        if (!initialized) Initialize(defaultMaxHealth);
    }

    public void Initialize(float maxHealth)
    {
        MaxHealth = Mathf.Max(1f, maxHealth);
        CurrentHealth = MaxHealth;
        initialized = true;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public bool ApplyDamage(float damage, bool playHitFeedback = true)
    {
        return ApplyDamage(
            damage,
            transform.position,
            Vector3.zero,
            playHitFeedback
        );
    }

    public bool ApplyDamage(float damage, Vector3 hitPoint, Vector3 shotDirection, bool playHitFeedback = true)
    {
        if (!IsAlive || damage <= 0f) return false;

        float previous = CurrentHealth;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (playHitFeedback && CurrentHealth < previous)
        {
            HitFeedbackRequested?.Invoke(previous, CurrentHealth, MaxHealth);
        }

        Damaged?.Invoke(hitPoint, shotDirection);

        if (CurrentHealth <= 0f) Died?.Invoke();

        return true;
    }

    public bool TryHeal(float amount)
    {
        if (!IsAlive || amount <= 0f || CurrentHealth >= MaxHealth)
        {
            return false;
        }

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        return true;
    }
}
