using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Run once from the menu, or with -executeMethod SceneInventorySetup.Apply.
public static class SceneInventorySetup
{
    private const string BattlePath = "Assets/00_Scenes/BattleFieldScene.unity";
    private const string HangarPath = "Assets/00_Scenes/HangarScene.unity";

    [MenuItem("Tools/SteelHunter/Connect Inventory Session Scenes")]
    public static void Apply()
    {
        Scene battle = EditorSceneManager.OpenScene(BattlePath, OpenSceneMode.Single);
        GameObject battlePlayer = Find(battle, "PlayerCharacter");
        InventoryPresenter battlePresenter = Find(battle, "InventoryUI")
            .GetComponent<InventoryPresenter>();
        QuickSlots battleQuickSlots = Find(battle, "QuickSlotPanel")
            .GetComponent<QuickSlots>();
        InventorySessionBinding battleBinding = EnsureBinding(battlePlayer,
            battleQuickSlots, battlePresenter, null);
        BattleFlowController battleFlow = FindComponent<BattleFlowController>(battle);
        SetReference(battleFlow, "sessionBinding", battleBinding);

        Scene hangar = EditorSceneManager.OpenScene(HangarPath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(hangar);
        GameObject hangarPlayer = Find(hangar, "PlayerCharacter");
        GameObject canvasObject = Find(hangar, "Canvas");
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        GameObject inventoryObject = EnsureUiCopy(battle, hangar, canvasObject,
            "InventoryUI");
        GameObject quickSlotsObject = EnsureUiCopy(battle, hangar, canvasObject,
            "QuickSlotPanel");
        GameObject promptObject = EnsureUiCopy(battle, hangar, canvasObject,
            "InteractionPrompt");

        PlayerInventory inventory = hangarPlayer.GetComponent<PlayerInventory>();
        CharacterEquipment equipment = hangarPlayer.GetComponent<CharacterEquipment>();
        Interactor interactor = hangarPlayer.GetComponent<Interactor>();
        PlayerCharacterInput input = hangarPlayer.GetComponent<PlayerCharacterInput>();
        CharacterHealth health = hangarPlayer.GetComponent<CharacterHealth>();
        Camera worldCamera = FindComponent<Camera>(hangar);
        MouseLookAhead lookAhead = FindComponent<MouseLookAhead>(hangar);
        InteractionPromptView prompt = promptObject.GetComponent<InteractionPromptView>();
        QuickSlots quickSlots = quickSlotsObject.GetComponent<QuickSlots>();
        InventoryPresenter presenter = inventoryObject.GetComponent<InventoryPresenter>();
        Button sortieButton = Find(hangar, "Sortie_Button").GetComponent<Button>();

        SetReference(interactor, "view", prompt);
        SetReference(interactor, "worldCamera", worldCamera);
        SetFloat(interactor, "range", 5f);
        SetReference(input, "aimCamera", worldCamera);
        SetReference(input, "mouseLookAhead", lookAhead);
        SetReference(quickSlots, "inventory", inventory);
        foreach (QuickSlotDropTarget dropTarget in
                 quickSlotsObject.GetComponentsInChildren<QuickSlotDropTarget>(true))
            SetReference(dropTarget, "quickSlots", quickSlots);

        SetReference(presenter, "view", inventoryObject.GetComponent<InventoryView>());
        SetReference(presenter, "inventory", inventory);
        SetReference(presenter, "interactor", interactor);
        SetReference(presenter, "playerInput", input);
        SetReference(presenter, "quickSlots", quickSlots);
        SetReference(presenter, "playerHealth", health);
        SetReference(presenter, "equipment", equipment);
        EnsureBinding(hangarPlayer, quickSlots, presenter, sortieButton);
        ClearPersistentClicks(sortieButton);

        EnsureFloor(hangar);
        EnsureStash(hangar);
        ConfigureScrolling(inventoryObject);

        EditorSceneManager.MarkSceneDirty(battle);
        EditorSceneManager.MarkSceneDirty(hangar);
        EditorSceneManager.SaveScene(battle);
        EditorSceneManager.SaveScene(hangar);
        Debug.Log("Inventory session scene setup complete.");
    }

    private static InventorySessionBinding EnsureBinding(GameObject player,
        QuickSlots quickSlots, InventoryPresenter presenter, Button sortieButton)
    {
        InventorySessionBinding binding = player.GetComponent<InventorySessionBinding>();
        if (binding == null) binding = player.AddComponent<InventorySessionBinding>();
        SetReference(binding, "inventory", player.GetComponent<PlayerInventory>());
        SetReference(binding, "equipment", player.GetComponent<CharacterEquipment>());
        SetReference(binding, "startingLoadout", player.GetComponent<StartingLoadout>());
        SetReference(binding, "playerInput", player.GetComponent<PlayerCharacterInput>());
        SetReference(binding, "interactor", player.GetComponent<Interactor>());
        SetReference(binding, "gun", player.GetComponent<CharacterGun>());
        SetReference(binding, "quickSlots", quickSlots);
        SetReference(binding, "inventoryPresenter", presenter);
        SetReference(binding, "sortieButton", sortieButton);
        return binding;
    }

    private static GameObject EnsureUiCopy(Scene sourceScene, Scene targetScene,
        GameObject targetCanvas, string objectName)
    {
        GameObject existing = FindOptional(targetScene, objectName);
        if (existing != null) return existing;
        GameObject source = Find(sourceScene, objectName);
        GameObject copy = UnityEngine.Object.Instantiate(source);
        copy.name = objectName;
        SceneManager.MoveGameObjectToScene(copy, targetScene);
        copy.transform.SetParent(targetCanvas.transform, false);
        return copy;
    }

    private static void EnsureFloor(Scene scene)
    {
        if (FindOptional(scene, "HangarFloor") != null) return;
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "HangarFloor";
        SceneManager.MoveGameObjectToScene(floor, scene);
        floor.layer = LayerMask.NameToLayer("Ground");
        floor.transform.position = new Vector3(0f, -0.1f, 0f);
        floor.transform.localScale = new Vector3(30f, 0.2f, 30f);
    }

    private static void EnsureStash(Scene scene)
    {
        if (FindOptional(scene, "HangarStash") != null) return;
        GameObject stash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stash.name = "HangarStash";
        SceneManager.MoveGameObjectToScene(stash, scene);
        stash.layer = LayerMask.NameToLayer("Interactable");
        stash.transform.position = new Vector3(2f, 0.5f, 1f);
        GameObject anchor = new GameObject("PromptAnchor");
        anchor.transform.SetParent(stash.transform, false);
        anchor.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        HangarStash component = stash.AddComponent<HangarStash>();
        SetReference(component, "promptAnchor", anchor.transform);
    }

    private static void ConfigureScrolling(GameObject inventoryObject)
    {
        foreach (ScrollRect scroll in inventoryObject.GetComponentsInChildren<ScrollRect>(true))
        {
            if (scroll.content == null) continue;
            scroll.vertical = true;
            scroll.horizontal = false;
            ContentSizeFitter fitter = scroll.content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = scroll.content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private static void ClearPersistentClicks(Button button)
    {
        SerializedObject serialized = new SerializedObject(button);
        SerializedProperty calls = serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        if (calls == null)
            throw new InvalidOperationException("Sortie button persistent calls are unavailable.");
        calls.ClearArray();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetReference(UnityEngine.Object target, string propertyName,
        UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(target.name + "." + propertyName);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object target, string propertyName,
        float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException(target.name + "." + propertyName);
        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        T component = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .FirstOrDefault();
        if (component == null)
            throw new InvalidOperationException(scene.name + " needs " + typeof(T).Name);
        return component;
    }

    private static GameObject Find(Scene scene, string name)
    {
        GameObject result = FindOptional(scene, name);
        if (result == null)
            throw new InvalidOperationException(scene.name + " needs " + name);
        return result;
    }

    private static GameObject FindOptional(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
        return null;
    }
}
