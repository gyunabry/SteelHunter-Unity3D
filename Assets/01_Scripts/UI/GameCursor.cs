using UnityEngine;
using UnityEngine.InputSystem;

public class GameCursor : MonoBehaviour
{
    public static GameCursor Current { get; private set; }

    [SerializeField] private CrosshairView view;
    [SerializeField, Min(0.01f)] private float hitFeedbackDuration = 0.15f;

    private CharacterGun playerGun;
    private InputAction pointerAction;

    private bool isCrosshairMode;
    private float hitFeedbackUntil = -1;

    private void Awake()
    {
        Current = this;

        ShowPointer();
    }

    public void Bind(CharacterGun gun, PlayerCharacterInput characterInput)
    {
        playerGun = gun;
        hitFeedbackUntil = -1f;

        PlayerInput input = characterInput != null
            ? characterInput.GetComponent<PlayerInput>()
            : null;

        pointerAction = input?.actions
            ?.FindActionMap("Player")
            ?.FindAction("AimPosition");
    }

    public void Unbind(CharacterGun gun)
    {
        if (playerGun != gun) return;

        playerGun = null;
        pointerAction = null;

        ShowPointer();
    }

    public void ShowCrosshair()
    {
        isCrosshairMode = true;
        hitFeedbackUntil = -1f;

        Cursor.lockState = CursorLockMode.None;

        view.SetScreenPosition(ReadPointerPosition());
        RefreshView();

        Cursor.visible = false;
    }

    public void ShowPointer()
    {
        isCrosshairMode = false;
        hitFeedbackUntil = -1f;

        if (view != null) view.Hide();

        Cursor.lockState = CursorLockMode.None;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
    }

    public void PlayHitFeedback()
    {
        if (!isCrosshairMode) return;

        // 연속으로 명중했을 때 마지막 명중 시점부터 시간을 갱신
        hitFeedbackUntil = Time.unscaledTime + Mathf.Max(0.01f, hitFeedbackDuration);
    }

    private void LateUpdate()
    {
        if (!isCrosshairMode) return;

        view.SetScreenPosition(ReadPointerPosition());
        RefreshView();
    }

    private void RefreshView()
    {
        bool reloading = playerGun != null && playerGun.IsReloading;

        float duration = Mathf.Max(0.01f, hitFeedbackDuration);
        float hitAlpha = Mathf.Clamp01((hitFeedbackUntil - Time.unscaledTime) / duration);

        float progress = reloading ? playerGun.ReloadProgress : 0f;

        view.ShowCrosshair(isCrosshairMode, reloading, hitAlpha, progress);
    }

    private Vector2 ReadPointerPosition()
    {
        if (pointerAction != null) return pointerAction.ReadValue<Vector2>();

        if (Mouse.current != null) return Mouse.current.position.ReadValue();

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void OnDisable()
    {
        if (Current == this)
            ShowPointer();
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }
}
