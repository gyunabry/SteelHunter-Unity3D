using System;
using UnityEngine;

public enum ConsumableEffectKind
{
    RestoreHealth,
    StopBleeding,
    RestoreHydration
}

[Serializable]
public struct ConsumableEffect
{
    public ConsumableEffectKind kind;

    [Min(0f)]
    public float amount;
}

[CreateAssetMenu(fileName = "Consumable Data", menuName = "SteelHunter/Items/Consumable Data")]
public class ConsumableData : ItemData
{
    [SerializeField, Min(0f)] private float useDuration;
    [SerializeField] private ConsumableEffect[] effects = Array.Empty<ConsumableEffect>();

    public override ItemKind Kind => ItemKind.Consumable;

    public float UseDuration => useDuration;

    public override bool TryUse(GameObject user)
    {
        if (user == null || useDuration > 0f || effects == null)
        {
            return false;
        }

        CharacterHealth health = user.GetComponent<CharacterHealth>();
        CharacterSurvival survival = user.GetComponent<CharacterSurvival>();

        bool applied = false;

        foreach (ConsumableEffect effect in effects)
        {
            switch (effect.kind)
            {
                case ConsumableEffectKind.RestoreHealth:
                    applied |= health != null && health.TryHeal(effect.amount);
                    break;

                case ConsumableEffectKind.StopBleeding:
                    applied |= survival != null && survival.TryStopBleeding();
                    break;

                case ConsumableEffectKind.RestoreHydration:
                    applied |= survival != null && survival.TryRestoreHydration(effect.amount);
                    break;
            }
        }

        return applied;
    }
}
