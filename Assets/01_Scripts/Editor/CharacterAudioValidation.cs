using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CharacterAudioValidation
{
    private const string BatchKey = "SteelHunter.CharacterAudio.Batch";
    private const string RequestPath = "Temp/CharacterAudioValidation.request";
    private const string ResultPath = "Temp/CharacterAudioValidation.result";
    private const string RestoreCameraKey = "SteelHunter.CharacterAudio.RestoreCameraOption";
    private const string CameraOptionKey = "SteelHunter.CharacterAudio.SaveCameraDuringPlay";
    private static int checks;
    private static double playStarted;

    static CharacterAudioValidation()
    {
        EditorApplication.update += PollRequest;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) RestoreEditorOptions();
            if (!SessionState.GetBool(BatchKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playStarted = EditorApplication.timeSinceStartup;
                EditorApplication.update += RunPlayWhenReady;
            }
        };
    }

    private static void PollRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        RunInEditor();
    }

    [MenuItem("Tools/SteelHunter/Validate Character Audio In Play Mode")]
    public static void RunInEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before validation.");
        try
        {
            CharacterAudioSetup.Apply();
            Run();
            File.WriteAllText(ResultPath, $"Edit checks passed: {checks}. Play checks pending.");
            // Validation must not propagate test camera movement back to an unsaved scene.
            SessionState.SetBool(CameraOptionKey, Unity.Cinemachine.Editor.SaveDuringPlay.Enabled);
            SessionState.SetBool(RestoreCameraKey, true);
            Unity.Cinemachine.Editor.SaveDuringPlay.Enabled = false;
            SessionState.SetBool(BatchKey, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception) { Finish(exception); }
    }

    private static void RestoreEditorOptions()
    {
        if (!SessionState.GetBool(RestoreCameraKey, false)) return;
        Unity.Cinemachine.Editor.SaveDuringPlay.Enabled = SessionState.GetBool(CameraOptionKey, false);
        SessionState.SetBool(RestoreCameraKey, false);
    }

    [MenuItem("Tools/SteelHunter/Validate Character Audio")]
    public static void Run()
    {
        checks = 0;
        SoundCue cue = ScriptableObject.CreateInstance<SoundCue>();
        CharacterAudioProfile profile = ScriptableObject.CreateInstance<CharacterAudioProfile>();
        try
        {
            Set(cue, "volume", 3f);
            Set(cue, "minDistance", -5f);
            Set(cue, "maxDistance", -10f);
            Check(cue.Volume == 1f && cue.MinDistance > 0f && cue.MaxDistance > cue.MinDistance,
                "Invalid authored volume/distances are clamped without recursive access");
            Set(profile, "walkFootstep", cue);
            Check(profile.SprintFootstep == cue, "Missing sprint cue falls back to walk");
            Check(typeof(SoundCue).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Count(field => field.FieldType == typeof(AudioClip)) == 1,
                "Each cue stores exactly one clip");
            Check(typeof(SoundCue).GetMethod("TrySelectClip") == null,
                "Single-clip cues do not include a selection API");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cue);
            UnityEngine.Object.DestroyImmediate(profile);
        }
        ValidatePrefab(CharacterAudioSetup.PlayerPath, true);
        ValidatePrefab(CharacterAudioSetup.EnemyPath, false);
        foreach (string guid in AssetDatabase.FindAssets("t:FirearmData", new[] { "Assets/05_Data" }))
        {
            FirearmData firearm = AssetDatabase.LoadAssetAtPath<FirearmData>(AssetDatabase.GUIDToAssetPath(guid));
            if (firearm.WeaponType != WeaponType.Pistol && firearm.WeaponType != WeaponType.Rifle) continue;
            Check(firearm.AudioProfile != null && firearm.AudioProfile.Fire != null &&
                firearm.AudioProfile.Fire.Clip != null, $"Fire cue assigned: {firearm.name}");
        }
        Debug.Log($"CharacterAudio EDIT VALIDATION PASSED ({checks} checks).");
    }

    private static void ValidatePrefab(string path, bool player)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Check(root != null && root.GetComponent<CharacterAudio>() != null, $"Audio component: {path}");
        SerializedObject audio = new SerializedObject(root.GetComponent<CharacterAudio>());
        SoundCuePlayer[] channels = new[] { "weaponPlayer", "footstepPlayer", "reloadPlayer", "voicePlayer" }
            .Select(name => audio.FindProperty(name).objectReferenceValue as SoundCuePlayer).ToArray();
        Check(channels.All(channel => channel != null) && channels.Distinct().Count() == 4,
            $"Four independently controllable channels: {path}");
        Check(channels.All(channel => !channel.GetComponent<AudioSource>().playOnAwake &&
            channel.GetComponent<AudioSource>().spatialBlend == 1f), $"3D source configuration: {path}");
        Check(root.GetComponentsInChildren<AudioListener>(true).Length == (player ? 1 : 0),
            $"Player-only listener: {path}");
    }

    public static void RunBatch()
    {
        try
        {
            CharacterAudioSetup.Apply();
            Run();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(BatchKey, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception) { Finish(exception); }
    }

    private static void RunPlayWhenReady()
    {
        double elapsed = EditorApplication.timeSinceStartup - playStarted;
        if (elapsed < 0.3 || Time.deltaTime <= 0f)
        {
            if (elapsed > 30) Finish(new TimeoutException("Play mode did not advance."));
            return;
        }
        EditorApplication.update -= RunPlayWhenReady;
        try { RunPlayChecks(); Finish(null); }
        catch (Exception exception) { Finish(exception); }
    }

    private static void RunPlayChecks()
    {
        checks = 0;
        GameAudioPause.SetPaused(false);
        Time.timeScale = 1f;
        if (Camera.main == null)
        {
            GameObject camera = new GameObject("Audio Validation Camera", typeof(Camera));
            camera.tag = "MainCamera";
            if (!UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
                .Any(listener => listener.isActiveAndEnabled)) camera.AddComponent<AudioListener>();
        }
        GameObject enemy = UnityEngine.Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<GameObject>(CharacterAudioSetup.EnemyPath),
            new Vector3(500f, 0.1f, 500f), Quaternion.identity);
        enemy.GetComponent<EnemyCharacterBrain>().enabled = false;
        CharacterRoot owner = enemy.GetComponent<CharacterRoot>();
        CharacterGun gun = enemy.GetComponent<CharacterGun>();
        CharacterAudio audio = enemy.GetComponent<CharacterAudio>();
        Check(owner.IsInitialized && gun.CanFireNow, "Enemy uses real prefab/loadout initialization");
        int shots = 0, starts = 0, completes = 0, cancels = 0;
        FirearmData shotData = null;
        gun.ShotFired += (data, position) => { shots++; shotData = data; };
        gun.ReloadStarted += data => starts++;
        gun.ReloadCompleted += data => completes++;
        gun.ReloadCancelled += () => cancels++;
        Check(gun.TryFire(Vector3.forward) && shots == 1 && shotData.AudioProfile != null,
            "Successful shot emits its actual weapon data");
        Check(!gun.TryFire(Vector3.forward) && shots == 1, "Fire cooldown does not emit a second shot");
        Check(gun.TryReload() && starts == 1, "Reload starts once after spending a round");
        Check(!gun.TryFire(Vector3.forward) && shots == 1, "Reload blocks shot/audio events");
        gun.CancelReload();
        gun.CancelReload();
        Check(cancels == 1 && !gun.IsReloading, "Reload cancellation is idempotent");
        Check(gun.TryReload() && starts == 2, "Reload can restart after cancellation");
        Set(gun, "reloadEndTime", Time.time - 1f);
        Invoke(gun, "Update");
        Check(completes == 1 && gun.LoadedRounds == gun.MagazineSize, "Actual ammo transfer emits completion");
        Check(AudioSubscribers(gun) == 1, "Audio subscribes once");
        audio.enabled = false;
        Check(AudioSubscribers(gun) == 0, "Disabled audio unsubscribes");
        audio.enabled = true;
        Check(AudioSubscribers(gun) == 1, "Re-enabled audio resubscribes once");

        SoundCuePlayer player = enemy.transform.Find("Audio/Weapon").GetComponent<SoundCuePlayer>();
        SoundCue empty = ScriptableObject.CreateInstance<SoundCue>();
        Check(!player.Play(null) && !player.Play(empty), "Missing audio data is a safe no-op");
        GameAudioPause.SetPaused(true);
        Check(!player.Play(shotData.AudioProfile.Fire), "Paused world audio rejects new sounds");
        GameAudioPause.SetPaused(false);
        Check(player.Play(shotData.AudioProfile.Fire), "Assigned single clip is accepted after resume");
        AudioSource source = player.GetComponent<AudioSource>();
        Check(source.minDistance == shotData.AudioProfile.Fire.MinDistance &&
            source.maxDistance == shotData.AudioProfile.Fire.MaxDistance, "Cue distances reach the audio source");

        GameObject pauseVoice = new GameObject("Paused Voice Validation");
        pauseVoice.SetActive(false);
        SoundCuePlayer voice = pauseVoice.AddComponent<SoundCuePlayer>();
        Set(voice, "playWhilePaused", true);
        pauseVoice.SetActive(true);
        GameAudioPause.SetPaused(true);
        Check(voice.Play(shotData.AudioProfile.Fire), "Voice/UI source can explicitly opt out of listener pause");
        voice.enabled = false;
        Check(!voice.Play(shotData.AudioProfile.Fire), "Disabled audio player rejects playback");
        GameAudioPause.SetPaused(false);
        UnityEngine.Object.DestroyImmediate(pauseVoice);

        GameObject fallbackCamera = new GameObject("Listener Fallback Validation");
        fallbackCamera.SetActive(false);
        Camera fallbackOutput = fallbackCamera.AddComponent<Camera>();
        AudioListener fallbackListener = fallbackCamera.AddComponent<AudioListener>();
        GameObject rig = new GameObject("Listener Rig Validation");
        rig.SetActive(false);
        CharacterAudioListenerFollow follow = rig.AddComponent<CharacterAudioListenerFollow>();
        follow.SetOutputCamera(fallbackOutput);
        fallbackCamera.SetActive(true);
        rig.SetActive(true);
        Check(!fallbackListener.enabled, "Player listener takes over only its output camera's listener");
        rig.SetActive(false);
        Check(fallbackListener.enabled, "Camera listener is restored when the player listener is disabled");
        UnityEngine.Object.DestroyImmediate(rig);
        UnityEngine.Object.DestroyImmediate(fallbackCamera);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(500f, -0.5f, 500f);
        floor.transform.localScale = new Vector3(20f, 1f, 20f);
        Physics.SyncTransforms();
        enemy.GetComponent<CharacterController>().Move(Vector3.down * 2f);
        CharacterMover mover = enemy.GetComponent<CharacterMover>();
        Set(mover, "<Velocity>k__BackingField", Vector3.right * 5f);
        CharacterNoiseEmitter noise = enemy.GetComponent<CharacterNoiseEmitter>();
        int steps = 0, heard = 0;
        noise.Stepped += (position, sprint) => steps++;
        Action<CharacterSound> onSound = sound => { if (sound.Source == owner) heard++; };
        CharacterStimulusBus.SoundReported += onSound;
        try
        {
            Set(noise, "walkRadius", 0f);
            Set(noise, "sprintRadius", 0f);
            Set(noise, "distanceSinceStep", 2f);
            Invoke(noise, "Update");
            Check(steps == 1 && heard == 0, "Zero AI hearing radius still emits an audible footstep event");
            Set(mover, "<Velocity>k__BackingField", Vector3.zero);
            Set(noise, "distanceSinceStep", 2f);
            Invoke(noise, "Update");
            Check(steps == 1, "Stationary character does not emit footsteps");
        }
        finally { CharacterStimulusBus.SoundReported -= onSound; }
        UnityEngine.Object.DestroyImmediate(empty);
        Debug.Log($"CharacterAudio PLAY VALIDATION PASSED ({checks} checks).");
    }

    private static int AudioSubscribers(CharacterGun gun)
    {
        Delegate listeners = (Delegate)typeof(CharacterGun).GetField("ShotFired",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gun);
        return listeners?.GetInvocationList().Count(handler => handler.Target is CharacterAudio) ?? 0;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Audio validation failed: " + message);
        checks++;
    }

    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static void Invoke(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    private static void Finish(Exception exception)
    {
        EditorApplication.update -= RunPlayWhenReady;
        SessionState.SetBool(BatchKey, false);
        GameAudioPause.SetPaused(false);
        Time.timeScale = 1f;
        if (exception != null) Debug.LogException(exception);
        File.WriteAllText(ResultPath, exception == null
            ? $"PASS: character audio edit and play checks completed. Play checks: {checks}."
            : exception.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(exception == null ? 0 : 1);
        else if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else RestoreEditorOptions();
    }
}
