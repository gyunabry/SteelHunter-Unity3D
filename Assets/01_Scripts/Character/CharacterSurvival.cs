using System;
using UnityEngine;

/*
 // 우선 수분과 출혈만 구현
 // 부위 골절과 부위별 출혈에 따른 디버프는 후순위
 
// TODO:
- 수분 부족 시 스태미너 이동속도 감소
- 수분도가 0이 되면 사망 처리
 */

[RequireComponent(typeof(CharacterHealth))]
public class CharacterSurvival : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHydration = 100f;
    [SerializeField, Min(1f)] private float hydrationLossPerSecond = 0.1f;
    [SerializeField, Min(1f)] private float bleedDamagePerSecond = 0.5f;

    private CharacterHealth health;
    private float hydration;
    private bool bleeding;

    public float Hydration => hydration;
    public float MaxHydration => maxHydration;
    public bool IsBleeding => bleeding;

    // 현재 수분도, 최대 수분도
    public event Action<float, float> HydrationChanged;

    private void Awake()
    {
        health = GetComponent<CharacterHealth>();
        hydration = maxHydration;
    }

    private void Update()
    {
        if (!health.IsAlive) return;

        SetHydration(hydration - hydrationLossPerSecond * Time.deltaTime);

        if (bleeding)
        {
            health.ApplyDamage(bleedDamagePerSecond * Time.deltaTime);
        }
    }

    public void StartBleeding()
    {
        if (health.IsAlive)
        {
            bleeding = true;
        }
    }

    // 아이템 등을 사용했을 때 출혈 중단
    public bool TryStopBleeding()
    {
        if (!health.IsAlive || !bleeding)
        {
            return false;
        }

        bleeding = false;
        return true;
    }

    private bool SetHydration(float value)
    {
        float next = Mathf.Clamp(value, 0f, maxHydration);

        if (hydration == next) return false;

        hydration = next;
        HydrationChanged?.Invoke(hydration, maxHydration);

        return true;
    }

    public bool TryRestoreHydration(float amount)
    {
        if (!health.IsAlive || amount <= 0f || hydration >= maxHydration)
        {
            return false;
        }

        return SetHydration(hydration + amount);
    }
}
