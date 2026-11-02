using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Isolated integration checks; never edits scenes, prefabs, or authored weapon data.</summary>
[InitializeOnLoad]
public static class FirearmHandlingValidation
{
    private const string BatchKey = "SteelHunter.FirearmHandling.Batch";
    private const string EditorRunKey = "SteelHunter.FirearmHandling.EditorRun";
    private const string CameraOptionKey = "SteelHunter.FirearmHandling.SaveCameraDuringPlay";
    private const string RequestPath = "Temp/FirearmHandlingValidation.request";
    private const string ResultPath = "Temp/FirearmHandlingValidation.result";
    private static int checks;

    static FirearmHandlingValidation()
    {
        EditorApplication.update += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
            File.Delete(RequestPath);
            RunInEditor();
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(EditorRunKey, false))
            {
                Unity.Cinemachine.Editor.SaveDuringPlay.Enabled = SessionState.GetBool(CameraOptionKey, false);
                SessionState.SetBool(EditorRunKey, false);
            }
            if (state != PlayModeStateChange.EnteredPlayMode ||
                (!SessionState.GetBool(BatchKey, false) && !SessionState.GetBool(EditorRunKey, false))) return;
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Run();
                    SessionState.SetBool(BatchKey, false);
                    if (SessionState.GetBool(EditorRunKey, false)) EditorApplication.ExitPlaymode();
                    else EditorApplication.Exit(0);
                }
                catch (Exception exception)
                {
                    File.WriteAllText(ResultPath, exception.ToString());
                    Debug.LogException(exception);
                    SessionState.SetBool(BatchKey, false);
                    if (SessionState.GetBool(EditorRunKey, false)) EditorApplication.ExitPlaymode();
                    else EditorApplication.Exit(1);
                }
            };
        };
    }

    [MenuItem("Tools/SteelHunter/Validate Firearm Handling")]
    public static void RunInEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before starting validation.");
        SessionState.SetBool(CameraOptionKey, Unity.Cinemachine.Editor.SaveDuringPlay.Enabled);
        Unity.Cinemachine.Editor.SaveDuringPlay.Enabled = false;
        SessionState.SetBool(EditorRunKey, true);
        EditorApplication.EnterPlaymode();
    }

    // Unity -batchmode -nographics -projectPath ... -executeMethod FirearmHandlingValidation.RunBatch
    public static void RunBatch()
    {
        SessionState.SetBool(BatchKey, true);
        // Use an empty scene only in the standalone batch process.
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Tools/SteelHunter/Validate Firearm Handling (Play Mode)")]
    public static void Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode before validation.");
        checks = 0;
        var randomState = UnityEngine.Random.state;
        float previousTimeScale = Time.timeScale;
        var data = ScriptableObject.CreateInstance<FirearmData>();
        var ammo = ScriptableObject.CreateInstance<AmmoData>();
        var caliber = ScriptableObject.CreateInstance<CaliberData>();
        var character = ScriptableObject.CreateInstance<CharacterData>();
        var attachment = ScriptableObject.CreateInstance<GunAttachmentData>();
        var objects = new GameObject("Firearm Handling Validation");
        objects.transform.position = new Vector3(10000f, 100f, 10000f);
        var ownerObject = new GameObject("Validation Owner (inactive)");
        ownerObject.SetActive(false);
        try
        {
            Time.timeScale = 1f;
            ValidateInput();
            Set(data, "caliber", caliber);
            Set(ammo, "caliber", caliber);
            var provider = new TestProvider { EquippedWeapon = new WeaponInstance(data) };
            WeaponInstance weapon = provider.EquippedWeapon;
            var health = objects.AddComponent<CharacterHealth>();
            health.Initialize(100f);
            var owner = ownerObject.AddComponent<CharacterRoot>();
            Set(owner, "<Health>k__BackingField", health);
            var gun = objects.AddComponent<CharacterGun>();
            var mover = objects.AddComponent<CharacterMover>();
            Set(character, "acceleration", 100000f);
            mover.Initialize(character);
            var aim = objects.AddComponent<CharacterAim>();
            aim.Initialize(character);
            var commands = objects.AddComponent<CharacterCommandReceiver>();
            commands.Initialize(mover, aim, gun, health);

            var visualObject = new GameObject("Validation Visuals");
            visualObject.transform.SetParent(objects.transform);
            visualObject.SetActive(false);
            var visuals = visualObject.AddComponent<WeaponVisuals>();
            var visual = visualObject.AddComponent<Weapon>();
            Set(visual, "muzzlePoint", visualObject.transform);
            Set(visuals, "<CurrentData>k__BackingField", data);
            Set(visuals, "<CurrentVisual>k__BackingField", visual);
            var bulletObject = new GameObject("Validation Bullet Template");
            bulletObject.transform.SetParent(objects.transform);
            bulletObject.SetActive(false);
            var bulletPrefab = bulletObject.AddComponent<BulletProjectile>();
            Set(gun, "weaponVisuals", visuals);
            Set(gun, "bulletPrefab", bulletPrefab);
            Set(gun, "owner", owner);
            Set(gun, "weaponProvider", provider);
            Set(gun, "ammoSource", provider);
            Set(gun, "initialized", true);
            Invoke(gun, "Subscribe");

            var inputObject = new GameObject("Validation Input (inactive)");
            inputObject.transform.SetParent(objects.transform);
            inputObject.SetActive(false);
            var input = inputObject.AddComponent<PlayerCharacterInput>();
            Set(input, "commands", commands);

            Check(!gun.IsAiming && Near(gun.CurrentSpreadAngle, 5f), "Default hip spread");
            commands.SetAiming(true);
            Check(gun.IsAiming && Near(gun.AimProgress, 0f), "Aim begins without instant accuracy");
            Check(Near(gun.MovementSpeedCoefficient, 0.6f), "Aim movement penalty begins immediately");
            Set(gun, "aimStartedTime", Time.time - 0.15f);
            Check(Near(gun.CurrentSpreadAngle, 3f), "Halfway through AimTime gives intermediate spread");
            commands.SetAiming(true);
            Check(Near(gun.CurrentSpreadAngle, 3f), "Holding aim does not restart its timer");
            Set(gun, "aimStartedTime", Time.time - 1f);
            Check(Near(gun.CurrentSpreadAngle, 1f), "Full aim reaches AimSpread");

            UnityEngine.Random.InitState(4271);
            Vector3 direction = new Vector3(1f, 0.2f, 1f).normalized;
            ValidateShots(gun, weapon, ammo, bulletPrefab, direction, 1f);
            commands.SetAiming(false);
            ValidateShots(gun, weapon, ammo, bulletPrefab, direction, 5f);
            Check(Near(gun.MovementSpeedCoefficient, 1f), "Releasing aim restores movement");

            commands.SetMove(Vector3.forward, false);
            Invoke(mover, "Update");
            Check(Near(Get<Vector3>(mover, "horizontalVelocity").z, character.WalkSpeed), "Normal walk speed");
            commands.SetMove(Vector3.forward, true);
            Invoke(mover, "Update");
            Check(Near(Get<Vector3>(mover, "horizontalVelocity").z, character.SprintSpeed), "Normal sprint speed");

            commands.SetAiming(true);
            commands.SetMove(Vector3.forward, true);
            Set(mover, "horizontalVelocity", Vector3.forward * 7.5f);
            Invoke(mover, "Update");
            Check(Near(Get<Vector3>(mover, "horizontalVelocity").z, character.WalkSpeed * 0.6f) && !mover.IsSprinting,
                "Aim suppresses sprint and uses aimed walk speed");
            commands.Stop();
            Check(!gun.IsAiming && mover.Velocity == Vector3.zero, "Stop clears aim and movement");
            commands.SetAiming(true);
            input.SetUIBlocked(true);
            Check(!gun.IsAiming && Get<bool>(input, "aimNeedsRelease"), "Opening UI clears aim and requires release");
            input.SetUIBlocked(false);
            commands.SetAiming(true);
            typeof(PlayerCharacterInput).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(input, new object[] { false });
            Check(!gun.IsAiming, "Losing application focus clears aim");

            Check(weapon.TryAttach(new ItemStack(attachment, 1), out _, out _), "Attachment can be fitted");
            weapon.TryDetach(attachment.Slot, out _, out _);
            Set(attachment, "modifiers", new[]
            {
                new GunStatModifier { stat = GunStat.HipFireSpread, percentValue = -0.5f },
                new GunStatModifier { stat = GunStat.AimSpread, percentValue = -0.5f },
                new GunStatModifier { stat = GunStat.MoveSpeedCoefficient, percentValue = -0.2f },
                new GunStatModifier { stat = GunStat.AimTime, percentValue = -1f }
            });
            Check(weapon.TryAttach(new ItemStack(attachment, 1), out _, out _), "Handling attachment can be fitted");
            Check(Near(gun.CurrentSpreadAngle, 2.5f) && Near(gun.MovementSpeedCoefficient, 0.8f),
                "Hip spread and movement consume attachment-adjusted stats");
            commands.SetAiming(true);
            Check(Near(gun.CurrentSpreadAngle, 0.5f) && Near(gun.MovementSpeedCoefficient, 0.48f),
                "Zero AimTime and aimed movement consume attachment-adjusted stats");
            ValidateShots(gun, weapon, ammo, bulletPrefab, direction, 0.5f);
            weapon.TryDetach(attachment.Slot, out _, out _);

            Set(attachment, "modifiers", new[]
            {
                new GunStatModifier { stat = GunStat.HipFireSpread, percentValue = -1f },
                new GunStatModifier { stat = GunStat.AimSpread, percentValue = -1f }
            });
            weapon.TryAttach(new ItemStack(attachment, 1), out _, out _);
            ValidateShots(gun, weapon, ammo, bulletPrefab, direction, 0f);
            weapon.TryDetach(attachment.Slot, out _, out _);

            Set(weapon, "<LoadedRounds>k__BackingField", 1);
            Set(weapon, "<LoadedAmmo>k__BackingField", ammo);
            Set(weapon, "<SelectedAmmo>k__BackingField", ammo);
            Check(commands.TryReload() && !gun.IsAiming, "Reload cancels aim");
            commands.SetAiming(true);
            Check(!gun.IsAiming, "Reload blocks aiming");
            gun.CancelReload();
            commands.SetAiming(true);
            provider.NotifyWeaponChanged();
            Check(!gun.IsAiming && Near(gun.AimProgress, 0f), "Weapon change clears previous accuracy");
            commands.SetAiming(true);
            provider.EquippedWeapon = null;
            provider.NotifyWeaponChanged();
            commands.SetAiming(true);
            Check(!gun.IsAiming && Near(gun.MovementSpeedCoefficient, 1f), "Unarmed movement is unpenalized");
            provider.EquippedWeapon = weapon;
            provider.NotifyWeaponChanged();
            commands.SetAiming(true);
            gun.enabled = false;
            Check(!gun.IsAiming && !gun.CanFireNow, "Disabling the gun clears aim and blocks firing");
            gun.enabled = true;
            commands.SetAiming(true);
            Time.timeScale = 0f;
            commands.Stop();
            commands.SetAiming(true);
            Check(!gun.IsAiming && !commands.TryFire(), "Pause blocks aiming and firing");
            Time.timeScale = 1f;
            commands.SetAiming(true);
            health.ApplyDamage(1000f, false);
            Check(!gun.IsAiming, "Death clears aim");
            commands.SetAiming(true);
            Check(!gun.IsAiming, "Dead characters cannot aim");

            var badStats = new System.Collections.Generic.Dictionary<GunStat, float>
            {
                [GunStat.AimTime] = -1f, [GunStat.HipFireSpread] = -5f,
                [GunStat.AimSpread] = 100f, [GunStat.MoveSpeedCoefficient] = -2f,
                [GunStat.AimMoveCoefficient] = 3f
            };
            var stats = (WeaponStats)Activator.CreateInstance(typeof(WeaponStats),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { badStats }, null);
            Check(stats.AimTime == 0f && stats.HipFireSpread == 0f && stats.AimSpread == 0f &&
                stats.MoveSpeedCoefficient == 0f && stats.AimMoveCoefficient == 1f,
                "Invalid attachment results cannot cause reverse movement or worse aimed spread");
            File.WriteAllText(ResultPath, $"PASS: {checks} checks, including 1024 actual projectile spawns.");
            Debug.Log($"Firearm Handling VALIDATION PASSED ({checks} checks).");
        }
        finally
        {
            Time.timeScale = previousTimeScale;
            UnityEngine.Random.state = randomState;
            UnityEngine.Object.DestroyImmediate(objects);
            UnityEngine.Object.DestroyImmediate(ownerObject);
            foreach (var asset in new UnityEngine.Object[] { data, ammo, caliber, character, attachment })
                UnityEngine.Object.DestroyImmediate(asset);
        }
    }

    private static void ValidateInput()
    {
        var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var action = input.FindActionMap("Player", true).FindAction("Aim", true);
        Check(action.type == InputActionType.Button && action.bindings.Any(binding =>
            binding.path == "<Mouse>/rightButton" && binding.groups.Contains("Keyboard&Mouse")),
            "Player/Aim is bound to right mouse button");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Character/PlayerCharacter.prefab");
        Check(prefab.GetComponent<PlayerInput>().actions == input, "Player prefab uses the updated input asset");
    }

    private static void ValidateShots(CharacterGun gun, WeaponInstance weapon, AmmoData ammo,
        BulletProjectile prefab, Vector3 direction, float limit)
    {
        bool left = false, right = false;
        for (int i = 0; i < 256; i++)
        {
            Set(weapon, "<LoadedRounds>k__BackingField", 2);
            Set(weapon, "<LoadedAmmo>k__BackingField", ammo);
            Set(gun, "nextFireTime", 0f);
            Check(gun.TryFire(direction), "Valid shot succeeds");
            var shot = Resources.FindObjectsOfTypeAll<BulletProjectile>().Single(bullet =>
                bullet.gameObject.scene.IsValid() && bullet.name == prefab.name + "(Clone)");
            try
            {
                Vector3 actual = Get<Vector3>(shot, "direction");
                float yaw = Vector3.SignedAngle(Vector3.ProjectOnPlane(direction, Vector3.up),
                    Vector3.ProjectOnPlane(actual, Vector3.up), Vector3.up);
                Check(Mathf.Abs(yaw) <= limit + 0.001f && Near(actual.y, direction.y) && Near(actual.magnitude, 1f),
                    "Projectile stays inside spread and preserves pitch and speed");
                Check(weapon.LoadedRounds == 1, "Each shot consumes exactly one round");
                left |= yaw < -limit * 0.5f;
                right |= yaw > limit * 0.5f;
            }
            finally { UnityEngine.Object.DestroyImmediate(shot.gameObject); }
        }
        Check(limit == 0f || (left && right), "Nonzero spread reaches both sides of the cursor");
    }

    private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.001f;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
    }
    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Get<T>(object target, string field) => (T)target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    private sealed class TestProvider : IWeaponProvider, IAmmoSource
    {
        public WeaponInstance EquippedWeapon { get; set; }
        public event Action WeaponChanged;
        public void NotifyWeaponChanged() => WeaponChanged?.Invoke();
        public int GetQuantity(AmmoData ammo) => 100;
        public int ConsumeUpTo(AmmoData ammo, int amount) => amount;
    }
}
