using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HangarShop", menuName = "SteelHunter/Shop Data")]
public sealed class ShopData : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public ItemData item;
        [Tooltip("재입고 시 구매 가능한 묶음 수의 최솟값")]
        [Min(1)] public int minQuantity = 1;
        [Tooltip("재입고 시 구매 가능한 묶음 수의 최댓값")]
        [Min(1)] public int maxQuantity = 1;
        [Tooltip("한 번 구매할 때 지급하는 개수. 재고 수량은 구매 가능한 묶음 수입니다.")]
        [Min(1)] public int bundleSize = 1;
        [Min(0)] public float weight = 1;
    }

    [Min(1)] public float refreshSeconds = 600;
    [Min(0)] public int initialBalance = 1000;
    [Min(0)] public int randomCount = 4;
    public Entry[] fixedItems = Array.Empty<Entry>();
    public Entry[] randomItems = Array.Empty<Entry>();
}
