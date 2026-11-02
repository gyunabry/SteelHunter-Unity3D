using System;
using UnityEngine;
using UnityEngine.Rendering;

public class WeaponVisuals : MonoBehaviour
{
    [SerializeField] private Transform weaponSocket;
    [Tooltip("Optional field-of-view material using the weapon atlas. Also disables weapon shadows.")]
    [SerializeField] private Material visibilityMaterial;

    private IWeaponProvider weaponProvider;
    
    public FirearmData CurrentData { get; private set; }
    public Weapon CurrentVisual { get; private set; }

    public WeaponType CurrentWeaponType =>
        CurrentData != null ? CurrentData.WeaponType : WeaponType.None;

    public Transform MuzzlePoint =>
        CurrentVisual != null ? CurrentVisual.MuzzlePoint : null;

    public Transform LeftHandGrip =>
        CurrentVisual != null ? CurrentVisual.LeftHandGrip : null;

    public event Action VisualChanged;

    public void Initialize(IWeaponProvider provider)
    {
        weaponProvider = provider;

        if (weaponProvider == null || weaponSocket == null)
        {
            Debug.LogError("WeaponProvider와 WeaponSocket이 필요합니다.", this);
            return;
        }

        SubscribeAndRefresh();
    }

    private void OnEnable()
    {
        SubscribeAndRefresh();
    }

    private void OnDisable()
    {
        if (weaponProvider != null) weaponProvider.WeaponChanged -= RefreshVisual;
        ClearVisual();
        VisualChanged?.Invoke();
    }

    private void RefreshVisual()
    {
        FirearmData nextData = weaponProvider.EquippedWeapon?.Data;

        // 같은 무기 데이터라면 현재 모델을 재사용
        if (nextData == CurrentData && (nextData == null || CurrentVisual != null))
        {
            return;
        }

        ClearVisual();
        CurrentData = nextData;

        if (nextData != null)
        {
            Weapon prefab = nextData.VisualPrefab;

            if (prefab == null)
            {
                Debug.LogError($"{nextData.name} : VisualPrefab이 없습니다.", this);
            }
            else
            {
                CurrentVisual = Instantiate(prefab, weaponSocket, false);
                ApplyVisibilityMaterial();

                CurrentVisual.transform.localPosition = Vector3.zero;
                CurrentVisual.transform.localRotation = Quaternion.identity;
                CurrentVisual.gameObject.SetActive(true);

                if (CurrentVisual.MuzzlePoint == null)
                {
                    Debug.LogError($"{nextData.name} : MuzzlePoint가 없습니다.", CurrentVisual);
                }
            }
        }

        VisualChanged?.Invoke();
    }

    private void ApplyVisibilityMaterial()
    {
        if (visibilityMaterial == null) return;

        foreach (Renderer renderer in CurrentVisual.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = visibilityMaterial;
            }
            renderer.sharedMaterials = materials;

            // Shadow maps cannot use the camera stencil. Match the enemy body's no-shadow setup.
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private void ClearVisual()
    {
        if (CurrentVisual != null)
        {
            CurrentVisual.gameObject.SetActive(false);
            Destroy(CurrentVisual.gameObject);
        }

        CurrentVisual = null;
        CurrentData = null;

        // 무장 교체 시에도 발생 시 불필요한 상태 전이 가능성
        //VisualChanged?.Invoke();
    }

    private void SubscribeAndRefresh()
    {
        if (weaponProvider == null || weaponSocket == null)
        {
            return;
        }

        weaponProvider.WeaponChanged -= RefreshVisual;
        weaponProvider.WeaponChanged += RefreshVisual;

        RefreshVisual();
    }
}
