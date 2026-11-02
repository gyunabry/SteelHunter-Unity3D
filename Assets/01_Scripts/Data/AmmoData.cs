using UnityEngine;

[CreateAssetMenu(fileName = "Ammo Data", menuName = "SteelHunter/Items/AmmoData")]
public class AmmoData : ItemData
{
    [SerializeField] private CaliberData caliber;
    [Tooltip("탄약 피해 계수")]
    [SerializeField, Min(0)] private float damageCoefficient = 1f;
    [SerializeField, Min(0)] private int armorPenetrationLevel;

    public override ItemKind Kind => ItemKind.Ammo;

    public CaliberData Caliber => caliber;
    public float DamageCoefficient => damageCoefficient;
    public int ArmorPenetrationLevel => armorPenetrationLevel;
}