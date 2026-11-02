using UnityEngine;

public enum ItemKind
{
    Firearm,        // 무기
    Ammo,           // 탄약
    Attachment,     // 부착물
    CharacterArmor, // 방어구
    Backpack,       // 가방
    Consumable,     // 소모품
    Valuable        // 판매품
}

[CreateAssetMenu(fileName = "ItemData", menuName = "SteelHunter/Item Data")]
public class ItemData : IdentifiedData
{
    [Header("아이템")]
    [SerializeField] private ItemKind kind;
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea] private string description;
    [SerializeField, Min(1)] private int maxStack = 999;
    [SerializeField, Min(0f)] private float unitWeight;
    [SerializeField, Min(0)] private int sellPrice;
    [SerializeField, Min(0)] private int buyPrice;
    [SerializeField] private bool canSell;

    public virtual ItemKind Kind => kind;
    public Sprite Icon => icon;
    public string Description => description;
    public virtual int MaxStack => Mathf.Max(1, maxStack);
    public virtual float UnitWeight => Mathf.Max(0, unitWeight);
    public int SellPrice => sellPrice;
    public int BuyPrice => buyPrice;
    public bool CanSell => canSell && sellPrice > 0;
    public bool CanBuy => buyPrice > 0 && buyPrice > sellPrice;

    public virtual bool TryUse(GameObject user)
    {
        return false;
    }
}
