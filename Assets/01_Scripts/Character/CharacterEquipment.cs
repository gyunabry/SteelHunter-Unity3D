using System;
using UnityEngine;

public enum EquipmentSlot
{
    PrimaryWeapon = 0,
    SecondaryWeapon = 1,
    Head = 2,
    Body = 3,
    Backpack = 4
}

[RequireComponent(typeof(PlayerInventory))]
public class CharacterEquipment : MonoBehaviour, IWeaponProvider
{
    [SerializeField] private PlayerInventory inventory;

    // ������ ���� �������� �����Ѵ�.
    private readonly ItemStack[] equipped = new ItemStack[5];

    // ���� ����� ���� ��ġ.
    private EquipmentSlot activeWeaponSlot =
        EquipmentSlot.PrimaryWeapon;

    // ��� ���� �Ǵ� Ȱ�� ������ �ٲ���� �� �߻�.
    public event Action Changed;

    // ���� ��� ���� ���� �ٲ���� ���� �߻�.
    public event Action WeaponChanged;

    public EquipmentSlot ActiveWeaponSlot => activeWeaponSlot;

    public ItemStack EquippedItem =>
        GetEquipped(activeWeaponSlot);

    public WeaponInstance EquippedWeapon =>
        EquippedItem?.Weapon;

    public ArmorData HeadArmor =>
        GetEquipped(EquipmentSlot.Head)?.Data as ArmorData;

    public ArmorData BodyArmor =>
        GetEquipped(EquipmentSlot.Body)?.Data as ArmorData;

    public BackpackData Backpack =>
        GetEquipped(EquipmentSlot.Backpack)?.Data as BackpackData;

