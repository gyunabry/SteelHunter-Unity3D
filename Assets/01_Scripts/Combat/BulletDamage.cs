using UnityEngine;

public static class BulletDamage
{
    public static float Calculate(float rawDamage, int armorLevel,
        int penetrationLevel, bool isCritical, float criticalMultiplier)
    {
        float damage = Mathf.Max(0f, rawDamage);
        int difference = armorLevel - penetrationLevel;
        if (armorLevel >= 0 && difference > 0)
            damage *= 2f / (difference + 2f);
        if (isCritical) damage *= Mathf.Max(1f, criticalMultiplier);
        return damage;
    }
}
