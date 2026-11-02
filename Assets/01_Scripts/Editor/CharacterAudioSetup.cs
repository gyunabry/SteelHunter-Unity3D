using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CharacterAudioSetup
{
    public const string DataRoot = "Assets/05_Data/Audio";
    public const string PlayerPath = "Assets/02_Prefabs/Character/PlayerCharacter.prefab";
    public const string EnemyPath = "Assets/02_Prefabs/Character/EnemyCharacter.prefab";

    [MenuItem("Tools/SteelHunter/Setup Character Audio")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before audio setup.");
        if (!AssetDatabase.IsValidFolder(DataRoot)) AssetDatabase.CreateFolder("Assets/05_Data", "Audio");
        SoundCue pistol = Cue("PistolFire", "weapon_blaster_001", 0.65f, 35f);
        SoundCue rifle = Cue("RifleFire", "weapon_gatling_001", 0.65f, 45f);
        SoundCue walk = Cue("WalkFootstep", null, 0.4f, 14f);
        SoundCue sprint = Cue("SprintFootstep", null, 0.55f, 20f);
        SoundCue reloadStart = Cue("ReloadStart", null, 0.5f, 12f);
        SoundCue reloadComplete = Cue("ReloadComplete", null, 0.5f, 12f);
        SoundCue hurt = Cue("CharacterHurt", null, 0.6f, 15f);
        SoundCue death = Cue("CharacterDeath", null, 0.7f, 20f);

        WeaponAudioProfile pistolProfile = Asset<WeaponAudioProfile>("PistolAudio");
        WeaponAudioProfile rifleProfile = Asset<WeaponAudioProfile>("RifleAudio");
        foreach (WeaponAudioProfile profile in new[] { pistolProfile, rifleProfile })
        {
            SetMissing(profile, "fire", profile == pistolProfile ? pistol : rifle);
            SetMissing(profile, "reloadStart", reloadStart);
            SetMissing(profile, "reloadComplete", reloadComplete);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:FirearmData", new[] { "Assets/05_Data" }))
        {
            FirearmData firearm = AssetDatabase.LoadAssetAtPath<FirearmData>(AssetDatabase.GUIDToAssetPath(guid));
            if (firearm.WeaponType != WeaponType.Pistol && firearm.WeaponType != WeaponType.Rifle) continue;
            SetMissing(firearm, "audioProfile", firearm.WeaponType == WeaponType.Pistol ? pistolProfile : rifleProfile);
        }
        foreach (string name in new[] { "PlayerAudio", "EnemyAudio" })
        {
            CharacterAudioProfile profile = Asset<CharacterAudioProfile>(name);
            SetMissing(profile, "walkFootstep", walk);
            SetMissing(profile, "sprintFootstep", sprint);
            SetMissing(profile, "hurt", hurt);
            SetMissing(profile, "death", death);
        }
        SetupPrefab(PlayerPath, Asset<CharacterAudioProfile>("PlayerAudio"), true);
        SetupPrefab(EnemyPath, Asset<CharacterAudioProfile>("EnemyAudio"), false);

        AssetDatabase.SaveAssets();
        Debug.Log("Character audio setup complete. Fire cues use existing sci-fi placeholders; assign footstep/reload/voice clips in Assets/05_Data/Audio.");
    }

    private static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = $"{DataRoot}/{name}.asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static SoundCue Cue(string name, string clipName, float volume, float distance)
    {
        bool exists = AssetDatabase.LoadAssetAtPath<SoundCue>($"{DataRoot}/{name}.asset") != null;
        SoundCue cue = Asset<SoundCue>(name);
        if (!exists)
        {
            SerializedObject so = new SerializedObject(cue);
            so.FindProperty("volume").floatValue = volume;
            so.FindProperty("maxDistance").floatValue = distance;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        if (clipName != null && cue.Clip == null)
        {
            string clipPath = AssetDatabase.FindAssets($"{clipName} t:AudioClip")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => System.IO.Path.GetFileNameWithoutExtension(path) == clipName);
            if (clipPath != null) SetMissing(cue, "clip", AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath));
        }
        return cue;
    }

    private static void SetupPrefab(string path, CharacterAudioProfile profile, bool player)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            CharacterAudio audio = root.GetComponent<CharacterAudio>();
            if (audio == null) audio = root.AddComponent<CharacterAudio>();
            Transform audioRoot = Child(root.transform, "Audio");
            foreach (string channel in new[] { "Weapon", "Footstep", "Reload", "Voice" })
            {
                Transform child = Child(audioRoot, channel);
                SoundCuePlayer cuePlayer = child.GetComponent<SoundCuePlayer>();
                if (cuePlayer == null)
                {
                    cuePlayer = child.gameObject.AddComponent<SoundCuePlayer>();
                    AudioSource source = child.GetComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = false;
                    source.spatialBlend = 1f;
                    source.dopplerLevel = 0f;
                    source.rolloffMode = AudioRolloffMode.Linear;
                    source.priority = channel == "Footstep" ? 160 : 100;
                    child.localPosition = channel == "Footstep" ? Vector3.zero : Vector3.up;
                    SerializedObject so = new SerializedObject(cuePlayer);
                    so.FindProperty("playWhilePaused").boolValue = player && channel == "Voice";
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                SetMissing(audio, char.ToLowerInvariant(channel[0]) + channel.Substring(1) + "Player", cuePlayer);
            }
            SetMissing(audio, "characterProfile", profile);
            if (player && root.GetComponentInChildren<CharacterAudioListenerFollow>(true) == null)
                Child(root.transform, "AudioListenerRig").gameObject.AddComponent<CharacterAudioListenerFollow>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Transform Child(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void SetMissing(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty field = so.FindProperty(property);
        if (field == null) throw new InvalidOperationException($"Missing {target.GetType().Name}.{property}");
        if (field.objectReferenceValue != null) return;
        field.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    public static void ApplyBatch()
    {
        Apply();
        CharacterAudioValidation.Run();
    }
}
