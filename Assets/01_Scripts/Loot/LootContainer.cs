using System;
using System.Collections.Generic;
using UnityEngine;

public class LootContainer : MonoBehaviour, IInteractable
{
    [Header("표시")]
    [SerializeField] private string containerName = "보급 상자";
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private string promptText = "";

    public string PromptText => string.IsNullOrWhiteSpace(promptText) ? containerName : promptText;

    [Header("씬에 배치한 상자의 초기 내용물")]
    [SerializeField] private LootTableData initialLootTable;

    private List<ItemStack> items = new();

    public string ContainerName => containerName;

    public Transform PromptAnchor => promptAnchor;

    public IReadOnlyList<ItemStack> Items => items;

    public bool IsInitialized { get; private set; }

    private void Start()
    {
        // 이미 적 드롭으로 초기화됐다면 바로 리턴
        if (IsInitialized) return;

        if (initialLootTable == null)
        {
            Debug.LogError($"{name} : 초기 루트 테이블이 없습니다.", this);
            return;
        }

        Initialize(initialLootTable);
    }

    public void Initialize(LootTableData table)
    {
        if (IsInitialized) return;

        List<ItemStack> rolledItems = table.Roll();

        items = rolledItems;
        IsInitialized = true;
    }

    public bool TryTransferTo(int index, PlayerInventory inventory)
    {
        if (!CanInteract() || inventory == null)
        {
            return false;
        }

        if (index < 0 || index >= items.Count)
        {
            return false;
        }

        ItemStack item = items[index];

        if (!inventory.TryAddItem(item))
        {
            return false;
        }

        items.RemoveAt(index);
        return true;
    }

    [ContextMenu("디버깅용 내용물 출력")]
    private void PrintContents()
    {
        if (!IsInitialized)
        {
            Debug.Log("아직 초기화되지 않은 상자입니다.");
            return;
        }

        Debug.Log($"{containerName} : {items.Count}개");
        
        foreach (ItemStack stack in items)
        {
            Debug.Log($"{stack.Data.DisplayName} x{stack.Quantity}");
        }
    }

    public bool CanInteract()
    {
        return isActiveAndEnabled && IsInitialized;
    }

    public void OnInteract()
    {
        if (!CanInteract())
        {
            return;
        }

        Debug.Log("상호작용!");
    }
}
