using UnityEngine;

[CreateAssetMenu(fileName = "Backpack Data", menuName = "SteelHunter/Items/Backpack Data")]
public class BackpackData : EquipmentData
{
    [SerializeField, Min(0)] private int addedSlotCount = 5;
    [SerializeField, Min(0f)] private float addedWeightCapacity = 10f;

    public override ItemKind Kind => ItemKind.Backpack;

    public int AddedSlotCount => Mathf.Max(0, addedSlotCount);
    public float AddedWeightCapacity => Mathf.Max(0f, addedWeightCapacity);
}
