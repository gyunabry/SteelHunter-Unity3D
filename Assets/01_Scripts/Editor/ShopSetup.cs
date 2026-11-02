using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopSetup
{
    public const string DataPath = "Assets/05_Data/Shop/HangarShop.asset";
    public const string ScenePath = "Assets/00_Scenes/HangarScene.unity";

    [MenuItem("Tools/SteelHunter/Setup Hangar Shop")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (!opened && scene.isDirty)
            throw new InvalidOperationException("Save the Hangar scene before shop setup.");
        ShopData data = AssetDatabase.LoadAssetAtPath<ShopData>(DataPath);
        if (data == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/05_Data/Shop"))
                AssetDatabase.CreateFolder("Assets/05_Data", "Shop");
            data = ScriptableObject.CreateInstance<ShopData>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/05_Data" }))
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (item == null) continue;
                SerializedObject so = new SerializedObject(item);
                int sell = item.SellPrice;
                switch (item.name)
                {
                    case "S_Rusty": sell = 1; break;
                    case "S_Normal": sell = 2; break;
                    case "Bandage": sell = 10; break;
                    case "MedKit": sell = 30; break;
                }
                so.FindProperty("sellPrice").intValue = sell;
                so.FindProperty("buyPrice").intValue = checked(sell * 2);
                so.FindProperty("canSell").boolValue = sell > 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
            }
            data.fixedItems = new[] {
                Entry("Ammo/S/S_Rusty", 2, 30), Entry("Ammo/S/S_Normal", 60),
                Entry("Consumable/Bandage", 5), Entry("Consumable/MedKit", 3) };
            data.randomItems = new[] {
                Entry("Weapon/Pistol"), Entry("Head/Head_Rusted"), Entry("Head/Head_Tactical"),
                Entry("Head/Head_Heavy"), Entry("Body/Body_Light"), Entry("Body/Body_Tactical"),
                Entry("Body/Body_Heavy"), Entry("Backpack/Small_Backpack"),
                Entry("Backpack/Field_Backpack"), Entry("Backpack/Tactical_Backpack") };
            AssetDatabase.CreateAsset(data, DataPath);
        }
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            InventorySessionBinding binding = Find<InventorySessionBinding>(scene);
            HangarShop terminal = Find<HangarShop>(scene);
            if (terminal == null)
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = "HangarShopTerminal";
                SceneManager.MoveGameObjectToScene(obj, scene);
                obj.layer = LayerMask.NameToLayer("Interactable");
                obj.transform.position = new Vector3(-2, .6f, 1);
                obj.transform.localScale = new Vector3(1, 1.2f, .6f);
                terminal = obj.AddComponent<HangarShop>();
                GameObject anchor = new GameObject("PromptAnchor");
                anchor.transform.SetParent(obj.transform, false);
                anchor.transform.localPosition = new Vector3(0, 1, 0);
                Set(terminal, "promptAnchor", anchor.transform);
            }
            Set(terminal, "data", data);
            ShopPresenter presenter = Find<ShopPresenter>(scene);
            if (presenter == null)
            {
                GameObject ui = new GameObject("ShopUI", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(ui, scene);
                Canvas canvas = ui.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                CanvasScaler scaler = ui.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.matchWidthOrHeight = .5f;
                ShopView view = ui.AddComponent<ShopView>();
                presenter = ui.AddComponent<ShopPresenter>();
                Set(presenter, "view", view);
            }
            Set(presenter, "binding", binding);
            Set(binding, "shopPresenter", presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Hangar shop setup complete.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
    private static ShopData.Entry Entry(string path, int quantity = 1, int bundleSize = 1)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/05_Data/" + path + ".asset");
        if (item == null) throw new InvalidOperationException("Missing shop item: " + path);
        return new ShopData.Entry { item = item, minQuantity = quantity, maxQuantity = quantity,
            bundleSize = bundleSize, weight = 1 };
    }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        if (target == null) throw new InvalidOperationException("Missing scene reference: " + field);
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
