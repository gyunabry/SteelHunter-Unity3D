using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLookAhead : MonoBehaviour
{
    [SerializeField] private Camera outputCamera;

    [SerializeField, Min(0f)]
    private float maxDistance = 2.5f;

    private CinemachineFollow follow;
    private Vector3 baseOffset;
    private bool uiBlocked;

    private void Awake()
    {
        follow = GetComponent<CinemachineFollow>();
        baseOffset = follow.FollowOffset;

        if (outputCamera == null) outputCamera = Camera.main;
    }

    private void Update()
    {
        Vector3 lookOffset = Vector3.zero;

        if (!uiBlocked &&
            Application.isFocused &&
            Time.timeScale > 0f &&
            Mouse.current != null &&
            outputCamera != null)
        {
            Rect rect = outputCamera.pixelRect;
            Vector2 mouse = Mouse.current.position.ReadValue();

            if (rect.width > 0f && rect.height > 0f && rect.Contains(mouse))
            {
                Vector3 input = new Vector2(
                    (mouse.x - rect.xMin) / rect.width * 2f - 1f,
                    (mouse.y - rect.yMin) / rect.height * 2f - 1f
                );

                input = Vector2.ClampMagnitude(input, 1f);

                Vector3 right = transform.right;
                right.y = 0f;
                right.Normalize();

                Vector3 forward = Vector3.Cross(right, Vector3.up);

                lookOffset = (right * input.x + forward * input.y) * maxDistance;
            }
        }

        follow.FollowOffset = baseOffset + lookOffset;
    }

    public void SetUIBlocked(bool blocked)
    {
        uiBlocked = blocked;
    }

    private void OnDisable()
    {
        if (follow != null)
            follow.FollowOffset = baseOffset;
    }
}
