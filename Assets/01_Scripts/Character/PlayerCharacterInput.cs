using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerAimResolver))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterCommandReceiver))]
[DefaultExecutionOrder(-100)]
public sealed class PlayerCharacterInput : MonoBehaviour
{
    [SerializeField] private Camera aimCamera;
    [SerializeField] private MouseLookAhead mouseLookAhead;

    public void SetCamera(Camera camera)
    {
        aimCamera = camera;
    }

    private ICharacterCommands commands;
    private CharacterEquipment equipment;

    private InputAction moveAction;
    private InputAction aimPositionAction;
    private InputAction aimAction;
    private InputAction attackAction;
    private InputAction sprintAction;
    private InputAction reloadAction;
    private InputAction selectAmmoAction;

    private PlayerAimResolver aimResolver;

    private bool attackNeedsRelease;
    private bool aimNeedsRelease;
    private bool uiBlocked;
    private int uiClosedFrame = -1;

    public event Action SelectAmmoRequested;

    private void Awake()
    {
        commands = GetComponent<ICharacterCommands>();
        aimResolver = GetComponent<PlayerAimResolver>();
        equipment = GetComponent<CharacterEquipment>(); 

        if (aimCamera == null) aimCamera = Camera.main;

        PlayerInput playerInput = GetComponent<PlayerInput>();
        InputActionMap playerMap = playerInput.actions?.FindActionMap("Player");
        if (aimCamera == null || playerMap == null)
        {
            Debug.LogError("카메라와 InputActionMap이 필요합니다.", this);
            enabled = false;
            return;
        }

        moveAction = playerMap.FindAction("Move");
        aimPositionAction = playerMap.FindAction("AimPosition");
        aimAction = playerMap.FindAction("Aim");
        attackAction = playerMap.FindAction("Attack");
        sprintAction = playerMap.FindAction("Sprint");
        reloadAction = playerMap.FindAction("Reload");
        selectAmmoAction = playerMap.FindAction("SelectAmmo");

        if (moveAction == null || aimPositionAction == null || aimAction == null || attackAction == null ||
            sprintAction == null || reloadAction == null || selectAmmoAction == null)
        {
            Debug.LogError("Action을 찾지 못 했습니다.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        // UI가 닫히는 프레임에는 조작을 막도록 제한
        if (uiBlocked || Time.frameCount == uiClosedFrame || Time.timeScale <= 0f || !Application.isFocused)
        {
            commands.Stop();
            return;
        }

        // 1,2 : 주무기, 보조무기 스위칭
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && equipment != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                equipment.TrySelectWeapon(EquipmentSlot.PrimaryWeapon);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                equipment.TrySelectWeapon(EquipmentSlot.SecondaryWeapon);
            }
        }

        // T : 탄약 종류 선택
        if (selectAmmoAction.WasPressedThisFrame())
        {
            SelectAmmoRequested?.Invoke();
        }

        // 이동 조작
        Vector2 axis = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        Vector3 cameraForward = Vector3.ProjectOnPlane(aimCamera.transform.forward, Vector3.up);
        Vector3 cameraRight = Vector3.ProjectOnPlane(aimCamera.transform.right, Vector3.up);

        cameraForward = cameraForward.sqrMagnitude > 0.001f
            ? cameraForward.normalized 
            : Vector3.forward;

        cameraRight = cameraRight.sqrMagnitude > 0.001f
            ? cameraRight.normalized 
            : Vector3.right;

        Vector3 worldMove = Vector3.ClampMagnitude(cameraRight * axis.x + cameraForward * axis.y, 1f);

        if (!aimAction.IsPressed()) aimNeedsRelease = false;
        commands.SetAiming(!aimNeedsRelease && aimAction.IsPressed());
        commands.SetMove(worldMove, sprintAction.IsPressed());

        Vector2 screenPosition = aimPositionAction.ReadValue<Vector2>();
        if (aimResolver != null &&
            aimResolver.TryResolve(aimCamera, screenPosition, out Vector3 aimPoint))
        {
            commands.SetAim(aimPoint);
        }

        if (!attackAction.IsPressed()) attackNeedsRelease = false;
        if (reloadAction.WasPressedThisFrame()) commands.TryReload();
        if (!attackNeedsRelease && attackAction.IsPressed()) commands.TryFire();
    }

    public void SetUIBlocked(bool blocked)
    {
        if (uiBlocked == blocked) return;

        uiBlocked = blocked;
        mouseLookAhead?.SetUIBlocked(blocked);
        attackNeedsRelease = true;
        aimNeedsRelease = true;
        commands?.Stop();
        if (!blocked) uiClosedFrame = Time.frameCount;
    }

    private void OnDisable()
    {
        commands?.Stop();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) return;
        attackNeedsRelease = true;
        aimNeedsRelease = true;
        commands?.Stop();
    }
}
