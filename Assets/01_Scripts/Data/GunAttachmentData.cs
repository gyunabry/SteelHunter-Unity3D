using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GunAttachmentData", menuName = "SteelHunter/Items/Gun Attachment Data")]
public class GunAttachmentData : EquipmentData
{
    [Tooltip("어떤 슬롯에 부착되는지")]
    [SerializeField] private GunAttachmentSlot slot;
    [Tooltip("어떤 스탯을 변경하는지")]
    [SerializeField] private GunStatModifier[] modifiers = Array.Empty<GunStatModifier>(); 

    public override ItemKind Kind => ItemKind.Attachment;

    public GunAttachmentSlot Slot => slot;
    public GunStatModifier[] Modifiers => modifiers;
}
