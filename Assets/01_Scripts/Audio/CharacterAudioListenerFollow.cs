using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioListener))]
[DefaultExecutionOrder(1000)]
public sealed class CharacterAudioListenerFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Camera outputCamera;
    [SerializeField] private Vector3 worldOffset = Vector3.up;
    private AudioListener replacedCameraListener;

    private void Awake()
    {
        if (target == null)
        {
            CharacterRoot owner = GetComponentInParent<CharacterRoot>();
            if (owner != null) target = owner.transform;
        }
    }

    public void SetOutputCamera(Camera camera) => outputCamera = camera;

    private void OnEnable()
    {
        if (outputCamera == null) outputCamera = Camera.main;
        UseCameraListenerFallback();
    }

    private void OnDisable() => RestoreCameraListener();

    private void UseCameraListenerFallback()
    {
        AudioListener candidate = outputCamera != null ? outputCamera.GetComponent<AudioListener>() : null;
        if (candidate == replacedCameraListener) return;
        RestoreCameraListener();
        if (candidate == null || !candidate.enabled) return;
        candidate.enabled = false;
        replacedCameraListener = candidate;
    }

    private void RestoreCameraListener()
    {
        if (replacedCameraListener != null) replacedCameraListener.enabled = true;
        replacedCameraListener = null;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + worldOffset;
        if (outputCamera == null || !outputCamera.isActiveAndEnabled) outputCamera = Camera.main;
        UseCameraListenerFallback();
        if (outputCamera == null) return;
        Vector3 forward = Vector3.ProjectOnPlane(outputCamera.transform.forward, Vector3.up);
        if (forward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }
}
