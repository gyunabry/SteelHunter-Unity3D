using System;

public class ItemStack
{
    public ItemData Data { get; }
    public int Quantity { get; }
    public string InstanceId { get; }
    public WeaponInstance Weapon { get; }

    public float TotalWeight
    {
        get
        {
            float weight = Data.UnitWeight * Quantity;
            if (Weapon != null && Weapon.LoadedAmmo != null &&
                Weapon.LoadedRounds > 0)
            {
                weight += Weapon.LoadedAmmo.UnitWeight * Weapon.LoadedRounds;
            }
            return weight;
        }
    }

    public ItemStack(ItemData data, int quantity)
        : this(data, quantity,
            data is EquipmentData ? Guid.NewGuid().ToString("N") : null,
            data is FirearmData firearm ? new WeaponInstance(firearm) : null)
    {
    }

    private ItemStack(ItemData data, int quantity, string instanceId, WeaponInstance weapon)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (data is EquipmentData &&
            (quantity != 1 || string.IsNullOrEmpty(instanceId)))
        {
            throw new ArgumentException("장비는 수량 1과 고유 ID가 필요합니다.");
        }
        if (data is FirearmData firearm &&
            (weapon == null || weapon.Data != firearm))
        {
            throw new ArgumentException("총기 정의와 무기 상태가 일치하지 않습니다.");
        }

        Data = data;
        Quantity = quantity;
        InstanceId = instanceId;
        Weapon = weapon;
    }

    public ItemStack DeepCopy()
    {
        return new ItemStack(Data, Quantity, InstanceId, Weapon?.DeepCopy());
    }
}
