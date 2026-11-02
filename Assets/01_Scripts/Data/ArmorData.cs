using UnityEngine;

public enum ArmorSlot
{
    Head,
    Body
}

[CreateAssetMenu(fileName = "ArmorData", menuName = "SteelHunter/Items/Character Armor Data")]
public class ArmorData : EquipmentData
{
    [SerializeField] private ArmorSlot armorSlot;
    [SerializeField, Min(0)] private int armorLevel;

    public override ItemKind Kind => ItemKind.CharacterArmor;

    public ArmorSlot Slot => armorSlot;
    public int ArmorLevel => armorLevel;
}
