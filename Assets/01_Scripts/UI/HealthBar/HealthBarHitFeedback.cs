using UnityEngine;

public class HealthBarHitFeedback : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RectTransform shakeRoot;
    [SerializeField] private RectTransform barArea;

    [Header("흔들림 크기")]
    [SerializeField, Min(0f)]
    private float minShakeRatio = 0.005f;

    [SerializeField, Min(0f)]
    private float maxShakeRatio = 0.025f;

    [Tooltip("한 번에 최대 체력의 이 비율을 잃으면 최대 강도로 흔들립니다.")]
    [SerializeField, Range(0.01f, 1f)]
    private float fullStrengthDamageRatio = 0.25f;

    [Header("흔들림 움직임")]
    [SerializeField, Min(0.01f)]
    private float shakeDuration = 0.14f;

    [SerializeField, Min(0f)]
    private float horizontalCycles = 3f;

    [SerializeField, Min(0f)]
    private float verticalCycles = 2f;

    [SerializeField, Range(0f, 1f)]
    private float verticalStrength = 0.25f;

    private Vector2 originalPosition;

    private float elapsed;
    private float amplitude;

    private bool initialized;
    private bool shaking;

    private void Awake()
    {
        if (shakeRoot == null || barArea == null)
        {
            Debug.LogError(
                "HealthBarHitFeedback의 Shake Root와 Bar Area를 연결하세요.",
                this
            );

            enabled = false;
            return;
        }

        originalPosition = shakeRoot.anchoredPosition;
        initialized = true;
    }

    private void Update()
    {
        UpdateShake();
    }

    private void OnDisable()
    {
        ResetFeedback();
    }

    private void OnValidate()
    {
        minShakeRatio = Mathf.Max(0f, minShakeRatio);
        maxShakeRatio = Mathf.Max(minShakeRatio, maxShakeRatio);

        shakeDuration = Mathf.Max(0.01f, shakeDuration);

        fullStrengthDamageRatio = Mathf.Clamp(
            fullStrengthDamageRatio,
            0.01f,
            1f
        );
    }

    public void Play(float previous, float current, float max)
    {
        if (!initialized || !isActiveAndEnabled)
            return;

        if (max <= 0f || current >= previous)
            return;

        StartShake(previous, current, max);
    }

    private void StartShake(float previous, float current, float max)
    {
        // 실제로 잃은 체력이 최대 체력에서 차지하는 비율
        float lostRatio = Mathf.Clamp01((previous - current) / max);

        // 피해 비율을 0~1의 연출 강도로 변환
        float strength = Mathf.Clamp01(lostRatio / fullStrengthDamageRatio);

        float shakeRatio = Mathf.Lerp(minShakeRatio, maxShakeRatio, strength);

        // 로컬 너비를 기준으로 계산
        float width = barArea.rect.width;

        if (width <= 0f) return;

        float nextAmplitude = width * shakeRatio;

        // 이미 흔들리는 중이면 현재 남은 진폭을 구한다
        float remainingAmplitude = 0f;

        if (shaking)
        {
            float t = Mathf.Clamp01(elapsed / shakeDuration);
            remainingAmplitude = amplitude * GetDecay(t);
        }

        // 작은 추가 피해 때문에 기존 흔들림이 갑자기 약해지지 않게 한다
        // 진폭을 더하지 않으므로 연사해도 무한히 커지지 않는다
        amplitude = Mathf.Min(Mathf.Max(remainingAmplitude, nextAmplitude), width * maxShakeRatio);

        elapsed = 0f;
        shaking = true;
    }

    private void UpdateShake()
    {
        if (!initialized || !shaking)
            return;

        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / shakeDuration);

        if (t >= 1f)
        {
            ResetFeedback();
            return;
        }

        float decay = GetDecay(t);

        float x = Mathf.Sin(
            t * Mathf.PI * 2f * horizontalCycles
        ) * amplitude * decay;

        float y = Mathf.Sin(
            t * Mathf.PI * 2f * verticalCycles
        ) * amplitude * verticalStrength * decay;

        shakeRoot.anchoredPosition = originalPosition + new Vector2(x, y);
    }

    private static float GetDecay(float t)
    {
        float remaining = 1f - t;
        return remaining * remaining;
    }

    public void ResetFeedback()
    {
        shaking = false;
        elapsed = 0f;
        amplitude = 0f;

        if (initialized && shakeRoot != null)
        {
            shakeRoot.anchoredPosition = originalPosition;
        }
    }
}