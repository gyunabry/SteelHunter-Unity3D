using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTable", menuName = "Steel Hunter/Loot Table")]
public class LootTableData : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemData item;

        [UnityEngine.Range(0f, 1f)]
        public float probability = 1f;

        [Min(1)]
        public int minQuantity = 1;

        [Min(1)]
        public int maxQuantity = 1;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    public List<ItemStack> Roll()
    {
        var result = new List<ItemStack>();

        if (entries == null)
        {
            return result;
        }

        foreach (Entry entry in entries)
        {
            float probability = Mathf.Clamp01(entry.probability);

            if (probability <= 0f)
            {
                continue;
            }

            if (UnityEngine.Random.value >= probability)
            {
                continue;
            }

            int remaining = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);

            // 최대 중첩 수를 넘으면 여러 스택으로 나눈다
            while (remaining > 0)
            {
                int quantity = Mathf.Min(remaining, entry.item.MaxStack);

                result.Add(new ItemStack(entry.item, quantity));
                remaining -= quantity;
            }
        }

        return result;
    }
}
