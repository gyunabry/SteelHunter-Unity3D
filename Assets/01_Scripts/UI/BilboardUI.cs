using UnityEngine;

public class BilboardUI : MonoBehaviour
{
    private Transform cameraTransform;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        transform.rotation = cameraTransform.rotation;
    }
}
