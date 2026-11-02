using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InventorySessionValidation
{
    [MenuItem("Tools/SteelHunter/Validate Inventory Session")]
    public static void Run()
    {
        ValidateScene("Assets/00_Scenes/HangarScene.unity", true);
        ValidateScene("Assets/00_Scenes/BattleFieldScene.unity", false);
        ValidateRuntimeCopies();
        Debug.Log("Inventory session validation passed.");
    }

    private static void ValidateScene(string path, bool hangar)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        InventorySessionBinding binding = FindComponent<InventorySessionBinding>(scene);
        PlayerInventory inventory = FindComponent<PlayerInventory>(scene);
        CharacterEquipment equipment = FindComponent<CharacterEquipment>(scene);
        InventoryPresenter presenter = FindComponent<InventoryPresenter>(scene);
        QuickSlots quickSlots = FindComponent<QuickSlots>(scene);
        Interactor interactor = FindComponent<Interactor>(scene);
        Check(Reference(binding, "inventory") == inventory, "Binding inventory");
        Check(Reference(binding, "equipment") == equipment, "Binding equipment");
        Check(Reference(binding, "inventoryPresenter") == presenter, "Binding presenter");
        Check(Reference(binding, "quickSlots") == quickSlots, "Binding quick slots");
        Check(Reference(presenter, "inventory") == inventory, "Presenter inventory");
        Check(Reference(presenter, "interactor") == interactor, "Presenter interactor");
        InventoryView view = Reference(presenter, "view") as InventoryView;
        Check(view != null && view.gameObject.scene == scene, "Presenter view scene");
        Check(Reference(view, "windowRoot") is GameObject &&
              Reference(view, "closeButton") is Button &&
              Reference(view, "inventoryContent") is Transform &&
              Reference(view, "lootContent") is Transform,
            "Inventory view controls");
        Check(Reference(quickSlots, "inventory") == inventory, "Quick slot inventory");
        InteractionPromptView prompt = Reference(interactor, "view") as InteractionPromptView;
        Check(prompt != null && prompt.gameObject.scene == scene, "Interaction prompt");
        foreach (QuickSlotDropTarget drop in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<QuickSlotDropTarget>(true)))
            Check(Reference(drop, "quickSlots") == quickSlots, "Quick slot drop target");
        Camera interactionCamera = Reference(interactor, "worldCamera") as Camera;
        Check(interactionCamera != null ||
              scene.GetRootGameObjects().SelectMany(root =>
                  root.GetComponentsInChildren<Camera>(true))
                  .Any(camera => camera.CompareTag("MainCamera")),
            "Interaction camera");

        if (hangar)
        {
            Check(FindComponent<HangarStash>(scene) != null, "Hangar stash");
            Button button = FindComponent<InventorySessionBinding>(scene)
                .GetComponent<PlayerInventory>() != null
                ? (Button)Reference(binding, "sortieButton") : null;
            Check(button != null, "Sortie button");
            SerializedProperty calls = new SerializedObject(button)
                .FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            Check(calls != null && calls.arraySize == 0, "Sortie button duplicate click");
        }
        else
        {
            BattleFlowController flow = FindComponent<BattleFlowController>(scene);
            Check(Reference(flow, "sessionBinding") == binding, "Battle result binding");
        }
    }

    private static void ValidateRuntimeCopies()
    {
        FirearmData firearm = LoadFirst<FirearmData>();
        AmmoData ammo = LoadFirst<AmmoData>();
        GameObject source = new GameObject("InventoryValidationSource");
        GameObject destination = new GameObject("InventoryValidationDestination");
        try
        {
            PlayerInventory sourceInventory = source.AddComponent<PlayerInventory>();
            CharacterEquipment sourceEquipment = source.AddComponent<CharacterEquipment>();
            QuickSlots sourceQuick = source.AddComponent<QuickSlots>();
            PlayerInventory destinationInventory = destination.AddComponent<PlayerInventory>();
            CharacterEquipment destinationEquipment = destination.AddComponent<CharacterEquipment>();
            QuickSlots destinationQuick = destination.AddComponent<QuickSlots>();
            Initialize(sourceInventory, sourceEquipment, sourceQuick);
            Initialize(destinationInventory, destinationEquipment, destinationQuick);

            ItemStack firearmItem = new ItemStack(firearm, 1);
            Check(sourceInventory.TryAddItem(firearmItem), "Add firearm");
            Check(sourceEquipment.TryEquip(0), "Equip firearm");
            WeaponInstance original = sourceEquipment.EquippedWeapon;
            SetWeapon(original, ammo, 7);
            Check(sourceInventory.TryAddItem(new ItemStack(ammo, 30)), "Add ammo");
            Check(sourceQuick.TryAssign(0, sourceInventory, ammo), "Assign quick slot");

            PlayerInventorySnapshot snapshot = PlayerInventorySnapshot.Capture(
                sourceInventory, sourceEquipment, sourceQuick);
            SetWeapon(original, ammo, 2);
            snapshot.Apply(destinationInventory, destinationEquipment, destinationQuick);
            Check(destinationEquipment.EquippedWeapon.LoadedRounds == 7,
                "Magazine restored from independent copy");
            Check(destinationEquipment.EquippedWeapon.SelectedAmmo == ammo &&
                  destinationEquipment.EquippedWeapon.LoadedAmmo == ammo,
                "Both ammo references restored");
            Check(!ReferenceEquals(destinationEquipment.EquippedWeapon, original),
                "Weapon runtime state shared between scenes");
            Check(destinationEquipment.EquippedItem.InstanceId == firearmItem.InstanceId,
                "Equipment identity changed");
            Check(destinationInventory.GetQuantity(ammo) == 30, "Ammo quantity changed");

            snapshot.Apply(destinationInventory, destinationEquipment, destinationQuick);
            Check(destinationInventory.GetQuantity(ammo) == 30 &&
                  destinationEquipment.EquippedItem.InstanceId == firearmItem.InstanceId,
                "Repeated restore duplicated items");

            StashInventory stash = new StashInventory();
            Check(Transfer(stash, "TryDeposit", destinationInventory, 0),
                "Deposit ammo into stash");
            Check(destinationInventory.GetQuantity(ammo) == 0 &&
                  stash.Items.Sum(item => item?.Quantity ?? 0) == 30,
                "Deposit did not transfer ownership");
            Check(Transfer(stash, "TryWithdraw", destinationInventory, 0),
                "Withdraw ammo from stash");
            Check(destinationInventory.GetQuantity(ammo) == 30 &&
                  stash.Items[0] == null, "Withdraw duplicated ammo");

            Check(destinationEquipment.TryUnequip(EquipmentSlot.PrimaryWeapon),
                "Unequip firearm for storage");
            int firearmIndex = Enumerable.Range(0, destinationInventory.SlotCount)
                .First(index => destinationInventory.GetItem(index)?.InstanceId ==
                                firearmItem.InstanceId);
            Check(Transfer(stash, "TryDeposit", destinationInventory, firearmIndex),
                "Deposit firearm into stash");
            Check(stash.Items[0]?.Weapon.LoadedRounds == 7 &&
                  stash.Items[0].InstanceId == firearmItem.InstanceId,
                "Stored weapon runtime state changed");
            Check(Transfer(stash, "TryWithdraw", destinationInventory, 0),
                "Withdraw firearm from stash");
            Check(Enumerable.Range(0, destinationInventory.SlotCount)
                    .Select(destinationInventory.GetItem)
                    .Any(item => item?.InstanceId == firearmItem.InstanceId &&
                                 item.Weapon.LoadedRounds == 7),
                "Withdrawing weapon lost state");

            snapshot.WithoutCarriedItems(destinationInventory.BaseSlotCount)
                .Apply(destinationInventory, destinationEquipment, destinationQuick);
            Check(destinationInventory.OccupiedSlotCount == 0 &&
                  destinationEquipment.EquippedItem == null,
                "Death result retained carried items");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(destination);
        }
    }

    private static void Initialize(PlayerInventory inventory,
        CharacterEquipment equipment, QuickSlots quickSlots)
    {
        typeof(PlayerInventory).GetMethod("Awake",
            BindingFlags.NonPublic | BindingFlags.Instance).Invoke(inventory, null);
        typeof(CharacterEquipment).GetMethod("Awake",
            BindingFlags.NonPublic | BindingFlags.Instance).Invoke(equipment, null);
        SerializedObject serialized = new SerializedObject(quickSlots);
        serialized.FindProperty("inventory").objectReferenceValue = inventory;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool Transfer(StashInventory stash, string method,
        PlayerInventory inventory, int index)
    {
        MethodInfo transfer = typeof(StashInventory).GetMethod(method,
            BindingFlags.NonPublic | BindingFlags.Instance);
        object[] arguments = method == "TryDeposit"
            ? new object[] { inventory, index, null }
            : new object[] { index, inventory, null };
        return (bool)transfer.Invoke(stash, arguments);
    }

    private static void SetWeapon(WeaponInstance weapon, AmmoData ammo,
        int loadedRounds)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        typeof(WeaponInstance).GetProperty("SelectedAmmo", flags)
            .SetValue(weapon, ammo);
        typeof(WeaponInstance).GetProperty("LoadedAmmo", flags)
            .SetValue(weapon, ammo);
        typeof(WeaponInstance).GetProperty("LoadedRounds", flags)
            .SetValue(weapon, loadedRounds);
    }

    private static T LoadFirst<T>() where T : UnityEngine.Object
    {
        string guid = AssetDatabase.FindAssets("t:" + typeof(T).Name).FirstOrDefault();
        if (guid == null) throw new InvalidOperationException("Missing " + typeof(T).Name);
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        T found = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .FirstOrDefault();
        if (found == null) throw new InvalidOperationException(scene.name + " needs " + typeof(T).Name);
        return found;
    }

    private static UnityEngine.Object Reference(UnityEngine.Object owner, string field)
    {
        SerializedProperty property = new SerializedObject(owner).FindProperty(field);
        return property?.objectReferenceValue;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
