using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Fire = Animator.StringToHash("Fire");
    private static readonly int IsArmed = Animator.StringToHash("IsArmed");
    private static readonly int PistolLocomotion = Animator.StringToHash("Pistol Upper Body.PistolLocomotion");
    private static readonly int RifleLocomotion = Animator.StringToHash("Rifle Upper Body.RifleLocomotion");
    private static readonly int WeaponIndex = Animator.StringToHash("WeaponIndex");
    private static readonly int IsAiming = Animator.StringToHash("IsAiming");

    [SerializeField] private Animator animator;
    [SerializeField] private WeaponVisuals weaponVisuals;
    [SerializeField] private InverseKinematic inverseKinematic;
    [SerializeField, Min(0.01f)] private float layerBlendSeconds = 0.1f;

    [SerializeField, Min(0f)]
    private float locomotionDampTime = 0.1f;
    [SerializeField, Min(0f)]
    private float stopSpeedThreshold = 0.05f;

    private CharacterMover mover;
    private CharacterGun gun;

    private WeaponType currentWeaponType;

    private int pistolUpperBodyLayer = -1;
    private int rifleUpperBodyLayer = -1;
    private int activeIKLayer = -1;

    private void Awake()
    {
        mover = GetComponent<CharacterMover>();
        gun = GetComponent<CharacterGun>();
        if (weaponVisuals == null) weaponVisuals = GetComponent<WeaponVisuals>();

        pistolUpperBodyLayer = animator.GetLayerIndex("Pistol Upper Body");
        rifleUpperBodyLayer = animator.GetLayerIndex("Rifle Upper Body");
    }

    private void OnEnable()
    {
        weaponVisuals.VisualChanged += HandleVisualChanged;
        gun.Fired += PlayFireAnimation;

        ApplyWeaponState(force: true);
        UpdateWeaponLayers(immediate: true);
    }

    private void Update()
    {
        UpdateLocomotionAnimation();
        UpdateWeaponLayers();

        bool isAiming = currentWeaponType != WeaponType.None &&
            gun.IsAiming &&
            !gun.IsReloading;

        animator.SetBool(IsAiming, isAiming);
    }

    private void OnDisable()
    {
        if (weaponVisuals != null) weaponVisuals.VisualChanged -= HandleVisualChanged;
        if (gun != null) gun.Fired -= PlayFireAnimation;

        // 기존 목표 참조를 해제
        if (inverseKinematic != null) inverseKinematic.SetWeaponIK(null, activeIKLayer);

        currentWeaponType = WeaponType.None;

        if (animator != null)
        {
            animator.ResetTrigger(Fire);
            animator.SetBool(IsArmed, false);
            animator.SetInteger(WeaponIndex, (int)WeaponType.None);
            animator.SetBool(IsAiming, false);

            UpdateWeaponLayers(immediate: true);
        }

    }

    /// <summary>
    /// 속도에 따른 블렌드 트리 애니메이션 제어
    /// </summary>
    private void UpdateLocomotionAnimation()
    {
        Vector3 worldVelocity = mover.PlaneVelocity;

        if (worldVelocity.sqrMagnitude < stopSpeedThreshold * stopSpeedThreshold)
        {
            worldVelocity = Vector3.zero;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        animator.SetFloat(
            MoveX, localVelocity.x,
            locomotionDampTime, Time.deltaTime);

        animator.SetFloat(
            MoveY, localVelocity.z,
            locomotionDampTime, Time.deltaTime);
    }

    private void HandleVisualChanged()
    {
        ApplyWeaponState();
    }

    private void ApplyWeaponState(bool force = false)
    {
        WeaponType nextType = weaponVisuals.CurrentWeaponType;

        bool typeChanged = force || nextType != currentWeaponType;

        if (typeChanged)
        {
            currentWeaponType = nextType;

            int locomotionState = 0;
            bool isArmed = currentWeaponType != WeaponType.None;

            switch (currentWeaponType)
            {
                case WeaponType.Pistol:
                    activeIKLayer = pistolUpperBodyLayer;
                    locomotionState = PistolLocomotion;
                    break;

                case WeaponType.Rifle:
                    activeIKLayer = rifleUpperBodyLayer;
                    locomotionState = RifleLocomotion;
                    break;

                default:
                    // 기존 레이어에 null IK 목표를 전달할 수 있게 유지
                    if (activeIKLayer < 0) activeIKLayer = pistolUpperBodyLayer;
                    break;
            }

            animator.ResetTrigger(Fire);
            animator.SetBool(IsArmed, isArmed);
            animator.SetInteger(WeaponIndex, (int)currentWeaponType);

            if (locomotionState != 0 && activeIKLayer >= 0)
                animator.Play(locomotionState, activeIKLayer, 0f);
        }

        // 같은 Rifle끼리 교체해도 새 모델의 손잡이는 다시 연결
        if (inverseKinematic != null)
        {
            inverseKinematic.SetWeaponIK(weaponVisuals.LeftHandGrip, activeIKLayer);
        }
    }

    private void UpdateWeaponLayers(bool immediate = false)
    {
        float step = immediate
            ? 1f
            : Time.deltaTime / Mathf.Max(layerBlendSeconds, 0.01f);

        SetLayerWeight(pistolUpperBodyLayer, currentWeaponType == WeaponType.Pistol, step);
        SetLayerWeight(rifleUpperBodyLayer, currentWeaponType == WeaponType.Rifle, step);
    }

    private void SetLayerWeight(int layerIndex, bool active, float step)
    {
        if (layerIndex < 0) return;

        float weight = Mathf.MoveTowards(
            animator.GetLayerWeight(layerIndex),
            active ? 1f : 0f,
            step);

        animator.SetLayerWeight(layerIndex, weight);
    }

    private void PlayFireAnimation()
    {
        if (currentWeaponType == WeaponType.None) return;

        animator.SetTrigger(Fire);
    }
}