    // ������ �� 2��, �Ӹ�, ��ü, ������ ��� �ջ��Ѵ�.
    // ItemStack.TotalWeight���� ���� ź�� ���Ե� ���Եȴ�.
    public float EquippedWeight
    {
        get
        {
            float total = 0f;

            foreach (ItemStack item in equipped)
            {
                // �� ��� ĭ�� null�̴�.
                if (item == null)
                    continue;

                total += item.TotalWeight;
            }

            return total;
        }
    }

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();
    }

    // ������ ��ġ�� ������ �������� ��ȸ�Ѵ�.
    public ItemStack GetEquipped(EquipmentSlot slot)
    {
        if (!IsValidSlot(slot))
            return null;

        return equipped[(int)slot];
    }

    // ������ ���� �������� �̹� �����Ǿ� �ִ��� Ȯ���Ѵ�.
    public bool Contains(ItemStack item)
    {
        return item != null
            && Array.IndexOf(equipped, item) >= 0;
    }

    // ���� �������� Ŭ������ �� ����� �ڵ� ���� �Լ�.
    // ���� InventoryPresenter�� StartingLoadout�� ȣ���Ѵ�.
    public bool TryEquip(int inventoryIndex)
    {
        if (inventory == null)
            return false;

        ItemStack item = inventory.GetItem(inventoryIndex);

        if (item == null)
            return false;

        // ���� ����
        if (item.Data is FirearmData)
        {
            EquipmentSlot target;

            if (GetEquipped(EquipmentSlot.PrimaryWeapon) == null)
            {
                target = EquipmentSlot.PrimaryWeapon;
            }
            else if (GetEquipped(EquipmentSlot.SecondaryWeapon) == null)
            {
                target = EquipmentSlot.SecondaryWeapon;
            }
            else
            {
                // �� ĭ�� ��� �� ������ ���� ����ϴ� ���� ��ü.
                target = activeWeaponSlot;
            }

            return TryEquip(inventoryIndex, target);
        }

        // �� ����
        if (item.Data is ArmorData armor)
        {
            if (armor.Slot == ArmorSlot.Head)
                return TryEquip(inventoryIndex, EquipmentSlot.Head);

            if (armor.Slot == ArmorSlot.Body)
                return TryEquip(inventoryIndex, EquipmentSlot.Body);

            return false;
        }

        // ���� ����
        if (item.Data is BackpackData)
        {
            return TryEquip(
                inventoryIndex,
                EquipmentSlot.Backpack);
        }

        return false;
    }

    // Ư�� ��� ĭ���� �巡������ �� ����� ���� ���� �Լ�.
    public bool TryEquip(int inventoryIndex, EquipmentSlot target)
    {
        if (inventory == null || !IsValidSlot(target))
            return false;

        ItemStack incoming = inventory.GetItem(inventoryIndex);

        if (!CanEquip(incoming, target))
            return false;

        WeaponInstance previousWeapon = EquippedWeapon;

        // ���� �� ������ ������ ������ �������� ����Ѵ�.
        BackpackData nextBackpack =
            target == EquipmentSlot.Backpack
                ? incoming.Data as BackpackData
                : Backpack;

        int nextCapacity = GetCapacity(nextBackpack);
        float nextMaxWeight = inventory.GetMaxWeight(nextBackpack);

        bool exchanged = inventory.TryExchangeEquipment(
            inventoryIndex,
            ref equipped[(int)target],
            nextCapacity,
            nextMaxWeight);

        if (!exchanged)
            return false;

        // ��� ���� ���� ������ ���� ������ ���� �����Ѵ�.
        if (IsWeaponSlot(target) && EquippedWeapon == null)
            activeWeaponSlot = target;

        PublishChange(previousWeapon, true);
        return true;
    }

    // ���� ȣ��ο� ȣȯ: ���� ��� ���� ���� �����Ѵ�.
    public bool TryUnequip()
    {
        return TryUnequip(activeWeaponSlot);
    }

    // ������ ��� �������� �ǵ�����.
    public bool TryUnequip(EquipmentSlot target)
    {
        if (inventory == null || !IsValidSlot(target))
            return false;

        if (GetEquipped(target) == null)
            return false;

        WeaponInstance previousWeapon = EquippedWeapon;

        // ������ �����ϸ� �⺻ �뷮�� �⺻ ��� ���Է� ���ư���.
        BackpackData nextBackpack =
            target == EquipmentSlot.Backpack
                ? null
                : Backpack;

        int nextCapacity = GetCapacity(nextBackpack);
        float nextMaxWeight = inventory.GetMaxWeight(nextBackpack);

        bool returned = inventory.TryReturnEquipment(
            ref equipped[(int)target],
            nextCapacity,
            nextMaxWeight);

        if (!returned)
            return false;

        // ��� ���̴� ���� �����ߴٸ� ���� ������ ��ȯ�Ѵ�.
        if (target == activeWeaponSlot)
        {
            EquipmentSlot other =
                target == EquipmentSlot.PrimaryWeapon
                    ? EquipmentSlot.SecondaryWeapon
                    : EquipmentSlot.PrimaryWeapon;

            if (GetEquipped(other)?.Weapon != null)
                activeWeaponSlot = other;
        }

        PublishChange(previousWeapon, true);
        return true;
    }

    // ���� 1/2 �Ǵ� ���� ���� Ŭ������ ����� ���� �����Ѵ�.
    public bool TrySelectWeapon(EquipmentSlot slot)
    {
        if (!IsWeaponSlot(slot))
            return false;

        if (GetEquipped(slot)?.Weapon == null)
            return false;

        // ���� ���� �ٽ� �����ϸ� ���¸� �������� �ʴ´�.
        if (activeWeaponSlot == slot)
            return true;

        WeaponInstance previousWeapon = EquippedWeapon;
        activeWeaponSlot = slot;

        PublishChange(previousWeapon, false);
        return true;
    }

    private int GetCapacity(BackpackData bag)
    {
        return inventory.BaseSlotCount
            + (bag != null ? Mathf.Max(0, bag.AddedSlotCount) : 0);
    }

    private void PublishChange(
        WeaponInstance previousWeapon,
        bool inventoryChanged)
    {
        // ���� �� ��ü�� �ٲ���� ���� �˸���.
        if (!ReferenceEquals(previousWeapon, EquippedWeapon))
            WeaponChanged?.Invoke();

        // ���� ������ ���¸� ��� ������ �� �˸���.
        if (inventoryChanged)
            inventory.PublishChanged();

        Changed?.Invoke();
    }

    private static bool IsValidSlot(EquipmentSlot slot)
    {
        int index = (int)slot;
        return index >= 0 && index < 5;
    }

    private static bool IsWeaponSlot(EquipmentSlot slot)
    {
        return slot == EquipmentSlot.PrimaryWeapon
            || slot == EquipmentSlot.SecondaryWeapon;
    }

    private static bool CanEquip(ItemStack item, EquipmentSlot target)
    {
        if (item == null || item.Quantity != 1)
            return false;

        switch (target)
        {
            case EquipmentSlot.PrimaryWeapon:
            case EquipmentSlot.SecondaryWeapon:
                return item.Data is FirearmData
                    && item.Weapon != null;

            case EquipmentSlot.Head:
                return item.Data is ArmorData head
                    && head.Slot == ArmorSlot.Head;

            case EquipmentSlot.Body:
                return item.Data is ArmorData body
                    && body.Slot == ArmorSlot.Body;

            case EquipmentSlot.Backpack:
                return item.Data is BackpackData;

            default:
                return false;
        }
    }
    internal ItemStack[] CopyEquipped()
    {
        ItemStack[] copy = new ItemStack[equipped.Length];
        for (int i = 0; i < copy.Length; i++)
            copy[i] = equipped[i]?.DeepCopy();
        return copy;
    }

    internal bool ContainsInstanceId(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return false;
        foreach (ItemStack item in equipped)
        {
            if (item != null && item.InstanceId == instanceId)
                return true;
        }
        return false;
    }

    internal static bool CanRestore(ItemStack[] items, EquipmentSlot active)
    {
        if (items == null || items.Length != 5 || !IsWeaponSlot(active))
            return false;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && !CanEquip(items[i], (EquipmentSlot)i))
                return false;
        }
        return true;
    }

    internal void RestoreSilently(ItemStack[] items, EquipmentSlot active)
    {
        Array.Copy(items, equipped, equipped.Length);
        activeWeaponSlot = active;
    }

    internal void PublishRestored()
    {
        // 새 무기 인스턴스로 교체했으므로 총과 HUD에도 알린다.
        WeaponChanged?.Invoke();
        Changed?.Invoke();
    }
}
