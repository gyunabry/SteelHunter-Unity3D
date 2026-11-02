using System;
using System.Collections.Generic;

public class WeaponInstance
{
    public FirearmData Data { get; }

    public AmmoData SelectedAmmo { get; internal set; }
    public AmmoData LoadedAmmo { get; internal set; }
    public int LoadedRounds { get; internal set; }

    public WeaponStats Stats { get; private set; }

    // 부착물을 슬롯별로 관리
    private readonly Dictionary<GunAttachmentSlot, ItemStack> attachments = new();

    public event Action AttachmentChanged;

    public WeaponInstance(FirearmData data)
    {
        Data = data != null
            ? data
            : throw new ArgumentNullException(nameof(data));

        RecalculateStats();
    }

    // 해당 슬롯에 장착된 아이템 조회
    public ItemStack GetAttachment(GunAttachmentSlot slot)
    {
        return attachments.TryGetValue(slot, out ItemStack item)
            ? item
            : null;
    }

    public float AttachmentWeight
    {
        get
        {
            float total = 0f;

            foreach (ItemStack item in attachments.Values)
                total += item.TotalWeight;

            return total;
        }
    }

    // 장착 가능 여부 검사
    public bool CanAttach(ItemStack item, out string error)
    {
        return TryPrepareAttachment(
            item,
            out _,
            out error);
    }

    // 장착 또는 교체
    // 성공하면 previous에 이전 부착물이 들어간다
    // 빈 슬롯에 장착했다면 previous는 null이다
    public bool TryAttach(
        ItemStack item,
        out ItemStack previous,
        out string error)
    {
        previous = null;

        if (!TryPrepareAttachment(
                item,
                out WeaponStats nextStats,
                out error))
        {
            return false;
        }

        var attachment = (GunAttachmentData)item.Data;
        GunAttachmentSlot slot = attachment.Slot;

        previous = GetAttachment(slot);

        // 검사가 모두 끝난 뒤 실제 상태를 변경
        attachments[slot] = item;
        Stats = nextStats;

        AttachmentChanged?.Invoke();
        return true;
    }

    // 부착물 해제
    // 성공하면 removed에 해제된 아이템이 들어간다
    public bool TryDetach(
        GunAttachmentSlot slot,
        out ItemStack removed,
        out string error)
    {
        removed = null;

        if (!Enum.IsDefined(typeof(GunAttachmentSlot), slot))
        {
            error = "유효하지 않은 슬롯입니다.";
            return false;
        }

        ItemStack current = GetAttachment(slot);

        if (current == null)
        {
            error = "해당 슬롯에 부착물이 없습니다.";
            return false;
        }

        // 대상 슬롯을 제외한 조합으로 먼저 계산한다.
        WeaponStats nextStats = WeaponStatCalculator.Calculate(
            Data,
            EnumerateDefinitionsAfterChange(slot, null));

        if (LoadedRounds > nextStats.MagazineCapacity)
        {
            error = "해제 후 탄창 용량보다 잔탄이 많습니다.";
            return false;
        }

        attachments.Remove(slot);
        Stats = nextStats;

        removed = current;
        error = null;

        AttachmentChanged?.Invoke();
        return true;
    }

    // 입력 검사와 변경 후 스탯 계산
    // 실제 attachments와 Stats는 수정하지 않는다
    private bool TryPrepareAttachment(
        ItemStack item,
        out WeaponStats nextStats,
        out string error)
    {
        nextStats = null;

        if (item == null)
        {
            error = "부착물 아이템이 없습니다.";
            return false;
        }

        if (!(item.Data is GunAttachmentData attachment))
        {
            error = "부착물 아이템이 아닙니다.";
            return false;
        }

        if (item.Quantity != 1 ||
            string.IsNullOrEmpty(item.InstanceId))
        {
            error = "유효하지 않은 부착물 아이템입니다.";
            return false;
        }

        GunAttachmentSlot slot = attachment.Slot;

        if (!Enum.IsDefined(typeof(GunAttachmentSlot), slot))
        {
            error = "유효하지 않은 부착물 슬롯입니다.";
            return false;
        }

        if (!Data.SupportsAttachmentSlot(slot))
        {
            error = "이 무기는 해당 부착물 슬롯을 지원하지 않습니다.";
            return false;
        }

        foreach (ItemStack equipped in attachments.Values)
        {
            if (equipped.InstanceId == item.InstanceId)
            {
                error = "이미 이 무기에 장착된 아이템입니다.";
                return false;
            }
        }

        // 기존 대상 슬롯을 제외하고 새 부착물을 넣은 조합.
        nextStats = WeaponStatCalculator.Calculate(
            Data,
            EnumerateDefinitionsAfterChange(slot, attachment));

        if (LoadedRounds > nextStats.MagazineCapacity)
        {
            nextStats = null;
            error = "변경 후 탄창 용량보다 잔탄이 많습니다.";
            return false;
        }

        error = null;
        return true;
    }

    // 현재 장착된 부착물의 정의 데이터만 전달.
    private IEnumerable<GunAttachmentData> EnumerateDefinitions()
    {
        foreach (ItemStack item in attachments.Values)
            yield return (GunAttachmentData)item.Data;
    }

    // 실제 상태를 바꾸지 않고 변경 후의 조합을 전달.
    private IEnumerable<GunAttachmentData> EnumerateDefinitionsAfterChange(
        GunAttachmentSlot targetSlot,
        GunAttachmentData incoming)
    {
        foreach (var pair in attachments)
        {
            // 교체 또는 해제할 기존 부착물은 제외.
            if (pair.Key == targetSlot)
                continue;

            yield return (GunAttachmentData)pair.Value.Data;
        }

        // incoming이 null이면 해제한 조합이 된다.
        if (incoming != null)
            yield return incoming;
    }

    private void RecalculateStats()
    {
        Stats = WeaponStatCalculator.Calculate(
            Data,
            EnumerateDefinitions());
    }

    public WeaponInstance DeepCopy()
    {
        var copy = new WeaponInstance(Data)
        {
            SelectedAmmo = SelectedAmmo,
            LoadedAmmo = LoadedAmmo,
            LoadedRounds = LoadedRounds
        };

        foreach (var pair in attachments)
        {
            copy.attachments.Add(
                pair.Key,
                pair.Value.DeepCopy());
        }

        copy.RecalculateStats();

        return copy;
    }
}