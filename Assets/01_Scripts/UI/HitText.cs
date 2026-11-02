using DG.Tweening;
using TMPro;
using UnityEngine;

public class HitText : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 0.5f;
    [SerializeField, Min(0.01f)] private float distance = 1.0f;
    [SerializeField] private float height = 1.0f;
    [SerializeField] private float drop = 0.3f;
    [SerializeField] private float fadeStart = 0.5f;
    [SerializeField] private Vector3 spawnOffset = Vector3.up;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color criticalColor = new Color(1f, 0.75f, 0.15f);

    private TMP_Text label;
    private Camera viewCamera;
    private Tween tween;
    private Vector3 originalScale;
    private Vector3 originalPosition;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.position = originalPosition;
        transform.localScale = originalScale;

        label.alpha = 1f;
    }

    public void Play(float damage, Vector3 hitPoint, Vector3 shotDirection, Camera camera, bool critical = false)
    {
        StopTween();

        viewCamera = camera;
        label.SetText($"{damage:0.0}");
        label.color = critical ? criticalColor : normalColor;

        Vector3 up = camera != null ? camera.transform.up : Vector3.up;

        Vector3 direction = shotDirection.sqrMagnitude > 0.0001f
            ? shotDirection.normalized
            : up;

        // 시작 접선이 총알 진행 방향과 일치하도록 설정
        Vector3 p0 = hitPoint + spawnOffset;
        Vector3 p1 = p0 + direction * (distance * 0.35f);
        Vector3 p2 = p0 + direction * (distance * 0.8f) + up * height;
        Vector3 p3 = p0 + direction * distance - up * drop;

        float criticalScale = critical ? 1.25f : 1f;

        void ApplyVisual(float t)
        {
            transform.position = Bezier(p0, p1, p2, p3, t);

            // 초반에 커졌다가 천천히 작아진다.
            float scale = t < 0.15f
                ? Mathf.Lerp(0.65f, 1.15f, t / 0.15f)
                : Mathf.Lerp(1.15f, 0.85f, (t - 0.15f) / 0.85f);

            transform.localScale = originalScale * scale * criticalScale;

            // 후반부에만 사라진다.
            label.alpha = 1f - Mathf.InverseLerp(fadeStart, 1f, t);
        }

        ApplyVisual(0f);

        tween = DOVirtual.Float(0f, 1f, duration, ApplyVisual)
            .SetEase(Ease.Linear)
            .OnComplete(ReturnToPool)
            .OnKill(() => tween = null);
    }

    public void ReturnToPool()
    {
        StopTween();

        PoolManager.Instance.ReturnPool<HitText>(this);
    }

    private static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;

        return u * u * u * p0
             + 3f * u * u * t * p1
             + 3f * u * t * t * p2
             + t * t * t * p3;
    }

    private void OnDisable()
    {
        StopTween();
    }

    private void StopTween()
    {
        tween?.Kill();
        tween = null;
    }
}
